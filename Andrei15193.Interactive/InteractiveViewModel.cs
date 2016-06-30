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
    /// <summary>
    /// Represents the implementation of a state machine for managing asynchronous states
    /// that a view model may go through.
    /// </summary>
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

        /// <summary>
        /// Represents an <see cref="ICommand"/> that triggers a state change on the
        /// underlying <see cref="InteractiveViewModel"/>.
        /// </summary>
        protected sealed class TransitionCommand
            : ICommand
        {
            private readonly InteractiveViewModel _interactiveViewModel;
            private readonly string _destinationState;
            private readonly Action<ErrorContext> _errorHandler;

            /// <summary>
            /// Creates a new <see cref="TransitionCommand"/> instance.
            /// </summary>
            /// <param name="interactiveViewModel">
            /// The <see cref="InteractiveViewModel"/> unto which to trigger a transition.
            /// </param>
            /// <param name="destinationState">
            /// The name of the state to transition to.
            /// </param>
            /// <param name="errorHandler">
            /// An optional error handler callback that will be invoked in case the transition
            /// becomes faulted or canceled.
            /// </param>
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

            /// <summary>
            /// Occurs when the transition completes.
            /// </summary>
            public event EventHandler ExecuteCompleted;

            /// <summary>
            /// Checks whether the command can be executed with the provided <paramref name="parameter"/>.
            /// </summary>
            /// <param name="parameter">
            /// An optional parameter to provide additional data for the command to process.
            /// </param>
            /// <returns>
            /// Returns true if the command can execute; otherwise false.
            /// </returns>
            public bool CanExecute(object parameter)
                => true;

            /// <summary>
            /// Triggers a state transition on the underlying <see cref="InteractiveViewModel"/>.
            /// </summary>
            /// <param name="parameter">
            /// An optional parameter to provide additional data for the command to process.
            /// </param>
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

                ExecuteCompleted?.Invoke(this, EventArgs.Empty);
            }

            /// <summary>
            /// Binds the command to the specified states.
            /// </summary>
            /// <param name="states">
            /// The state in which the command is available.
            /// </param>
            /// <returns>
            /// Returns a proxy <see cref="ICommand"/> that is only available when the
            /// underlying <see cref="InteractiveViewModel"/> is in one of the provided
            /// <paramref name="states"/>.
            /// </returns>
            public ICommand BindTo(IEnumerable<string> states)
                => _interactiveViewModel.BindCommand(this, states);

            /// <summary>
            /// Binds the command to the specified states.
            /// </summary>
            /// <param name="states">
            /// The state in which the command is available.
            /// </param>
            /// <returns>
            /// Returns a proxy <see cref="ICommand"/> that is only available when the
            /// underlying <see cref="InteractiveViewModel"/> is in one of the provided
            /// <paramref name="states"/>.
            /// </returns>
            public ICommand BindTo(params string[] states)
                => BindTo(states.AsEnumerable());
        }

        /// <summary>
        /// Represents an <see cref="ICommand"/> that enqueues a state change on the
        /// underlying <see cref="InteractiveViewModel"/>.
        /// </summary>
        protected sealed class EnqueuingTransitionCommand
            : ICommand
        {
            private readonly InteractiveViewModel _interactiveViewModel;
            private readonly string _destinationState;
            private readonly Action<ErrorContext> _errorHandler;

            /// <summary>
            /// Creates a new <see cref="EnqueuingTransitionCommand"/> instance.
            /// </summary>
            /// <param name="interactiveViewModel">
            /// The <see cref="InteractiveViewModel"/> unto which to enqueue a transition.
            /// </param>
            /// <param name="destinationState">
            /// The name of the state to enqueue the transition to.
            /// </param>
            /// <param name="errorHandler">
            /// An optional error handler callback that will be invoked in case the transition
            /// becomes faulted or canceled.
            /// </param>
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

            /// <summary>
            /// Occurs when the transition completes.
            /// </summary>
            public event EventHandler ExecuteCompleted;

            /// <summary>
            /// Checks whether the command can be executed with the provided <paramref name="parameter"/>.
            /// </summary>
            /// <param name="parameter">
            /// An optional parameter to provide additional data for the command to process.
            /// </param>
            /// <returns>
            /// Returns true if the command can execute; otherwise false.
            /// </returns>
            public bool CanExecute(object parameter)
                => true;

            /// <summary>
            /// Enqueues a state transition on the underlying <see cref="InteractiveViewModel"/>.
            /// </summary>
            /// <param name="parameter">
            /// An optional parameter to provide additional data for the command to process.
            /// </param>
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

                ExecuteCompleted?.Invoke(this, EventArgs.Empty);
            }

            /// <summary>
            /// Binds the command to the specified states.
            /// </summary>
            /// <param name="states">
            /// The state in which the command is available.
            /// </param>
            /// <returns>
            /// Returns a proxy <see cref="ICommand"/> that is only available when the
            /// underlying <see cref="InteractiveViewModel"/> is in one of the provided
            /// <paramref name="states"/>.
            /// </returns>
            public ICommand BindTo(IEnumerable<string> states)
                => _interactiveViewModel.BindCommand(this, states);

            /// <summary>
            /// Binds the command to the specified states.
            /// </summary>
            /// <param name="states">
            /// The state in which the command is available.
            /// </param>
            /// <returns>
            /// Returns a proxy <see cref="ICommand"/> that is only available when the
            /// underlying <see cref="InteractiveViewModel"/> is in one of the provided
            /// <paramref name="states"/>.
            /// </returns>
            public ICommand BindTo(params string[] states)
                => BindTo(states.AsEnumerable());
        }

        private string _state = null;
        private bool _isInActionState = false;
        private string _lastEnqueuedState = null;
        private Task _lastTransitionTask = Task.FromResult<object>(null);
        private readonly CancellationCommand _cancelCommand = new CancellationCommand();
        private readonly IDictionary<string, ViewModelState> _states = new Dictionary<string, ViewModelState>(StateStringComparer);

        /// <summary>
        /// Creates a new <see cref="InteractiveViewModel"/> instance.
        /// </summary>
        public InteractiveViewModel()
        {
        }

        /// <summary>
        /// The state in which the <see cref="InteractiveViewModel"/> is currently in.
        /// </summary>
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

        /// <summary>
        /// An <see cref="ICommand"/> that can be used to cancel the current asynchronous operation.
        /// </summary>
        public ICommand CancelCommand
            => _cancelCommand;

        /// <summary>
        /// Creates a synchronous action state.
        /// </summary>
        /// <param name="name">
        /// The name of the action state.
        /// </param>
        /// <param name="action">
        /// A callback to perform the actual operation.
        /// </param>
        protected void CreateActionState(string name, Action<ActionStateContext> action)
        {
            _states.Add(
                name,
                new ViewModelState(name, action));
        }
        /// <summary>
        /// Creates an asynchronous action state.
        /// </summary>
        /// <param name="name">
        /// The name of the action state.
        /// </param>
        /// <param name="asyncAction">
        /// A callback to perform the actual asynchronous operation.
        /// </param>
        protected void CreateActionState(string name, Func<ActionStateContext, Task> asyncAction)
        {
            _states.Add(
                name,
                new ViewModelState(name, asyncAction));
        }
        /// <summary>
        /// Creates a cancelable asynchronous action state.
        /// </summary>
        /// <param name="name">
        /// The name of the action state.
        /// </param>
        /// <param name="cancelableAsyncAction">
        /// A callback to perform the actual cancelable asynchronous operation.
        /// </param>
        protected void CreateActionState(string name, Func<ActionStateContext, CancellationToken, Task> cancelableAsyncAction)
        {
            _states.Add(
                name,
                new ViewModelState(name, cancelableAsyncAction));
        }

        /// <summary>
        /// Transitions the <see cref="InteractiveViewModel"/> to the given <paramref name="state"/>.
        /// </summary>
        /// <param name="state">
        /// The state to transition to.
        /// </param>
        /// <param name="parameter">
        /// A parameter containing additional data that an action state might use.
        /// </param>
        /// <returns>
        /// Returns an awaitable task that completes once the transition is complete. If the
        /// target <paramref name="state"/> is an action state then the task will complete
        /// when the <see cref="InteractiveViewModel"/> will finally transition into a
        /// quiet state or become faulted or canceled.
        /// </returns>
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
        /// <summary>
        /// Transitions the <see cref="InteractiveViewModel"/> to the given <paramref name="state"/>.
        /// </summary>
        /// <param name="state">
        /// The state to transition to.
        /// </param>
        /// <returns>
        /// Returns an awaitable task that completes once the transition is complete. If the
        /// target <paramref name="state"/> is an action state then the task will complete
        /// when the <see cref="InteractiveViewModel"/> will finally transition into a
        /// quiet state or become faulted or canceled.
        /// </returns>
        protected Task TransitionToAsync(string state)
            => TransitionToAsync(state, null);

        /// <summary>
        /// Enqueues a transition to the <see cref="InteractiveViewModel"/> to the given <paramref name="state"/>.
        /// </summary>
        /// <param name="state">
        /// The state to eventaully transition to.
        /// </param>
        /// <param name="parameter">
        /// A parameter containing additional data that an action state might use.
        /// </param>
        /// <returns>
        /// Returns an awaitable task that completes once the transition is complete.
        /// <para>
        /// If the <see cref="InteractiveViewModel"/> is already in an action state, then
        /// task will await the respective transition and complete only when the
        /// <see cref="InteractiveViewModel"/> finally transitions to the provided
        /// <paramref name="state"/>.
        /// </para>
        /// <para>
        /// If the target <paramref name="state"/> is an action state then the task will
        /// complete when the <see cref="InteractiveViewModel"/> will finally transition
        /// into a quiet state or become faulted or canceled.
        /// </para>
        /// </returns>
        protected Task EnqueueTransitionToAsync(string state, object parameter)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (_lastTransitionTask.IsCompleted)
            {
                _lastEnqueuedState = null;
                _lastTransitionTask = TransitionToAsync(state, parameter);
            }
            else if (!StateStringComparer.Equals(_lastEnqueuedState, state))
            {
                var previousTransitionTask = _lastTransitionTask;
                _lastEnqueuedState = state;
                _lastTransitionTask = new Func<Task>(
                    async delegate
                    {
                        await Task.Yield();
                        await previousTransitionTask;
                        await TransitionToAsync(state, parameter);
                    }).Invoke();
            }

            return _lastTransitionTask;
        }
        /// <summary>
        /// Enqueues a transition to the <see cref="InteractiveViewModel"/> to the given <paramref name="state"/>.
        /// </summary>
        /// <param name="state">
        /// The state to eventaully transition to.
        /// </param>
        /// <returns>
        /// Returns an awaitable task that completes once the transition is complete.
        /// <para>
        /// If the <see cref="InteractiveViewModel"/> is already in an action state, then
        /// task will await the respective transition and complete only when the
        /// <see cref="InteractiveViewModel"/> finally transitions to the provided
        /// <paramref name="state"/>.
        /// </para>
        /// <para>
        /// If the target <paramref name="state"/> is an action state then the task will
        /// complete when the <see cref="InteractiveViewModel"/> will finally transition
        /// into a quiet state or become faulted or canceled.
        /// </para>
        /// </returns>
        protected Task EnqueueTransitionToAsync(string state)
            => EnqueueTransitionToAsync(state, null);

        /// <summary>
        /// Binds an <see cref="ICommand"/> to the given <paramref name="states"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="ICommand"/> to bind.
        /// </param>
        /// <param name="states">
        /// The <paramref name="states"/> under which the provided <paramref name="command"/>
        /// may become available.
        /// </param>
        /// <returns>
        /// Returns an <see cref="ICommand"/> proxy that delegates execution to the provided
        /// <paramref name="command"/> but may only be available when the
        /// <see cref="InteractiveViewModel"/> is in one of the provided <paramref name="states"/>.
        /// </returns>
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
        /// <summary>
        /// Binds an <see cref="ICommand"/> to the given <paramref name="states"/>.
        /// </summary>
        /// <param name="command">
        /// The <see cref="ICommand"/> to bind.
        /// </param>
        /// <param name="states">
        /// The <paramref name="states"/> under which the provided <paramref name="command"/>
        /// may become available.
        /// </param>
        /// <returns>
        /// Returns an <see cref="ICommand"/> proxy that delegates execution to the provided
        /// <paramref name="command"/> but may only be available when the
        /// <see cref="InteractiveViewModel"/> is in one of the provided <paramref name="states"/>.
        /// </returns>
        protected ICommand BindCommand(ICommand command, params string[] states)
            => BindCommand(command, states.AsEnumerable());

        /// <summary>
        /// Gets a <see cref="TransitionCommand"/> that can be exposed in order to
        /// trigger state transitions.
        /// </summary>
        /// <param name="destinationState">
        /// The name of te state to trigger the transition to.
        /// </param>
        /// <returns>
        /// Returns a <see cref="TransitionCommand"/> that upon execution will invoke
        /// <see cref="TransitionToAsync(string,object)"/> on the current
        /// <see cref="InteractiveViewModel"/>.
        /// </returns>
        protected TransitionCommand GetTransitionCommand(string destinationState)
            => GetTransitionCommand(destinationState, null);
        /// <summary>
        /// Gets a <see cref="TransitionCommand"/> that can be exposed in order to
        /// trigger state transitions.
        /// </summary>
        /// <param name="destinationState">
        /// The name of te state to trigger the transition to.
        /// </param>
        /// <param name="errorHandler">
        /// An error handler callback that will be invoked if the transition becomes
        /// faulted or canceled.
        /// </param>
        /// <returns>
        /// Returns a <see cref="TransitionCommand"/> that upon execution will invoke
        /// <see cref="TransitionToAsync(string,object)"/> on the current
        /// <see cref="InteractiveViewModel"/>.
        /// </returns>
        protected TransitionCommand GetTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
            => new TransitionCommand(this, destinationState, errorHandler);

        /// <summary>
        /// Gets an <see cref="EnqueuingTransitionCommand"/> that can be exposed in order
        /// to enqueue state transitions.
        /// </summary>
        /// <param name="destinationState">
        /// The name of te state to enqueue a transition to.
        /// </param>
        /// <returns>
        /// Returns an <see cref="EnqueuingTransitionCommand"/> that upon execution will
        /// invoke <see cref="EnqueueTransitionToAsync(string,object)"/> on the current
        /// <see cref="InteractiveViewModel"/>.
        /// </returns>
        protected EnqueuingTransitionCommand GetEnqueuingTransitionCommand(string destinationState)
            => GetEnqueuingTransitionCommand(destinationState, null);

        /// <summary>
        /// Gets an <see cref="EnqueuingTransitionCommand"/> that can be exposed in order
        /// to enqueue state transitions.
        /// </summary>
        /// <param name="destinationState">
        /// The name of te state to enqueue a transition to.
        /// </param>
        /// <param name="errorHandler">
        /// An error handler callback that will be invoked if the transition becomes
        /// faulted or canceled.
        /// </param>
        /// <returns>
        /// Returns an <see cref="EnqueuingTransitionCommand"/> that upon execution will
        /// invoke <see cref="EnqueueTransitionToAsync(string,object)"/> on the current
        /// <see cref="InteractiveViewModel"/>.
        /// </returns>
        protected EnqueuingTransitionCommand GetEnqueuingTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
            => new EnqueuingTransitionCommand(this, destinationState, errorHandler);
    }

    /// <summary>
    /// Represents an <see cref="InteractiveViewModel"/> that operates on a data context.
    /// </summary>
    /// <typeparam name="TDataModel">
    /// The type of the data object.
    /// </typeparam>
    public class InteractiveViewModel<TDataModel>
        : InteractiveViewModel
    {
        /// <summary>
        /// Creates a new <see cref="InteractiveViewModel{TDataModel}"/> instance.
        /// </summary>
        /// <param name="dataModel">
        /// The object to use as data.
        /// </param>
        public InteractiveViewModel(TDataModel dataModel)
        {
            DataModel = dataModel;
            Errors = new ObservableCollection<ValidationError>();
            Context = new ViewModelContext<TDataModel>(DataModel, new ReadOnlyObservableCollection<ValidationError>(Errors));
        }

        /// <summary>
        /// The context on which the current <see cref="InteractiveViewModel{TDataModel}"/> relies on.
        /// </summary>
        public ViewModelContext<TDataModel> Context { get; }

        /// <summary>
        /// The data object on which the current instance reies on.
        /// </summary>
        protected TDataModel DataModel { get; }

        /// <summary>
        /// A mutable collection of <see cref="ValidationError"/>s concerning the <see cref="DataModel"/>.
        /// </summary>
        protected ObservableCollection<ValidationError> Errors { get; }
    }
}