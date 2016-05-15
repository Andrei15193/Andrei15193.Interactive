using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Andrei15193.Interactive.Validation;

namespace Andrei15193.Interactive
{
    public class InteractiveViewModel
        : PropertyChangedNotifier
    {
        private static readonly IEqualityComparer<string> StateStringComparer = StringComparer.OrdinalIgnoreCase;

        private sealed class ViewModelState
        {
            private readonly Action<ActionStateContext> _action;
            private readonly Func<ActionStateContext, Task> _asyncAction;
            private readonly Func<ActionStateContext, CancellationToken, Task> _cancelableAsyncAction;

            public ViewModelState(string name, Action<ActionStateContext> action)
                : this(name)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                IsCancellable = false;
                _action = action;
            }
            public ViewModelState(string name, Func<ActionStateContext, Task> asyncAction)
                : this(name)
            {
                if (asyncAction == null)
                    throw new ArgumentNullException(nameof(asyncAction));

                IsCancellable = false;
                _asyncAction = asyncAction;
            }
            public ViewModelState(string name, Func<ActionStateContext, CancellationToken, Task> asyncCancelableAction)
                : this(name)
            {
                if (asyncCancelableAction == null)
                    throw new ArgumentNullException(nameof(asyncCancelableAction));

                IsCancellable = true;
                _cancelableAsyncAction = asyncCancelableAction;
            }
            private ViewModelState(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    if (name == null)
                        throw new ArgumentNullException(nameof(name));
                    else
                        throw new ArgumentException("Cannot be empty or white space.", nameof(name));

                Name = name;
            }

            public string Name { get; }

            public bool IsCancellable { get; }

            public async Task ExecuteAsync(ActionStateContext actionStateContext, CancellationToken cancellationToken)
            {
                if (_action != null)
                    _action(actionStateContext);
                else if (_asyncAction != null)
                    await _asyncAction(actionStateContext);
                else
                    await _cancelableAsyncAction(actionStateContext, cancellationToken);
            }
        }

        private sealed class CancellationCommand
            : ICommand
        {
            private bool _canExecuteCommand;

            public CancellationTokenSource CancellationTokenSource
            {
                get;
                set;
            }

            public bool CanExecuteCommand
            {
                get
                {
                    return _canExecuteCommand;
                }
                set
                {
                    if (_canExecuteCommand != value)
                    {
                        _canExecuteCommand = value;
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object delay)
                => CancellationTokenSource != null && _canExecuteCommand;

            public void Execute(object delay)
            {
                if (CancellationTokenSource == null)
                    throw new InvalidOperationException("Cannot invoke the cancel command while the view model is in an uncancelable state.");

                CancellationTokenSource.Cancel();
            }
        }

        private sealed class BoundCommand
            : ICommand
        {
            private bool _isInBoundState;
            private readonly InteractiveViewModel _viewModel;
            private readonly ICommand _command;

            public BoundCommand(InteractiveViewModel interactiveViewModel, ICommand command)
            {
                if (interactiveViewModel == null)
                    throw new ArgumentNullException(nameof(interactiveViewModel));
                if (command == null)
                    throw new ArgumentNullException(nameof(command));

                _isInBoundState = false;
                _viewModel = interactiveViewModel;
                _command = command;
                BoundStates = new HashSet<string>(StateStringComparer);
                interactiveViewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(interactiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase)
                            && BoundStates.Contains(_viewModel.State) != _isInBoundState)
                        {
                            _isInBoundState = !_isInBoundState;
                            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                        }
                    };
                command.CanExecuteChanged +=
                    delegate
                    {
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    };
            }

            public ICollection<string> BoundStates { get; }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
                => _isInBoundState && _command.CanExecute(parameter);

            public void Execute(object parameter)
                => _command.Execute(parameter);
        }

        protected sealed class TransitionCommand
            : ICommand
        {
            private readonly InteractiveViewModel _interactiveViewModel;
            private readonly string _destinationState;
            private readonly Action<ErrorContext> _errorHandler;

            internal TransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState, Action<ErrorContext> errorHandler)
            {
                if (interactiveViewModel == null)
                    throw new ArgumentNullException(nameof(interactiveViewModel));
                if (destinationState == null)
                    throw new ArgumentNullException(nameof(destinationState));

                _interactiveViewModel = interactiveViewModel;
                _destinationState = destinationState;
                _errorHandler = errorHandler;
            }

