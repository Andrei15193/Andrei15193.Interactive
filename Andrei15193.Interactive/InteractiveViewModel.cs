using System;
using System.Collections;
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
            private interface ITransitionCommand
                : ICommand
            {
                [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
                event EventHandler ExecuteCompleted;

                ICommand BindTo(IEnumerable<string> states);

                ICommand BindTo(params string[] states);
            }

            private class CurrentTransitionCommand
                : ITransitionCommand
            {
                private readonly InteractiveViewModel _interactiveViewModel;
                private readonly string _destinationState;

                internal CurrentTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState)
                {
                    if (interactiveViewModel == null)
                        throw new ArgumentNullException(nameof(interactiveViewModel));
                    if (destinationState == null)
                        throw new ArgumentNullException(nameof(destinationState));

                    _interactiveViewModel = interactiveViewModel;
                    _destinationState = destinationState;
                }

                event EventHandler ICommand.CanExecuteChanged { add { } remove { } }

                [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
                public event EventHandler ExecuteCompleted;

                public bool CanExecute(object parameter)
                    => true;

                public async void Execute(object parameter)
                {
                    await _interactiveViewModel.TransitionToAsync(_destinationState, parameter);
                    ExecuteCompleted?.Invoke(this, EventArgs.Empty);
                }

                public ICommand BindTo(IEnumerable<string> states)
                    => _interactiveViewModel.BindCommand(this, states);

                public ICommand BindTo(params string[] states)
                    => BindTo(states.AsEnumerable());
            }

            [Obsolete("This is an obsolete implementation. Use CurrentTransitionCommand instead.")]
            private class ObsoleteTransitionCommand
                : ITransitionCommand
            {
                private readonly InteractiveViewModel _interactiveViewModel;
                private readonly string _destinationState;
                private readonly Action<ErrorContext> _errorHandler;

                internal ObsoleteTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState, Action<ErrorContext> errorHandler)
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

                [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
                public event EventHandler ExecuteCompleted;

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

                    ExecuteCompleted?.Invoke(this, EventArgs.Empty);
                }

                public ICommand BindTo(IEnumerable<string> states)
                    => _interactiveViewModel.BindCommand(this, states);

                public ICommand BindTo(params string[] states)
                    => BindTo(states.AsEnumerable());
            }

            private readonly ITransitionCommand _transitionCommand;

            /// <summary>
            /// Creates a new <see cref="TransitionCommand"/> instance.
            /// </summary>
            /// <param name="interactiveViewModel">
            /// The <see cref="InteractiveViewModel"/> unto which to trigger a transition.
            /// </param>
            /// <param name="destinationState">
            /// The name of the state to transition to.
            /// </param>
            internal TransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState)
            {
                _transitionCommand = new CurrentTransitionCommand(interactiveViewModel, destinationState);
            }
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
            [Obsolete("This constructor takes an error handler callback which is no longer supported. Consider using an constructor overload that does not require an error handler callback.")]
            internal TransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState, Action<ErrorContext> errorHandler)
            {
                _transitionCommand = new ObsoleteTransitionCommand(interactiveViewModel, destinationState, errorHandler);
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                add
                {
                    _transitionCommand.CanExecuteChanged += value;
                }
                remove
                {
                    _transitionCommand.CanExecuteChanged -= value;
                }
            }

            /// <summary>
            /// Occurs when the transition completes.
            /// </summary>
            [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
            public event EventHandler ExecuteCompleted
            {
                add
                {
                    _transitionCommand.ExecuteCompleted += value;
                }
                remove
                {
                    _transitionCommand.ExecuteCompleted -= value;
                }
            }

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
                => _transitionCommand.CanExecute(parameter);

            /// <summary>
            /// Triggers a state transition on the underlying <see cref="InteractiveViewModel"/>.
            /// </summary>
            /// <param name="parameter">
            /// An optional parameter to provide additional data for the command to process.
            /// </param>
            public void Execute(object parameter)
                => _transitionCommand.Execute(parameter);

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
                => _transitionCommand.BindTo(states);

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
                => _transitionCommand.BindTo(states);
        }

        /// <summary>
        /// Represents an <see cref="ICommand"/> that enqueues a state change on the
        /// underlying <see cref="InteractiveViewModel"/>.
        /// </summary>
        protected sealed class EnqueuingTransitionCommand
            : ICommand
        {
            private interface IEnqueuingTransitionCommand
                : ICommand
            {
                [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
                event EventHandler ExecuteCompleted;

                ICommand BindTo(IEnumerable<string> states);

                ICommand BindTo(params string[] states);
            }

            private class CurrentEnqueuingTransitionCommand
                : IEnqueuingTransitionCommand
            {
                private readonly InteractiveViewModel _interactiveViewModel;
                private readonly string _destinationState;

                internal CurrentEnqueuingTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState)
                {
                    if (interactiveViewModel == null)
                        throw new ArgumentNullException(nameof(interactiveViewModel));
                    if (destinationState == null)
                        throw new ArgumentNullException(nameof(destinationState));

                    _interactiveViewModel = interactiveViewModel;
                    _destinationState = destinationState;
                }

                event EventHandler ICommand.CanExecuteChanged { add { } remove { } }

                /// <summary>
                /// Occurs when the transition completes.
                /// </summary>
                [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
                public event EventHandler ExecuteCompleted;

                public bool CanExecute(object parameter)
                    => true;

                public async void Execute(object parameter)
                {
                    await _interactiveViewModel.EnqueueTransitionToAsync(_destinationState, parameter);
                    ExecuteCompleted?.Invoke(this, EventArgs.Empty);
                }

                public ICommand BindTo(IEnumerable<string> states)
                    => _interactiveViewModel.BindCommand(this, states);

                public ICommand BindTo(params string[] states)
                    => BindTo(states.AsEnumerable());
            }

            [Obsolete("This is an obsolete implementation. Use CurrentEnqueuingTransitionCommand instead.")]
            private sealed class ObsoleteEnqueuingTransitionCommand
                : IEnqueuingTransitionCommand
            {
                private readonly InteractiveViewModel _interactiveViewModel;
                private readonly string _destinationState;
                private readonly Action<ErrorContext> _errorHandler;

                internal ObsoleteEnqueuingTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState, Action<ErrorContext> errorHandler)
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
                [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
                public event EventHandler ExecuteCompleted;

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

                    ExecuteCompleted?.Invoke(this, EventArgs.Empty);
                }

                public ICommand BindTo(IEnumerable<string> states)
                    => _interactiveViewModel.BindCommand(this, states);

                public ICommand BindTo(params string[] states)
                    => BindTo(states.AsEnumerable());
            }

            private readonly IEnqueuingTransitionCommand _enqueuingTransitionCommand;

            /// <summary>
            /// Creates a new <see cref="EnqueuingTransitionCommand"/> instance.
            /// </summary>
            /// <param name="interactiveViewModel">
            /// The <see cref="InteractiveViewModel"/> unto which to enqueue a transition.
            /// </param>
            /// <param name="destinationState">
            /// The name of the state to enqueue the transition to.
            /// </param>
            internal EnqueuingTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState)
            {
                if (interactiveViewModel == null)
                    throw new ArgumentNullException(nameof(interactiveViewModel));
                if (destinationState == null)
                    throw new ArgumentNullException(nameof(destinationState));

                _enqueuingTransitionCommand = new CurrentEnqueuingTransitionCommand(interactiveViewModel, destinationState);
            }

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
            [Obsolete("This constructor takes an error handler callback which is no longer supported. Consider using an constructor overload that does not require an error handler callback.")]
            internal EnqueuingTransitionCommand(InteractiveViewModel interactiveViewModel, string destinationState, Action<ErrorContext> errorHandler)
            {
                if (interactiveViewModel == null)
                    throw new ArgumentNullException(nameof(interactiveViewModel));
                if (destinationState == null)
                    throw new ArgumentNullException(nameof(destinationState));

                _enqueuingTransitionCommand = new ObsoleteEnqueuingTransitionCommand(interactiveViewModel, destinationState, errorHandler);
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                add
                {
                    _enqueuingTransitionCommand.CanExecuteChanged += value;
                }
                remove
                {
                    _enqueuingTransitionCommand.CanExecuteChanged -= value;
                }
            }

            /// <summary>
            /// Occurs when the transition completes.
            /// </summary>
            [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
            public event EventHandler ExecuteCompleted
            {
                add
                {
                    _enqueuingTransitionCommand.ExecuteCompleted += value;
                }
                remove
                {
                    _enqueuingTransitionCommand.ExecuteCompleted -= value;
                }
            }

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
                => _enqueuingTransitionCommand.CanExecute(parameter);

            /// <summary>
            /// Enqueues a state transition on the underlying <see cref="InteractiveViewModel"/>.
            /// </summary>
            /// <param name="parameter">
            /// An optional parameter to provide additional data for the command to process.
            /// </param>
            public void Execute(object parameter)
            {
                _enqueuingTransitionCommand.Execute(parameter);
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
                => _enqueuingTransitionCommand.BindTo(states);

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
                => _enqueuingTransitionCommand.BindTo(states);
        }

        private sealed class ActionStatesCollection
            : IDictionary<string, ViewModelState>
        {
            private bool _isFrozen = false;
            private readonly Dictionary<string, ViewModelState> _actionStates = new Dictionary<string, ViewModelState>(StateStringComparer);

            public ViewModelState this[string stateName]
            {
                get
                {
                    return _actionStates[stateName];
                }
                set
                {
                    _EnsureIsNotFrozen();
                    throw new NotImplementedException();
                }
            }

            public int Count
                => _actionStates.Count;

            public bool IsReadOnly
                => _isFrozen;

            public ICollection<string> Keys
                => _actionStates.Keys;

            public ICollection<ViewModelState> Values
                => _actionStates.Values;

            public void Add(string stateName, ViewModelState state)
            {
                _EnsureIsNotFrozen();
                _actionStates.Add(stateName, state);
            }
            void ICollection<KeyValuePair<string, ViewModelState>>.Add(KeyValuePair<string, ViewModelState> item)
            {
                _EnsureIsNotFrozen();
                ((ICollection<KeyValuePair<string, ViewModelState>>)_actionStates).Add(item);
            }

            public void Clear()
            {
                _EnsureIsNotFrozen();
                _actionStates.Clear();
            }

            public bool Contains(KeyValuePair<string, ViewModelState> item)
                => _actionStates.Contains(item);

            public bool ContainsKey(string stateName)
                => _actionStates.ContainsKey(stateName);

            public bool TryGetValue(string stateName, out ViewModelState state)
                => _actionStates.TryGetValue(stateName, out state);

            void ICollection<KeyValuePair<string, ViewModelState>>.CopyTo(KeyValuePair<string, ViewModelState>[] array, int arrayIndex)
            {
                ((ICollection<KeyValuePair<string, ViewModelState>>)_actionStates).CopyTo(array, arrayIndex);
            }

            public bool Remove(string stateName)
            {
                _EnsureIsNotFrozen();
                return _actionStates.Remove(stateName);
            }
            bool ICollection<KeyValuePair<string, ViewModelState>>.Remove(KeyValuePair<string, ViewModelState> item)
            {
                _EnsureIsNotFrozen();
                return ((ICollection<KeyValuePair<string, ViewModelState>>)_actionStates).Remove(item);
            }


            public IEnumerator<KeyValuePair<string, ViewModelState>> GetEnumerator()
                => _actionStates.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public void Freze()
            {
                _isFrozen = true;
            }

            private void _EnsureIsNotFrozen()
            {
                if (_isFrozen)
                    throw new InvalidOperationException("The action states are already in use and can no longer be configured. Configure all action states before transitioning to any state (quiet or action).");
            }
        }

        private string _state = null;
        private bool _isInActionState = false;
        private string _lastEnqueuedState = null;
        private Task<string> _lastTransitionTask = Task.FromResult<string>(null);
        private readonly CancellationCommand _cancelCommand = new CancellationCommand();
        private readonly ActionStatesCollection _actionStates = new ActionStatesCollection();

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
        /// Gets a <see cref="Task{String}"/> that represents the current transition. The
        /// <see cref="Task{String}"/> will complete once the <see cref="InteractiveViewModel"/>
        /// has reached a quiet state and the <see cref="Task{String}"/> itself will provide
        /// the name of the state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is useful to await transitions on an <see cref="InteractiveViewModel"/>
        /// when the awaiting object does not have access to the <see cref="Task{String}"/>
        /// returned by one of the transition methods.
        /// </para>
        /// <para>
        /// Sometimes it is impossible as the <see cref="InteractiveViewModel"/> may
        /// have an action state as the initial state, thus the result of the transition
        /// is lost unless it is captured and exposed by user code.
        /// </para>
        /// </remarks>
        public Task<string> Transition
            => _lastTransitionTask;

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
            _actionStates.Add(
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
            _actionStates.Add(
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
            _actionStates.Add(
                name,
                new ViewModelState(name, cancelableAsyncAction));
        }

        /// <summary>
        /// Transitions to the initial state. This method should be called from the
        /// constructor instead of calling <see cref="TransitionToAsync(string)"/> or
        /// any of its overloads.
        /// </summary>
        /// <param name="initialActionStateName">
        /// The name of the initial state.
        /// </param>
        /// <param name="canceledState">
        /// The name of the state to transition to in case the initial action is canceled.
        /// </param>
        /// <param name="faultedState">
        /// The name of the state to transition to in case the initial action fails.
        /// </param>
        /// <param name="parameter">
        /// A parameter containing additional data that an action state might use.
        /// </param>
        protected void TransitionToInitialActionState(string initialActionStateName, string canceledState, string faultedState, object parameter)
        {
            if (!_actionStates.ContainsKey(initialActionStateName))
                throw new ArgumentException("The initial state is not an action state. Use the TransitionToInitialQuietState method to transition to an initial quiet state.", nameof(initialActionStateName));
            if (_actionStates.ContainsKey(canceledState))
                throw new ArgumentException("The canceled state must be a quiet state.", nameof(canceledState));
            if (_actionStates.ContainsKey(faultedState))
                throw new ArgumentException("The faulted state must be a quiet state.", nameof(faultedState));
            if (_state != null)
                throw new InvalidOperationException("Cannot transition to an initial state if the view model has already transitioned to a state.");

            Action transitionAction =
                async delegate
                {
                    var transitionTask = TransitionToAsync(initialActionStateName, parameter);
                    try
                    {
                        await transitionTask;
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception);
                        if (transitionTask.IsCanceled)
                            State = canceledState;
                        else
                            State = faultedState;
                    }
                };
            transitionAction.Invoke();
        }
        /// <summary>
        /// Transitions to the initial state. This method should be called from the
        /// constructor instead of calling <see cref="TransitionToAsync(string)"/> or
        /// any of its overloads.
        /// </summary>
        /// <param name="initialActionStateName">
        /// The name of the initial state.
        /// </param>
        /// <param name="canceledState">
        /// The name of the state to transition to in case the initial action is canceled.
        /// </param>
        /// <param name="faultedState">
        /// The name of the state to transition to in case the initial action fails.
        /// </param>
        protected void TransitionToInitialActionState(string initialActionStateName, string canceledState, string faultedState)
            => TransitionToInitialActionState(initialActionStateName, canceledState, faultedState, null);
        /// <summary>
        /// Transitions to the initial state. This method should be called from the
        /// constructor instead of calling <see cref="TransitionToAsync(string)"/> or
        /// any of its overloads.
        /// </summary>
        /// <param name="initialQuietStateName">
        /// The name of the initial state.
        /// </param>
        protected void TransitionToInitialQuietState(string initialQuietStateName)
        {
            _actionStates.Freze();
            if (_actionStates.ContainsKey(initialQuietStateName))
                throw new ArgumentException("Having an initial action state may make the view model behave unexpectedly if the operation would be canceled or faulted.", nameof(initialQuietStateName));
            if (_state != null)
                throw new InvalidOperationException("Cannot transition to an initial state if the view model has already transitioned to a state.");

            State = initialQuietStateName;
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
        protected Task<string> TransitionToAsync(string state, object parameter) => _lastTransitionTask = new Func<Task<string>>(async delegate
        {
            _actionStates.Freze();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (_isInActionState)
                throw new InvalidOperationException("Cannot transition to a different state while in an action state.");

            var nextState = state;
            try
            {
                _isInActionState = true;

                ViewModelState viewModelState;
                while (_actionStates.TryGetValue(nextState, out viewModelState))
                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        _cancelCommand.CancellationTokenSource = cancellationTokenSource;
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
            finally
            {
                _cancelCommand.CanExecuteCommand = false;
                _cancelCommand.CancellationTokenSource = null;
                _isInActionState = false;
            }

            State = nextState;
            return nextState;
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
        protected Task<string> TransitionToAsync(string state)
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
        protected Task<string> EnqueueTransitionToAsync(string state, object parameter)
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
                _lastTransitionTask = new Func<Task<string>>(
                    async delegate
                    {
                        await Task.Yield();
                        await previousTransitionTask;
                        return await TransitionToAsync(state, parameter);
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
        protected Task<string> EnqueueTransitionToAsync(string state)
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
            => new TransitionCommand(this, destinationState);
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
        [Obsolete(@"Error handling is no longer supported through a separate callback. Exceptions must be treated in callbacks associated with action states.

Allowing InteractiveViewModels to not transition to any state (because of an uncaught exception) and remain ""stuck"" in an action state leads to ""partial"" transitions and inconsistencies when using commands to trigger transitions.")]
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
            => new EnqueuingTransitionCommand(this, destinationState);

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
        [Obsolete(@"Error handling is no longer supported through a separate callback. Exceptions must be treated in callbacks associated with action states.

Allowing InteractiveViewModels to not transition to any state (because of an uncaught exception) and remain ""stuck"" in an action state leads to ""partial"" transitions and inconsistencies when using commands to trigger transitions.")]
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