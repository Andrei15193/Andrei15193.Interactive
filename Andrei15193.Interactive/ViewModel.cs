using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Andrei15193.Interactive
{
    public class ViewModel
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
            private CancellationTokenSource _cancellationTokenSource;

            public CancellationTokenSource CancellationTokenSource
            {
                get
                {
                    return _cancellationTokenSource;
                }
                set
                {
                    if (_cancellationTokenSource != value)
                    {
                        _cancellationTokenSource = value;
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object delay)
                => _cancellationTokenSource != null;

            public void Execute(object delay)
            {
                if (!CanExecute(delay))
                    throw new InvalidOperationException("Cannot invoke the cancel command while the view model is in an uncancelable state.");

                _cancellationTokenSource.Cancel();
            }
        }

        private sealed class BoundCommand
            : ICommand
        {
            private bool _isInBoundState;
            private readonly ViewModel _viewModel;
            private readonly ICommand _command;

            public BoundCommand(ViewModel viewModel, ICommand command)
            {
                if (viewModel == null)
                    throw new ArgumentNullException(nameof(viewModel));
                if (command == null)
                    throw new ArgumentNullException(nameof(command));

                _isInBoundState = false;
                _viewModel = viewModel;
                _command = command;
                BoundStates = new HashSet<string>(StateStringComparer);
                viewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(viewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase)
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
            private readonly ViewModel _viewModel;
            private readonly string _destinationState;
            private readonly Action<ErrorContext> _errorHandler;

            internal TransitionCommand(ViewModel viewModel, string destinationState, Action<ErrorContext> errorHandler)
            {
                if (viewModel == null)
                    throw new ArgumentNullException(nameof(viewModel));
                if (destinationState == null)
                    throw new ArgumentNullException(nameof(destinationState));

                _viewModel = viewModel;
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
                    await _viewModel
                        .TransitionToAsync(destinationState)
                        .ContinueWith(
                            task =>
                            {
                                if (task.Status == TaskStatus.RanToCompletion)
                                    destinationState = null;
                                else
                                {
                                    if (_errorHandler == null)
                                        throw new InvalidOperationException("Unhandled exception by user code.", task.Exception);

                                    ErrorContext errorContext;
                                    if (task.IsCanceled)
                                        errorContext = new ErrorContext(_viewModel, _viewModel._state);
                                    else
                                        errorContext = new ErrorContext(_viewModel, _viewModel._state, task.Exception);

                                    _errorHandler(errorContext);
                                    if (errorContext.NextState == null)
                                        throw new InvalidOperationException("Cannot transition to 'null' state.");

                                    destinationState = errorContext.NextState;
                                }
                            });

                (parameter as ManualResetEventSlim)?.Set();
            }

            public ICommand BindTo(IEnumerable<string> states)
                => _viewModel.BindCommand(this, states);
            public ICommand BindTo(params string[] states)
                => BindTo(states.AsEnumerable());
        }

        private string _state;
        private bool _isInActionState = false;
        private readonly CancellationCommand _cancelCommand;
        private readonly IDictionary<string, ViewModelState> _states = new Dictionary<string, ViewModelState>(StateStringComparer);

        internal ViewModel(object dataContext)
        {
            if (dataContext == null)
                throw new ArgumentNullException(nameof(dataContext));

            DataContext = dataContext;
            _cancelCommand = new CancellationCommand();
        }

        protected object DataContext
        {
            get;
        }

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

        protected async Task TransitionToAsync(string state)
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
                    ViewModelState viewModelState;
                    while (_states.TryGetValue(nextState, out viewModelState))
                    {
                        var actionStateContext = new ActionStateContext(this, _state);
                        State = nextState;

                        if (viewModelState.IsCancellable)
                            _cancelCommand.CancellationTokenSource = cancellationTokenSource;
                        else
                            _cancelCommand.CancellationTokenSource = null;
                        await viewModelState.ExecuteAsync(actionStateContext, cancellationTokenSource.Token);

                        if (actionStateContext.NextState == null)
                            throw new InvalidOperationException("Cannot transition to 'null' state.");
                        nextState = actionStateContext.NextState;
                    }
                }
            }
            finally
            {
                _cancelCommand.CancellationTokenSource = null;
                _isInActionState = false;
            }

            State = nextState;
        }

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
    }

    public class ViewModel<TDataContext>
        : ViewModel
    {
        public ViewModel(TDataContext dataContext)
            : base(dataContext)
        {
            DataContext = dataContext;
        }

        new public TDataContext DataContext { get; }
    }
}