            event EventHandler ICommand.CanExecuteChanged { add { } remove { } }

            public bool CanExecute(object parameter)
                => true;

            public async void Execute(object parameter)
            {
                var destinationState = _destinationState;
                while (destinationState != null)
                {
                    var canTransitionWhenFaulted = !_interactiveViewModel._isInActionState;
                    var transitionTask = _interactiveViewModel.TransitionToAsync(destinationState, parameter);
                    try
                    {
                        await transitionTask;
                        destinationState = null;
                    }
                    catch
                    {
                        if (_errorHandler == null)
                            throw new InvalidOperationException("Unhandled exception by user code.", transitionTask.Exception);

                        ErrorContext errorContext;
                        if (transitionTask.IsCanceled)
                            errorContext = new ErrorContext(_interactiveViewModel, _interactiveViewModel._state);
                        else
                            errorContext = new ErrorContext(_interactiveViewModel, _interactiveViewModel._state, canTransitionWhenFaulted, transitionTask.Exception);

                        _errorHandler(errorContext);
                        if (!errorContext.CanTransition)
                            destinationState = null;
                        else if (errorContext.NextState == null)
                            throw new InvalidOperationException("Cannot transition to 'null' state.");
                        else
                            destinationState = errorContext.NextState;
                    }
                }

                (parameter as ManualResetEventSlim)?.Set();
            }

