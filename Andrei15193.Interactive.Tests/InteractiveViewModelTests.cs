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
        private const string DestinationState = "destinationTestState";
        private const string FinalState = "finalTestState";

        private sealed class MockInteractiveViewModel<TDataModel>
            : InteractiveViewModel<TDataModel>
        {
            public MockInteractiveViewModel(TDataModel dataContext)
                : base(dataContext)
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

            new public Task TransitionToAsync(string state)
                => base.TransitionToAsync(state);
            new public Task EnqueueTransitionToAsync(string state)
                => base.EnqueueTransitionToAsync(state);

            new public ICommand BindCommand(ICommand command, IEnumerable<string> states)
                => base.BindCommand(command, states);
            new public ICommand BindCommand(ICommand command, params string[] states)
                => base.BindCommand(command, states);

            new public ICommand GetTransitionCommand(string destinationState)
                => base.GetTransitionCommand(destinationState);
            new public ICommand GetTransitionCommand(string destinationState, Action<ErrorContext> errorHandler)
                => base.GetTransitionCommand(destinationState, errorHandler);
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

        private object DataContext { get; set; }
        private MockInteractiveViewModel<object> InteractiveViewModel { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            DataContext = new object();
            InteractiveViewModel = new MockInteractiveViewModel<object>(DataContext);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            InteractiveViewModel = null;
            DataContext = null;
        }

        [TestMethod]
        public void TestContextGetsSetThroughConstructor()
        {
            Assert.AreSame(DataContext, InteractiveViewModel.DataContext);
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
                        raiseCount++;
                        InteractiveViewModel.PropertyChanged -= eventHandler;
                        Task.Run(() => InteractiveViewModel.TransitionToAsync(FinalState)).Wait();
                    };

                var actionStateTransitionTask = InteractiveViewModel.TransitionToAsync(ActionState);

                InteractiveViewModel.PropertyChanged += eventHandler;

                exceptionThrownEvent.Set();
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
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = InteractiveViewModel.TransitionToAsync(ActionState);

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

            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                InteractiveViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = InteractiveViewModel.TransitionToAsync(ActionState);

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
                        });

                transitionCommand.Execute(transitionCompletedEvent);

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);

                Assert.AreEqual(FinalState, InteractiveViewModel.State, ignoreCase: false);
            }
        }

        [TestMethod]
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
                        });

                transitionCommand.Execute(transitionCompletedEvent);

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
                        });

                transitionCommand.Execute(transitionCompletedEvent);

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);
            }

            Assert.AreSame(expectedException, actualException);
        }

        [TestMethod]
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
                        });

                transitionCommand.Execute(transitionCompletedEvent);

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
    }
}