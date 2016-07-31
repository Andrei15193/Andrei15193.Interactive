using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class InteractiveViewModelTests
    {
        private const string InitialState = "initialTestState";
        private const string ActionState = "actionTestState";
        private const string FollowUpActionState = "followUpActionTestState";
        private const string SecondFollowUpActionState = "secondFollowUpActionTestState";
        private const string DestinationState = "destinationTestState";
        private const string FinalState = "finalTestState";

        private sealed class MockInteractiveViewModel<TDataModel>
            : InteractiveViewModel<TDataModel>
        {
            public MockInteractiveViewModel(TDataModel dataModel)
                : base(dataModel)
            {
            }

            new public void CreateActionState(string name, Action<ActionStateContext> action)
            {
                base.CreateActionState(name, action);
            }
            new public void CreateActionState(string name, Func<ActionStateContext, Task> asyncAction)
            {
                base.CreateActionState(name, asyncAction);
            }
            new public void CreateActionState(string name, Func<ActionStateContext, CancellationToken, Task> cancelableAsyncAction)
            {
                base.CreateActionState(name, cancelableAsyncAction);
            }

            new public Task TransitionToAsync(string state, object parameter)
                => base.TransitionToAsync(state, parameter);
            new public Task TransitionToAsync(string state)
                => base.TransitionToAsync(state);
            new public Task EnqueueTransitionToAsync(string state, object parameter)
                => base.EnqueueTransitionToAsync(state, parameter);
            new public Task EnqueueTransitionToAsync(string state)
                => base.EnqueueTransitionToAsync(state);

            new public ICommand BindCommand(ICommand command, IEnumerable<string> states)
                => base.BindCommand(command, states);
            new public ICommand BindCommand(ICommand command, params string[] states)
                => base.BindCommand(command, states);

            new public ICommand GetTransitionCommand(string destinationState)
                => base.GetTransitionCommand(destinationState);
            [Obsolete(@"Error handling is no longer supported through a separate callback. Exceptions must be treated in callbacks associated with action states.

Allowing InteractiveViewModels to not transition to any state (because of an uncaught exception) and remain ""stuck"" in an action state leads to ""partial"" transitions and inconsistencies when using commands to trigger transitions.")]
            new public ICommand GetTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
                => base.GetTransitionCommand(destinationState, errorHandler);

            [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
            public ICommand GetTransitionCommand(string destinationState, ManualResetEventSlim completionEvent)
            {
                var transitionCommand = base.GetTransitionCommand(destinationState);
                transitionCommand.ExecuteCompleted +=
                    delegate
                    {
                        completionEvent.Set();
                    };

                return transitionCommand;
            }
            [Obsolete("This even is now obsolete. Use the Transition property exposed by InteractiveViewModels instead.")]
            public ICommand GetTransitionCommand(string destinationState, Action<ErrorContext> errorHandler, ManualResetEventSlim completionEvent)
            {
                var transitionCommand = base.GetTransitionCommand(destinationState, errorHandler);
                transitionCommand.ExecuteCompleted +=
                    delegate
                    {
                        completionEvent.Set();
                    };

                return transitionCommand;
            }

            new public ICommand GetEnqueuingTransitionCommand(string destinationState)
                => base.GetEnqueuingTransitionCommand(destinationState);
            [Obsolete(@"Error handling is no longer supported through a separate callback. Exceptions must be treated in callbacks associated with action states.

Allowing InteractiveViewModels to not transition to any state (because of an uncaught exception) and remain ""stuck"" in an action state leads to ""partial"" transitions and inconsistencies when using commands to trigger transitions.")]
            new public ICommand GetEnqueuingTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
                => base.GetEnqueuingTransitionCommand(destinationState, errorHandler);
        }

        private sealed class BoundMockInteractiveViewModel
            : InteractiveViewModel<object>
        {
            public BoundMockInteractiveViewModel()
                : base(new object())
            {
                GoToActionStateCommand = GetTransitionCommand(ActionState)
                    .BindTo(InitialState);
                GoToDestinationStateCommand = GetTransitionCommand(DestinationState)
                    .BindTo(ActionState);

                Task.Run(() => TransitionToAsync(InitialState)).Wait();
            }

            public ICommand GoToActionStateCommand { get; }
            public ICommand GoToDestinationStateCommand { get; }
        }

        private sealed class MockCommand
            : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            public MockCommand(Func<object, bool> canExecute = null, Action<object> execute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public void RaiseCanExecuteChanged()
                => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
                => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter)
                => _execute?.Invoke(parameter);
        }

        private object DataModel { get; set; }
        private MockInteractiveViewModel<object> InteractiveViewModel { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            DataModel = new object();
            InteractiveViewModel = new MockInteractiveViewModel<object>(DataModel);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            InteractiveViewModel = null;
            DataModel = null;
        }

        [TestMethod]
        public void TestContextGetsSetThroughConstructor()
        {
            Assert.AreSame(DataModel, InteractiveViewModel.Context.DataModel);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestContextCannotBeNull()
        {
            new InteractiveViewModel<object>(null);
        }

        [TestMethod]
        public async Task TestTransitioningToAStateUpdatesTheStateProperty()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            Assert.AreEqual(InitialState, InteractiveViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestCannotTransitionToNullState()
        {
            await InteractiveViewModel.TransitionToAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestCannotRetrieveStateIfItHasNotBeenSet()
        {
            var state = InteractiveViewModel.State;
        }

        [TestMethod]
        public async Task TestTransitioningToActionStateDoesNotAllowOtherTransitionsUntilItHasReachedADestinationState()
        {
            var invocationCount = 0;

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                await InteractiveViewModel.TransitionToAsync(InitialState);

                InteractiveViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        invocationCount++;
                        actionContext.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                Assert.AreEqual(ActionState, InteractiveViewModel.State, ignoreCase: false);

                completeActionStateEvent.Set();
                await actionStateTransitionTask;

                Assert.AreEqual(DestinationState, InteractiveViewModel.State, ignoreCase: false);
            }

            Assert.AreEqual(1, invocationCount);
        }

        [TestMethod]
        public async Task TestViewModelGetsSetOnActionContext()
        {
            var invocationCount = 0;

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                await InteractiveViewModel.TransitionToAsync(InitialState);

                InteractiveViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        invocationCount++;
                        Assert.AreSame(InteractiveViewModel, actionContext.InteractiveViewModel);
                        actionContext.NextState = DestinationState;

                        return Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                completeActionStateEvent.Set();

                await actionStateTransitionTask;
            }

            Assert.AreEqual(1, invocationCount);
        }

        [TestMethod]
        public async Task TestSourceStateIsSetOnActionContext()
        {
            var invocationCount = 0;

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                await InteractiveViewModel.TransitionToAsync(InitialState);

                InteractiveViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        invocationCount++;
                        Assert.AreEqual(InitialState, actionContext.PreviousState);
                        actionContext.NextState = DestinationState;

                        return Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                completeActionStateEvent.Set();

                await actionStateTransitionTask;
            }

            Assert.AreEqual(1, invocationCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task TestNotSettingADestinationStateThrowsException()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            InteractiveViewModel.CreateActionState(
                ActionState,
                actionContext =>
                {
                });

            await InteractiveViewModel.TransitionToAsync(ActionState);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task CannotTransitionToAnotherStateWhileInAnActionState()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var exceptionThrownEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        actionContext.NextState = DestinationState;
                        return Task.Factory.StartNew(exceptionThrownEvent.Wait);
                    });

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                try
                {
                    await InteractiveViewModel.TransitionToAsync(DestinationState);
                }
                finally
                {
                    exceptionThrownEvent.Set();
                    await actionStateTransitionTask;
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task TestCannotTransitionThroughPropertyChangedEventHandlerWhileInActionState()
        {
            var raiseCount = 0;
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var exceptionThrownEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        actionContext.NextState = DestinationState;
                        return Task.Factory.StartNew(exceptionThrownEvent.Wait);
                    });

                PropertyChangedEventHandler eventHandler = null;
                eventHandler =
                    (sender, e) =>
                    {
                        InteractiveViewModel.PropertyChanged -= eventHandler;
                        raiseCount++;
                        try
                        {
                            Task.Run(() => InteractiveViewModel.TransitionToAsync(DestinationState)).Wait();
                        }
                        catch (AggregateException aggregateException)
                        {
                            throw aggregateException.InnerException;
                        }
                    };
                InteractiveViewModel.PropertyChanged += eventHandler;

                await InteractiveViewModel.TransitionToAsync(ActionState);
            }

            Assert.AreEqual(1, raiseCount);
        }

        [TestMethod]
        public async Task TestTransitionFromPropertyChangedEventHandlerAfterActionStateCompleted()
        {
            var raiseCount = 0;
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var reachedActionStateEvent = new ManualResetEventSlim(false))
            using (var continueActionStateTransitionEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        reachedActionStateEvent.Set();
                        actionContext.NextState = DestinationState;
                        return Task.Factory.StartNew(continueActionStateTransitionEvent.Wait);
                    });

                PropertyChangedEventHandler eventHandler = null;
                eventHandler =
                    (sender, e) =>
                    {
                        raiseCount++;
                        InteractiveViewModel.PropertyChanged -= eventHandler;
                        Task.Run(() => InteractiveViewModel.TransitionToAsync(FinalState)).Wait();
                    };

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                await Task.Factory.StartNew(reachedActionStateEvent.Wait);
                InteractiveViewModel.PropertyChanged += eventHandler;

                continueActionStateTransitionEvent.Set();
                await actionStateTransitionTask;
            }

            Assert.AreEqual(FinalState, InteractiveViewModel.State, ignoreCase: false);
            Assert.AreEqual(1, raiseCount);
        }

        [TestMethod]
        public async Task TestChainingActionStates()
        {
            var stateChanges = new List<string>();
            InteractiveViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateChanges.Add(InteractiveViewModel.State);
                };
            await InteractiveViewModel.TransitionToAsync(InitialState);

            InteractiveViewModel.CreateActionState(
                ActionState,
                context =>
                {
                    context.NextState = FollowUpActionState;
                });

            InteractiveViewModel.CreateActionState(
                FollowUpActionState,
                context =>
                {
                    context.NextState = DestinationState;
                });

            await InteractiveViewModel.TransitionToAsync(ActionState);

            Assert.IsTrue(
                new[]
                {
                    InitialState,
                    ActionState,
                    FollowUpActionState,
                    DestinationState
                }.SequenceEqual(stateChanges));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task TestCannotTransitionIntoADifferentStateFromTheMiddleOfAChainOfActionStats()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            InteractiveViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        if (FollowUpActionState.Equals(InteractiveViewModel.State, StringComparison.Ordinal))
                            try
                            {
                                Task.Run(() => InteractiveViewModel.TransitionToAsync(DestinationState)).Wait();
                            }
                            catch (AggregateException aggregateException)
                            {
                                throw aggregateException.InnerException;
                            }
                };

            using (var continueFollowUpActionEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        context.NextState = FollowUpActionState;
                    });

                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(continueFollowUpActionEvent.Wait);
                    });

                await InteractiveViewModel.TransitionToAsync(ActionState);
            }
        }

        [TestMethod]
        public async Task TestCancelableActionStatesReceiveACancelableCancellationToken()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            InteractiveViewModel.CreateActionState(
                ActionState,
                (context, cancellationToken) =>
                {
                    context.NextState = DestinationState;
                    Assert.IsTrue(cancellationToken.CanBeCanceled);
                    return Task.FromResult<object>(null);
                });

            await InteractiveViewModel.TransitionToAsync(ActionState);
        }

        [TestMethod]
        public async Task TestWhileInACancelableActionStateTheCancelCommandCanBeExecuted()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var completeActionEvent = new ManualResetEventSlim(false))
            using (var reachedActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        reachedActionStateEvent.Set();
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = InteractiveViewModel.TransitionToAsync(ActionState);

                await Task.Factory.StartNew(reachedActionStateEvent.Wait);
                Assert.IsTrue(InteractiveViewModel.CancelCommand.CanExecute(null));

                completeActionEvent.Set();
                await actionStateTranstionTask;
            }

            Assert.IsFalse(InteractiveViewModel.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestWhileInAnActionStateThatCannotBeCanceledTheCancelComamndCannotBeExecuted()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = InteractiveViewModel.TransitionToAsync(ActionState);

                Assert.IsFalse(InteractiveViewModel.CancelCommand.CanExecute(null));

                completeActionEvent.Set();
                await actionStateTranstionTask;
            }

            Assert.IsFalse(InteractiveViewModel.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestChainedCancelableAndNonCancelableActionStatesEnableAndDisableCancelCommand()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var reachedActionStateEvent = new ManualResetEventSlim(false))
            using (var transitionedToFollowUpStateEvent = new ManualResetEventSlim(false))
            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                            if (FollowUpActionState.Equals(InteractiveViewModel.State, StringComparison.Ordinal))
                                transitionedToFollowUpStateEvent.Set();
                    };

                InteractiveViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        reachedActionStateEvent.Set();
                        context.NextState = FollowUpActionState;
                        return Task.Factory
                            .StartNew(completeActionEvent.Wait)
                            .ContinueWith(task => completeActionEvent.Reset());
                    });
                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = InteractiveViewModel.TransitionToAsync(ActionState);

                await Task.Factory.StartNew(reachedActionStateEvent.Wait);
                Assert.IsTrue(InteractiveViewModel.CancelCommand.CanExecute(null));
                completeActionEvent.Set();

                transitionedToFollowUpStateEvent.Wait();

                Assert.IsFalse(InteractiveViewModel.CancelCommand.CanExecute(null));
                completeActionEvent.Set();

                await actionStateTranstionTask;
            }

            Assert.IsFalse(InteractiveViewModel.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestCanExecuteIsRaisedWhenCancelCommandBecomesAvailableOrUnavailable()
        {
            var raiseCount = 0;
            InteractiveViewModel.CancelCommand.CanExecuteChanged += delegate { raiseCount++; };

            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var reachedActionStateEvent = new ManualResetEventSlim(false))
            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        reachedActionStateEvent.Set();
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = InteractiveViewModel.TransitionToAsync(ActionState);

                await Task.Factory.StartNew(reachedActionStateEvent.Wait);
                Assert.AreEqual(1, raiseCount);

                completeActionEvent.Set();
                await actionStateTranstionTask;
            }

            Assert.AreEqual(2, raiseCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task TestTryingToExecuteCancelCommandWhenItIsUnavailableThrowsException()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            Assert.IsFalse(InteractiveViewModel.CancelCommand.CanExecute(null));
            InteractiveViewModel.CancelCommand.Execute(null);
        }

        [TestMethod]
        public async Task TestExecutingTheCancelCommandSignalsTheCancellationToken()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            using (var cancelSignledEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async (context, cancellationToken) =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(cancelSignledEvent.Wait);
                        Assert.IsTrue(cancellationToken.IsCancellationRequested);
                    });

                var actionStateTranstitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                InteractiveViewModel.CancelCommand.Execute(null);

                cancelSignledEvent.Set();
                await actionStateTranstitionTask;
            }
        }

        [TestMethod]
        public async Task TestBindingACommandMakesItAvailableInTheSelectedStates()
        {
            var boundCommand = InteractiveViewModel.BindCommand(new MockCommand(), ActionState);

            await InteractiveViewModel.TransitionToAsync(InitialState);
            Assert.IsFalse(boundCommand.CanExecute(null));

            await InteractiveViewModel.TransitionToAsync(ActionState);
            Assert.IsTrue(boundCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestCanExecuteChangedIsRaisedWhenTheViewModelMovesInAndOutOfBoundCommands()
        {
            var raiseCount = 0;
            var boundCommand = InteractiveViewModel.BindCommand(new MockCommand(), ActionState, DestinationState);
            boundCommand.CanExecuteChanged +=
                (sender, e) => raiseCount++;

            await InteractiveViewModel.TransitionToAsync(InitialState);
            Assert.IsFalse(boundCommand.CanExecute(null));
            Assert.AreEqual(0, raiseCount);

            await InteractiveViewModel.TransitionToAsync(ActionState);
            Assert.IsTrue(boundCommand.CanExecute(null));
            Assert.AreEqual(1, raiseCount);

            await InteractiveViewModel.TransitionToAsync(DestinationState);
            Assert.IsTrue(boundCommand.CanExecute(null));
            Assert.AreEqual(1, raiseCount);

            await InteractiveViewModel.TransitionToAsync(FinalState);
            Assert.IsFalse(boundCommand.CanExecute(null));
            Assert.AreEqual(2, raiseCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCannotBindNullComamnd()
        {
            InteractiveViewModel.BindCommand(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCannotBindNullStatesCollection()
        {
            InteractiveViewModel.BindCommand(new MockCommand(), null);
        }

        [TestMethod]
        public async Task TestExecutingTransitionCommandTranstionsTheViewModelInTheTargetedState()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            var transitionCommand = InteractiveViewModel.GetTransitionCommand(DestinationState);

            using (var stateChangedEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                            stateChangedEvent.Set();
                    };

                transitionCommand.Execute(null);
                stateChangedEvent.Wait();

                Assert.AreEqual(DestinationState, InteractiveViewModel.State);
            }
        }

        [TestMethod]
        public void TestBoundTransitionCommandIsAvailableOnlyInStatesToWhichItHasBeenBound()
        {
            var interactiveViewModel = new BoundMockInteractiveViewModel();

            Assert.IsTrue(interactiveViewModel.GoToActionStateCommand.CanExecute(false));
            Assert.IsFalse(interactiveViewModel.GoToDestinationStateCommand.CanExecute(false));

            using (var stateChangedEvent = new ManualResetEventSlim(false))
            {
                interactiveViewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(interactiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                            stateChangedEvent.Set();
                    };

                interactiveViewModel.GoToActionStateCommand.Execute(null);
                stateChangedEvent.Wait();
                stateChangedEvent.Reset();

                Assert.IsFalse(interactiveViewModel.GoToActionStateCommand.CanExecute(false));
                Assert.IsTrue(interactiveViewModel.GoToDestinationStateCommand.CanExecute(false));

                interactiveViewModel.GoToDestinationStateCommand.Execute(null);
                stateChangedEvent.Wait();
            }

            Assert.IsFalse(interactiveViewModel.GoToActionStateCommand.CanExecute(false));
            Assert.IsFalse(interactiveViewModel.GoToDestinationStateCommand.CanExecute(false));
        }

        [TestMethod]
        public async Task TestInitialStateCanBeAnActionState()
        {
            using (var continueEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    InitialState,
                    context =>
                    {
                        context.NextState = FinalState;
                        return Task.Factory.StartNew(continueEvent.Wait);
                    });

                var initialStateTransitionTask = InteractiveViewModel.TransitionToAsync(InitialState);
                Assert.AreEqual(InitialState, InteractiveViewModel.State, ignoreCase: false);

                continueEvent.Set();
                await initialStateTransitionTask;
            }

            Assert.AreEqual(FinalState, InteractiveViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestTransitioningToAFaultingStateWithTransitionCommandWillGoThroughTheErrorHandler()
        {
            using (var transitionCompletedEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        throw new Exception();
                    });

                var transitionCommand =
                    InteractiveViewModel.GetTransitionCommand(
                        ActionState,
                        context =>
                        {
                            context.NextState = FinalState;
                        },
                        transitionCompletedEvent);

                transitionCommand.Execute(null);

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);

                Assert.AreEqual(FinalState, InteractiveViewModel.State, ignoreCase: false);
            }
        }

        [TestMethod]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestTransitioningFromAFaultedStateToAnActionStateThroughTheErrorHandler()
        {
            var stateChanges = new List<string>();
            InteractiveViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateChanges.Add(InteractiveViewModel.State);
                };
            using (var transitionCompletedEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        throw new Exception();
                    });
                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = FinalState;
                    });

                var transitionCommand =
                    InteractiveViewModel.GetTransitionCommand(
                        ActionState,
                        context =>
                        {
                            context.NextState = FollowUpActionState;
                        },
                        transitionCompletedEvent);

                transitionCommand.Execute(null);

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);
            }

            Assert.IsTrue(
                new[]
                {
                    ActionState,
                    FollowUpActionState,
                    FinalState
                }.SequenceEqual(stateChanges));
        }

        [TestMethod]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestExceptionThatIsThrownIsRetrievedThroughErrorContext()
        {
            var expectedException = new Exception();
            Exception actualException = null;
            using (var transitionCompletedEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        throw expectedException;
                    });

                var transitionCommand =
                    InteractiveViewModel.GetTransitionCommand(
                        ActionState,
                        context =>
                        {
                            context.NextState = FinalState;
                            actualException = context.Exception;
                        },
                        transitionCompletedEvent);

                transitionCommand.Execute(null);

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);
            }

            Assert.AreSame(expectedException, actualException);
        }

        [TestMethod]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestCancelingAnActionStateSetsIsCanceledAsTrueOnErrorContext()
        {
            var isCanceled = false;
            using (var cancelCommandExecutedEvent = new ManualResetEventSlim(false))
            using (var transitionCompletedEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async (context, cancellationToken) =>
                    {
                        await Task.Factory.StartNew(cancelCommandExecutedEvent.Wait);
                        cancellationToken.ThrowIfCancellationRequested();
                    });

                var transitionCommand =
                    InteractiveViewModel.GetTransitionCommand(
                        ActionState,
                        context =>
                        {
                            context.NextState = FinalState;
                            isCanceled = context.IsCanceled;
                        },
                        transitionCompletedEvent);

                transitionCommand.Execute(null);

                InteractiveViewModel.CancelCommand.Execute(null);
                cancelCommandExecutedEvent.Set();

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);
            }

            Assert.IsTrue(isCanceled);
        }

        [TestMethod]
        public async Task TestEnqueueTransitionToTransitionsToStateIfNotInAnActionState()
        {
            await InteractiveViewModel.TransitionToAsync(InitialState);

            await InteractiveViewModel.EnqueueTransitionToAsync(DestinationState);

            Assert.AreEqual(DestinationState, InteractiveViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        public async Task TestEnqueueTransitionToTransitionsToStateAfterItHasCompletedCurrentActionState()
        {
            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFinalStateTask = InteractiveViewModel.EnqueueTransitionToAsync(FinalState);

                completeActionStateEvent.Set();

                await Task.WhenAll(transitionToActionStateTask, transitionToFinalStateTask);

            }
            Assert.AreEqual(FinalState, InteractiveViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        public async Task TestEnqueueingTheSameStateTwiceWhileInAnActionStateWillTransitionOnlyOneAfterTheActionStateIsCompelted()
        {
            var stateTransitions = new List<string>();

            InteractiveViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateTransitions.Add(InteractiveViewModel.State);
                };

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFinalStateTask = Task.WhenAll(
                    InteractiveViewModel.EnqueueTransitionToAsync(FinalState),
                    InteractiveViewModel.EnqueueTransitionToAsync(FinalState));

                completeActionStateEvent.Set();

                await Task.WhenAll(transitionToActionStateTask, transitionToFinalStateTask);

            }

            Assert.IsTrue(
                new[]
                {
                    ActionState,
                    DestinationState,
                    FinalState,
                }.SequenceEqual(stateTransitions));
        }

        [TestMethod]
        public async Task TestEnqueueTransitionToEnqueuesStateOnlyIfItIsNotFirstInQueue()
        {
            var stateTransitions = new List<string>();

            InteractiveViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateTransitions.Add(InteractiveViewModel.State);
                };

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFinalStateTask = Task.WhenAll(
                    InteractiveViewModel.EnqueueTransitionToAsync(FinalState),
                    InteractiveViewModel.EnqueueTransitionToAsync(FollowUpActionState),
                    InteractiveViewModel.EnqueueTransitionToAsync(FinalState));

                completeActionStateEvent.Set();

                await Task.WhenAll(transitionToActionStateTask, transitionToFinalStateTask);

            }

            Assert.IsTrue(
                new[]
                {
                    ActionState,
                    DestinationState,
                    FinalState,
                    FollowUpActionState,
                    FinalState
                }.SequenceEqual(stateTransitions));
        }

        [TestMethod]
        public async Task TestEnqueuingTransitionToReturnsTaskThatWillBeCompletedOnceTheTransitionToThatStateIsCompelted()
        {
            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = FinalState;
                    });
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFollowUpStateTask = InteractiveViewModel.EnqueueTransitionToAsync(FollowUpActionState);

                Assert.IsFalse(transitionToFollowUpStateTask.IsCompleted);
                completeActionStateEvent.Set();

                await Task.WhenAll(transitionToActionStateTask, transitionToFollowUpStateTask);
            }
        }

        [TestMethod]
        public async Task TestTranstionToAsyncCompeltesBeforeAnyEnqueuedTransitions()
        {
            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                        completeActionStateEvent.Reset();
                    });
                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    async context =>
                    {
                        context.NextState = FinalState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFollowUpStateTask = InteractiveViewModel.EnqueueTransitionToAsync(FollowUpActionState);

                completeActionStateEvent.Set();
                await transitionToActionStateTask;

                Assert.IsFalse(transitionToFollowUpStateTask.IsCompleted);
                completeActionStateEvent.Set();

                await transitionToFollowUpStateTask;
            }
        }

        [TestMethod]
        public async Task TestTransitioningWithParameterSetsItToActionContext()
        {
            var parameter = new object();
            object actualParameter = null;
            InteractiveViewModel.CreateActionState(
                ActionState,
                context =>
                {
                    context.NextState = DestinationState;
                    actualParameter = context.Parameter;
                });

            await InteractiveViewModel.TransitionToAsync(ActionState, parameter);

            Assert.AreSame(parameter, actualParameter);
        }

        [TestMethod]
        public async Task TestEnqueingTransitionWithParameterSetsItToActionContext()
        {
            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                var parameter = new object();
                object actualParameter = null;
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = FollowUpActionState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        actualParameter = context.Parameter;
                    });

                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFollowUpActionStateTask = InteractiveViewModel.EnqueueTransitionToAsync(FollowUpActionState, parameter);

                completeActionStateEvent.Set();
                await Task.WhenAll(transitionToActionStateTask, transitionToFollowUpActionStateTask);

                Assert.AreSame(parameter, actualParameter);
            }
        }

        [TestMethod]
        public async Task TestUsingTransitionCommandToTransitioningWithParameterSetsItToActionContext()
        {
            var parameter = new object();
            object actualParameter = null;
            InteractiveViewModel.CreateActionState(
                ActionState,
                context =>
                {
                    context.NextState = DestinationState;
                    actualParameter = context.Parameter;
                });
            var transitionCommand = InteractiveViewModel.GetTransitionCommand(ActionState);
            transitionCommand.Execute(parameter);

            await InteractiveViewModel.Transition;

            Assert.AreSame(parameter, actualParameter);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestExecutingTransitionCommandWhileInActionStateThrowsException()
        {
            Exception exception = null;
            using (var continueActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(continueActionStateEvent.Wait);
                    });

                var transitionCommand = InteractiveViewModel.GetTransitionCommand(FinalState, errorContext => exception = errorContext.Exception);
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);

                transitionCommand.Execute(null);
                continueActionStateEvent.Set();
                await transitionToActionStateTask;

            }

            throw exception;
        }

        [TestMethod]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestErrorContextHasCanTransitionSetToFalseWhenTheViewModelIsInAnActiveState()
        {
            var canTransition = true;
            using (var continueActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(continueActionStateEvent.Wait);
                    });

                var transitionCommand = InteractiveViewModel.GetTransitionCommand(FinalState, context => canTransition = context.CanTransition);
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);

                transitionCommand.Execute(null);
                continueActionStateEvent.Set();
                await transitionToActionStateTask;
            }

            Assert.IsFalse(canTransition);
        }

        [TestMethod]
        [Obsolete("Tests obsolete functionality. While it is still in the library it must work properly.")]
        public async Task TestExceptionIsNotThrownWhenUsingEnqueuingTransitionCommandAndTheViewModelIsInAnActiveState()
        {
            Exception exception = null;
            using (var continueActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(continueActionStateEvent.Wait);
                    });

                var enqueuedTransitionCommand = InteractiveViewModel.GetEnqueuingTransitionCommand(
                    FinalState,
                    context => exception = context.Exception);
                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);

                enqueuedTransitionCommand.Execute(null);
                continueActionStateEvent.Set();
                await transitionToActionStateTask;

                await InteractiveViewModel.Transition;
            }

            Assert.IsNull(exception);
        }

        [TestMethod]
        public async Task TestEnqueuingTwoTransitionsWillMakeTheViewModelTransitionToThemInTheSameOrderTheQueuingWasDone()
        {
            var transitions = new List<string>();
            InteractiveViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        transitions.Add(InteractiveViewModel.State);
                };

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            using (var completeFollowUpStateEvent = new ManualResetEventSlim(false))
            using (var completeSecondFollowUpStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                InteractiveViewModel.CreateActionState(
                    FollowUpActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeFollowUpStateEvent.Wait);
                    });
                InteractiveViewModel.CreateActionState(
                    SecondFollowUpActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeSecondFollowUpStateEvent.Wait);
                    });

                var transitionToActionStateTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transitionToFollowUpStateTask = InteractiveViewModel.EnqueueTransitionToAsync(FollowUpActionState);
                var transitionToSecondFollowUpStateTask = InteractiveViewModel.EnqueueTransitionToAsync(SecondFollowUpActionState);

                completeActionStateEvent.Set();
                completeFollowUpStateEvent.Set();
                completeSecondFollowUpStateEvent.Set();

                await Task.WhenAll(transitionToActionStateTask, transitionToFollowUpStateTask, transitionToSecondFollowUpStateTask);
            }

            Assert.IsTrue(
                transitions.SequenceEqual(
                    new[]
                    {
                        ActionState,
                        DestinationState,
                        FollowUpActionState,
                        DestinationState,
                        SecondFollowUpActionState,
                        DestinationState
                    }));
        }

        [TestMethod]
        public async Task TestAwaitingViewModelTransitionsCompletesAfterTheViewModelHasReachedAQuietState()
        {
            using (var compelteActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = FinalState;
                        await Task.Factory.StartNew(compelteActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transition = InteractiveViewModel.Transition;

                Assert.IsFalse(transition.IsCompleted);

                compelteActionStateEvent.Set();

                await Task.WhenAll(actionStateTransitionTask, transition);
            }
        }

        [TestMethod]
        public async Task TestAwaitingViewModelTransitionTaskProvidesTheNameOfTheStateThatTheViewModelHasFinallyTransitionedTo()
        {
            using (var compelteActionStateEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = FinalState;
                        await Task.Factory.StartNew(compelteActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);
                var transition = InteractiveViewModel.Transition;

                compelteActionStateEvent.Set();

                await Task.WhenAll(actionStateTransitionTask, transition);
                var result = await transition;

                Assert.AreEqual(InteractiveViewModel.State, result);
            }
        }

        [TestMethod]
        public async Task TestAwaitingViewModelTransitionTaskProvidesTheNameOfTheStateThatTheViewModelHasFinallyTransitionedToEvenWhenPerformingSubsequentTransitions()
        {
            await InteractiveViewModel.TransitionToAsync(ActionState);
            var transition = InteractiveViewModel.Transition;

            await InteractiveViewModel.TransitionToAsync(FollowUpActionState);

            var result = await transition;
            Assert.AreEqual(ActionState, result);
        }
    }
}