            public ICommand BindTo(IEnumerable<string> states)
                => _interactiveViewModel.BindCommand(this, states);
            public ICommand BindTo(params string[] states)
                => BindTo(states.AsEnumerable());
        }

        protected sealed class EnqueuingTransitionCommand
            : ICommand
        {
            private readonly InteractiveViewModel _interactiveViewModel;
            private readonly string _destinationState;
            private readonly Action<ErrorContext> _errorHandler;

            internal EnqueuingTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState, Action<ErrorContext> errorHandler)
            {
                if (interactiveViewModel == null)
                    throw new ArgumentNullException(nameof(interactiveViewModel));
                if (destinationState == null)
                    throw new ArgumentNullException(nameof(destinationState));

                _interactiveViewModel = interactiveViewModel;
                _destinationState = destinationState;
                _errorHandler = errorHandler;
            }

            event EventHandler ICommand.CanExecuteChanged { add { } remove { } }

            public bool CanExecute(object parameter)
                => true;

            public async void Execute(object parameter)
            {
                var destinationState = _destinationState;
                while (destinationState != null)
                {
                    var enqueuedTransitionTask = _interactiveViewModel.EnqueueTransitionToAsync(destinationState, parameter);
                    try
                    {
                        await enqueuedTransitionTask;
                        destinationState = null;
                    }
                    catch
                    {
                        if (_errorHandler == null)
                            throw new InvalidOperationException("Unhandled exception by user code.", enqueuedTransitionTask.Exception);

                        ErrorContext errorContext;
                        if (enqueuedTransitionTask.IsCanceled)
                            errorContext = new ErrorContext(_interactiveViewModel, _interactiveViewModel._state);
                        else
                            errorContext = new ErrorContext(_interactiveViewModel, _interactiveViewModel._state, false, enqueuedTransitionTask.Exception);

                        _errorHandler(errorContext);
                        if (errorContext.NextState == null)
                            throw new InvalidOperationException("Cannot transition to 'null' state.");

                        destinationState = errorContext.NextState;
                    }
                }

                (parameter as ManualResetEventSlim)?.Set();
            }

            public ICommand BindTo(IEnumerable<string> states)
                => _interactiveViewModel.BindCommand(this, states);
            public ICommand BindTo(params string[] states)
                => BindTo(states.AsEnumerable());
        }

        private string _state = null;
        private bool _isInActionState = false;
        private string _lastEnqueuedState = null;
        private Task _lastTransitionTask = Task.FromResult<object>(null);
        private readonly CancellationCommand _cancelCommand = new CancellationCommand();
        private readonly IDictionary<string, ViewModelState> _states = new Dictionary<string, ViewModelState>(StateStringComparer);

        public string State
        {
            get
            {
                if (_state == null)
                    throw new InvalidOperationException("The state has not been set.");
                return _state;
            }
            set
            {
                _state = value;
                Debug.WriteLine($"InteractiveViewModel.{nameof(State)} = {value}");
                NotifyPropertyChanged(nameof(State));
            }
        }

        public ICommand CancelCommand
            => _cancelCommand;

        protected void CreateActionState(string name, Action<ActionStateContext> action)
        {
            _states.Add(
                name,
                new ViewModelState(name, action));
        }
        protected void CreateActionState(string name, Func<ActionStateContext, Task> asyncAction)
        {
            _states.Add(
                name,
                new ViewModelState(name, asyncAction));
        }
        protected void CreateActionState(string name, Func<ActionStateContext, CancellationToken, Task> cancelableAsyncAction)
        {
            _states.Add(
                name,
                new ViewModelState(name, cancelableAsyncAction));
        }

        protected Task TransitionToAsync(string state, object parameter)
            => _lastTransitionTask = new Func<Task>(
                async delegate
                {
                    if (state == null)
                        throw new ArgumentNullException(nameof(state));
                    if (_isInActionState)
                        throw new InvalidOperationException("Cannot transition to a different state while in an action state.");

                    var nextState = state;
                    try
                    {
                        _isInActionState = true;
                        using (var cancellationTokenSource = new CancellationTokenSource())
                        {
                            _cancelCommand.CancellationTokenSource = cancellationTokenSource;
                            ViewModelState viewModelState;
                            while (_states.TryGetValue(nextState, out viewModelState))
                            {
                                var actionStateContext = new ActionStateContext(this, _state, parameter);

                                _state = nextState;
                                await Task.Yield();
                                State = nextState;

                                if (viewModelState.IsCancellable)
                                    _cancelCommand.CanExecuteCommand = true;
                                else
                                    _cancelCommand.CanExecuteCommand = false;

                                await viewModelState.ExecuteAsync(actionStateContext, cancellationTokenSource.Token);

                                if (actionStateContext.NextState == null)
                                    throw new InvalidOperationException("Cannot transition to 'null' state.");
                                nextState = actionStateContext.NextState;
                            }
                        }
                    }
                    finally
                    {
                        _cancelCommand.CanExecuteCommand = false;
                        _cancelCommand.CancellationTokenSource = null;
                        _isInActionState = false;
                    }

                    State = nextState;
                }).Invoke();
        protected Task TransitionToAsync(string state)
            => TransitionToAsync(state, null);

        protected Task EnqueueTransitionToAsync(string state, object paramter)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (_lastTransitionTask.IsCompleted)
            {
                _lastEnqueuedState = null;
                _lastTransitionTask = TransitionToAsync(state, paramter);
            }
            else if (!StateStringComparer.Equals(_lastEnqueuedState, state))
            {
                _lastEnqueuedState = state;
                _lastTransitionTask = new Func<Task>(
                    async delegate
                    {
                        await _lastTransitionTask;
                        await TransitionToAsync(state, paramter);
                    }).Invoke();
            }

            return _lastTransitionTask;
        }
        protected Task EnqueueTransitionToAsync(string state)
            => EnqueueTransitionToAsync(state, null);

        protected ICommand BindCommand(ICommand command, IEnumerable<string> states)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (states == null)
                throw new ArgumentNullException(nameof(states));

            BoundCommand boundCommand = command as BoundCommand;
            if (boundCommand == null)
                boundCommand = new BoundCommand(this, command);

            foreach (var state in states)
                if (state != null)
                    boundCommand.BoundStates.Add(state);

            return boundCommand;
        }
        protected ICommand BindCommand(ICommand command, params string[] states)
            => BindCommand(command, states.AsEnumerable());

        protected TransitionCommand GetTransitionCommand(string destinationState)
            => GetTransitionCommand(destinationState, null);
        protected TransitionCommand GetTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
            => new TransitionCommand(this, destinationState, errorHandler);

        protected EnqueuingTransitionCommand GetEnqueuingTransitionCommand(string destinationState)
            => GetEnqueuingTransitionCommand(destinationState, null);
        protected EnqueuingTransitionCommand GetEnqueuingTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
            => new EnqueuingTransitionCommand(this, destinationState, errorHandler);
    }

    public class InteractiveViewModel<TDataModel>
        : InteractiveViewModel
    {
        public InteractiveViewModel(TDataModel dataModel)
        {
            DataModel = dataModel;
            Errors = new ObservableCollection<ValidationError>();
            Context = new ViewModelContext<TDataModel>(DataModel, new ReadOnlyObservableCollection<ValidationError>(Errors));
        }

        public ViewModelContext<TDataModel> Context { get; }

        protected TDataModel DataModel { get; }

        protected ObservableCollection<ValidationError> Errors { get; }
    }
}