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
    public class ViewModelTests
    {
        private const string InitialState = "initialTestState";
        private const string ActionState = "actionTestState";
        private const string FollowUpActionState = "followUpActionTestState";
        private const string DestinationState = "destinationTestState";
        private const string FinalState = "finalTestState";

        private sealed class MockViewModel<TDataModel>
            : ViewModel<TDataModel>
        {
            public MockViewModel(TDataModel dataContext)
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

        private sealed class BoundMockViewModel
            : ViewModel<object>
        {
            public BoundMockViewModel()
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
        private MockViewModel<object> ViewModel { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            DataContext = new object();
            ViewModel = new MockViewModel<object>(DataContext);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ViewModel = null;
            DataContext = null;
        }

        [TestMethod]
        public void TestContextGetsSetThroughConstructor()
        {
            Assert.AreSame(DataContext, ViewModel.DataContext);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestContextCannotBeNull()
        {
            new ViewModel<object>(null);
        }

        [TestMethod]
        public async Task TestTransitioningToAStateUpdatesTheStateProperty()
        {
            await ViewModel.TransitionToAsync(InitialState);

            Assert.AreEqual(InitialState, ViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestCannotTransitionToNullState()
        {
            await ViewModel.TransitionToAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestCannotRetrieveStateIfItHasNotBeenSet()
        {
            var state = ViewModel.State;
        }

        [TestMethod]
        public async Task TestTransitioningToActionStateDoesNotAllowOtherTransitionsUntilItHasReachedADestinationState()
        {
            var invocationCount = 0;

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                await ViewModel.TransitionToAsync(InitialState);

                ViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        invocationCount++;
                        actionContext.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = ViewModel.TransitionToAsync(ActionState);
                Assert.AreEqual(ActionState, ViewModel.State, ignoreCase: false);

                completeActionStateEvent.Set();
                await actionStateTransitionTask;

                Assert.AreEqual(DestinationState, ViewModel.State, ignoreCase: false);
            }

            Assert.AreEqual(1, invocationCount);
        }

        [TestMethod]
        public async Task TestViewModelGetsSetOnActionContext()
        {
            var invocationCount = 0;

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                await ViewModel.TransitionToAsync(InitialState);

                ViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        invocationCount++;
                        Assert.AreSame(ViewModel, actionContext.ViewModel);
                        actionContext.NextState = DestinationState;

                        return Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = ViewModel.TransitionToAsync(ActionState);
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
                await ViewModel.TransitionToAsync(InitialState);

                ViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        invocationCount++;
                        Assert.AreEqual(InitialState, actionContext.PreviousState);
                        actionContext.NextState = DestinationState;

                        return Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });

                var actionStateTransitionTask = ViewModel.TransitionToAsync(ActionState);
                completeActionStateEvent.Set();

                await actionStateTransitionTask;
            }

            Assert.AreEqual(1, invocationCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task TestNotSettingADestinationStateThrowsException()
        {
            await ViewModel.TransitionToAsync(InitialState);

            ViewModel.CreateActionState(
                ActionState,
                actionContext =>
                {
                });

            await ViewModel.TransitionToAsync(ActionState);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task CannotTransitionToAnotherStateWhileInAnActionState()
        {
            await ViewModel.TransitionToAsync(InitialState);

            using (var exceptionThrownEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    actionContext =>
                    {
                        actionContext.NextState = DestinationState;
                        return Task.Factory.StartNew(exceptionThrownEvent.Wait);
                    });

                var actionStateTransitionTask = ViewModel.TransitionToAsync(ActionState);
                try
                {
                    await ViewModel.TransitionToAsync(DestinationState);
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
            await ViewModel.TransitionToAsync(InitialState);

            using (var exceptionThrownEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
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
                        ViewModel.PropertyChanged -= eventHandler;
                        raiseCount++;
                        try
                        {
                            Task.Run(() => ViewModel.TransitionToAsync(DestinationState)).Wait();
                        }
                        catch (AggregateException aggregateException)
                        {
                            throw aggregateException.InnerException;
                        }
                    };
                ViewModel.PropertyChanged += eventHandler;

                await ViewModel.TransitionToAsync(ActionState);
            }

            Assert.AreEqual(1, raiseCount);
        }

        [TestMethod]
        public async Task TestTransitionFromPropertyChangedEventHandlerAfterActionStateCompleted()
        {
            var raiseCount = 0;
            await ViewModel.TransitionToAsync(InitialState);

            using (var exceptionThrownEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
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
                        ViewModel.PropertyChanged -= eventHandler;
                        Task.Run(() => ViewModel.TransitionToAsync(FinalState)).Wait();
                    };

                var actionStateTransitionTask = ViewModel.TransitionToAsync(ActionState);

                ViewModel.PropertyChanged += eventHandler;

                exceptionThrownEvent.Set();
                await actionStateTransitionTask;
            }

            Assert.AreEqual(FinalState, ViewModel.State, ignoreCase: false);
            Assert.AreEqual(1, raiseCount);
        }

        [TestMethod]
        public async Task TestChainingActionStates()
        {
            var stateChanges = new List<string>();
            ViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateChanges.Add(ViewModel.State);
                };
            await ViewModel.TransitionToAsync(InitialState);

            ViewModel.CreateActionState(
                ActionState,
                context =>
                {
                    context.NextState = FollowUpActionState;
                });

            ViewModel.CreateActionState(
                FollowUpActionState,
                context =>
                {
                    context.NextState = DestinationState;
                });

            await ViewModel.TransitionToAsync(ActionState);

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
            await ViewModel.TransitionToAsync(InitialState);

            ViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        if (FollowUpActionState.Equals(ViewModel.State, StringComparison.Ordinal))
                            try
                            {
                                Task.Run(() => ViewModel.TransitionToAsync(DestinationState)).Wait();
                            }
                            catch (AggregateException aggregateException)
                            {
                                throw aggregateException.InnerException;
                            }
                };

            using (var continueFollowUpActionEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        context.NextState = FollowUpActionState;
                    });

                ViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(continueFollowUpActionEvent.Wait);
                    });

                await ViewModel.TransitionToAsync(ActionState);
            }
        }

        [TestMethod]
        public async Task TestCancelableActionStatesReceiveACancelableCancellationToken()
        {
            await ViewModel.TransitionToAsync(InitialState);

            ViewModel.CreateActionState(
                ActionState,
                (context, cancellationToken) =>
                {
                    context.NextState = DestinationState;
                    Assert.IsTrue(cancellationToken.CanBeCanceled);
                    return Task.FromResult<object>(null);
                });

            await ViewModel.TransitionToAsync(ActionState);
        }

        [TestMethod]
        public async Task TestWhileInACancelableActionStateTheCancelCommandCanBeExecuted()
        {
            await ViewModel.TransitionToAsync(InitialState);

            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = ViewModel.TransitionToAsync(ActionState);

                Assert.IsTrue(ViewModel.CancelCommand.CanExecute(null));

                completeActionEvent.Set();
                await actionStateTranstionTask;
            }

            Assert.IsFalse(ViewModel.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestWhileInAnActionStateThatCannotBeCanceledTheCancelComamndCannotBeExecuted()
        {
            await ViewModel.TransitionToAsync(InitialState);

            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = ViewModel.TransitionToAsync(ActionState);

                Assert.IsFalse(ViewModel.CancelCommand.CanExecute(null));

                completeActionEvent.Set();
                await actionStateTranstionTask;
            }

            Assert.IsFalse(ViewModel.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestChainedCancelableAndNonCancelableActionStatesEnableAndDisableCancelCommand()
        {
            await ViewModel.TransitionToAsync(InitialState);

            using (var transitionedToFollowUpStateEvent = new ManualResetEventSlim(false))
            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                ViewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                            if (FollowUpActionState.Equals(ViewModel.State, StringComparison.Ordinal))
                                transitionedToFollowUpStateEvent.Set();
                    };

                ViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        context.NextState = FollowUpActionState;
                        return Task.Factory
                            .StartNew(completeActionEvent.Wait)
                            .ContinueWith(task => completeActionEvent.Reset());
                    });
                ViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = ViewModel.TransitionToAsync(ActionState);

                Assert.IsTrue(ViewModel.CancelCommand.CanExecute(null));
                completeActionEvent.Set();

                transitionedToFollowUpStateEvent.Wait();

                Assert.IsFalse(ViewModel.CancelCommand.CanExecute(null));
                completeActionEvent.Set();

                await actionStateTranstionTask;
            }

            Assert.IsFalse(ViewModel.CancelCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestCanExecuteIsRaisedWhenCancelCommandBecomesAvailableOrUnavailable()
        {
            var raiseCount = 0;
            ViewModel.CancelCommand.CanExecuteChanged += delegate { raiseCount++; };

            await ViewModel.TransitionToAsync(InitialState);

            using (var completeActionEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    (context, cancellationToken) =>
                    {
                        context.NextState = DestinationState;
                        return Task.Factory.StartNew(completeActionEvent.Wait);
                    });

                var actionStateTranstionTask = ViewModel.TransitionToAsync(ActionState);

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
            await ViewModel.TransitionToAsync(InitialState);

            Assert.IsFalse(ViewModel.CancelCommand.CanExecute(null));
            ViewModel.CancelCommand.Execute(null);
        }

        [TestMethod]
        public async Task TestExecutingTheCancelCommandSignalsTheCancellationToken()
        {
            await ViewModel.TransitionToAsync(InitialState);

            using (var cancelSignledEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    async (context, cancellationToken) =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(cancelSignledEvent.Wait);
                        Assert.IsTrue(cancellationToken.IsCancellationRequested);
                    });

                var actionStateTranstitionTask = ViewModel.TransitionToAsync(ActionState);
                ViewModel.CancelCommand.Execute(null);

                cancelSignledEvent.Set();
                await actionStateTranstitionTask;
            }
        }

        [TestMethod]
        public async Task TestBindingACommandMakesItAvailableInTheSelectedStates()
        {
            var boundCommand = ViewModel.BindCommand(new MockCommand(), ActionState);

            await ViewModel.TransitionToAsync(InitialState);
            Assert.IsFalse(boundCommand.CanExecute(null));

            await ViewModel.TransitionToAsync(ActionState);
            Assert.IsTrue(boundCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task TestCanExecuteChangedIsRaisedWhenTheViewModelMovesInAndOutOfBoundCommands()
        {
            var raiseCount = 0;
            var boundCommand = ViewModel.BindCommand(new MockCommand(), ActionState, DestinationState);
            boundCommand.CanExecuteChanged +=
                (sender, e) => raiseCount++;

            await ViewModel.TransitionToAsync(InitialState);
            Assert.IsFalse(boundCommand.CanExecute(null));
            Assert.AreEqual(0, raiseCount);

            await ViewModel.TransitionToAsync(ActionState);
            Assert.IsTrue(boundCommand.CanExecute(null));
            Assert.AreEqual(1, raiseCount);

            await ViewModel.TransitionToAsync(DestinationState);
            Assert.IsTrue(boundCommand.CanExecute(null));
            Assert.AreEqual(1, raiseCount);

            await ViewModel.TransitionToAsync(FinalState);
            Assert.IsFalse(boundCommand.CanExecute(null));
            Assert.AreEqual(2, raiseCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCannotBindNullComamnd()
        {
            ViewModel.BindCommand(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCannotBindNullStatesCollection()
        {
            ViewModel.BindCommand(new MockCommand(), null);
        }

        [TestMethod]
        public async Task TestExecutingTransitionCommandTranstionsTheViewModelInTheTargetedState()
        {
            await ViewModel.TransitionToAsync(InitialState);

            var transitionCommand = ViewModel.GetTransitionCommand(DestinationState);

            using (var stateChangedEvent = new ManualResetEventSlim(false))
            {
                ViewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                            stateChangedEvent.Set();
                    };

                transitionCommand.Execute(null);
                stateChangedEvent.Wait();

                Assert.AreEqual(DestinationState, ViewModel.State);
            }
        }

        [TestMethod]
        public void TestBoundTransitionCommandIsAvailableOnlyInStatesToWhichItHasBeenBound()
        {
            var viewModel = new BoundMockViewModel();

            Assert.IsTrue(viewModel.GoToActionStateCommand.CanExecute(false));
            Assert.IsFalse(viewModel.GoToDestinationStateCommand.CanExecute(false));

            using (var stateChangedEvent = new ManualResetEventSlim(false))
            {
                viewModel.PropertyChanged +=
                    (sender, e) =>
                    {
                        if (nameof(viewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                            stateChangedEvent.Set();
                    };

                viewModel.GoToActionStateCommand.Execute(null);
                stateChangedEvent.Wait();
                stateChangedEvent.Reset();

                Assert.IsFalse(viewModel.GoToActionStateCommand.CanExecute(false));
                Assert.IsTrue(viewModel.GoToDestinationStateCommand.CanExecute(false));

                viewModel.GoToDestinationStateCommand.Execute(null);
                stateChangedEvent.Wait();
            }

            Assert.IsFalse(viewModel.GoToActionStateCommand.CanExecute(false));
            Assert.IsFalse(viewModel.GoToDestinationStateCommand.CanExecute(false));
        }

        [TestMethod]
        public async Task TestInitialStateCanBeAnActionState()
        {
            using (var continueEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    InitialState,
                    context =>
                    {
                        context.NextState = FinalState;
                        return Task.Factory.StartNew(continueEvent.Wait);
                    });

                var initialStateTransitionTask = ViewModel.TransitionToAsync(InitialState);
                Assert.AreEqual(InitialState, ViewModel.State, ignoreCase: false);

                continueEvent.Set();
                await initialStateTransitionTask;
            }

            Assert.AreEqual(FinalState, ViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        public async Task TestTransitioningToAFaultingStateWithTransitionCommandWillGoThroughTheErrorHandler()
        {
            using (var transitionCompletedEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        throw new Exception();
                    });

                var transitionCommand =
                    ViewModel.GetTransitionCommand(
                        ActionState,
                        context =>
                        {
                            context.NextState = FinalState;
                        });

                transitionCommand.Execute(transitionCompletedEvent);

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);

                Assert.AreEqual(FinalState, ViewModel.State, ignoreCase: false);
            }
        }

        [TestMethod]
        public async Task TestTransitioningFromAFaultedStateToAnActionStateThroughTheErrorHandler()
        {
            var stateChanges = new List<string>();
            ViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateChanges.Add(ViewModel.State);
                };
            using (var transitionCompletedEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        throw new Exception();
                    });
                ViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = FinalState;
                    });

                var transitionCommand =
                    ViewModel.GetTransitionCommand(
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
                ViewModel.CreateActionState(
                    ActionState,
                    context =>
                    {
                        throw expectedException;
                    });

                var transitionCommand =
                    ViewModel.GetTransitionCommand(
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
                ViewModel.CreateActionState(
                    ActionState,
                    async (context, cancellationToken) =>
                    {
                        await Task.Factory.StartNew(cancelCommandExecutedEvent.Wait);
                        cancellationToken.ThrowIfCancellationRequested();
                    });

                var transitionCommand =
                    ViewModel.GetTransitionCommand(
                        ActionState,
                        context =>
                        {
                            context.NextState = FinalState;
                            isCanceled = context.IsCanceled;
                        });

                transitionCommand.Execute(transitionCompletedEvent);

                ViewModel.CancelCommand.Execute(null);
                cancelCommandExecutedEvent.Set();

                await Task.Factory.StartNew(transitionCompletedEvent.Wait);
            }

            Assert.IsTrue(isCanceled);
        }

        [TestMethod]
        public async Task TestEnqueueTransitionToTransitionsToStateIfNotInAnActionState()
        {
            await ViewModel.TransitionToAsync(InitialState);

            await ViewModel.EnqueueTransitionToAsync(DestinationState);

            Assert.AreEqual(DestinationState, ViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        public async Task TestEnqueueTransitionToTransitionsToStateAfterItHasCompletedCurrentActionState()
        {
            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = ViewModel.TransitionToAsync(ActionState);
                var transitionToFinalStateTask = ViewModel.EnqueueTransitionToAsync(FinalState);

                completeActionStateEvent.Set();

                await Task.WhenAll(transitionToActionStateTask, transitionToFinalStateTask);

            }
            Assert.AreEqual(FinalState, ViewModel.State, ignoreCase: false);
        }

        [TestMethod]
        public async Task TestEnqueueingTheSameStateTwiceWhileInAnActionStateWillTransitionOnlyOneAfterTheActionStateIsCompelted()
        {
            var stateTransitions = new List<string>();

            ViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateTransitions.Add(ViewModel.State);
                };

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = ViewModel.TransitionToAsync(ActionState);
                var transitionToFinalStateTask = Task.WhenAll(
                    ViewModel.EnqueueTransitionToAsync(FinalState),
                    ViewModel.EnqueueTransitionToAsync(FinalState));

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

            ViewModel.PropertyChanged +=
                (sender, e) =>
                {
                    if (nameof(ViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                        stateTransitions.Add(ViewModel.State);
                };

            using (var completeActionStateEvent = new ManualResetEventSlim(false))
            {
                ViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = ViewModel.TransitionToAsync(ActionState);
                var transitionToFinalStateTask = Task.WhenAll(
                    ViewModel.EnqueueTransitionToAsync(FinalState),
                    ViewModel.EnqueueTransitionToAsync(FollowUpActionState),
                    ViewModel.EnqueueTransitionToAsync(FinalState));

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
                ViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                ViewModel.CreateActionState(
                    FollowUpActionState,
                    context =>
                    {
                        context.NextState = FinalState;
                    });
                var transitionToActionStateTask = ViewModel.TransitionToAsync(ActionState);
                var transitionToFollowUpStateTask = ViewModel.EnqueueTransitionToAsync(FollowUpActionState);

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
                ViewModel.CreateActionState(
                    ActionState,
                    async context =>
                    {
                        context.NextState = DestinationState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                        completeActionStateEvent.Reset();
                    });
                ViewModel.CreateActionState(
                    FollowUpActionState,
                    async context =>
                    {
                        context.NextState = FinalState;
                        await Task.Factory.StartNew(completeActionStateEvent.Wait);
                    });
                var transitionToActionStateTask = ViewModel.TransitionToAsync(ActionState);
                var transitionToFollowUpStateTask = ViewModel.EnqueueTransitionToAsync(FollowUpActionState);

                completeActionStateEvent.Set();
                await transitionToActionStateTask;

                Assert.IsFalse(transitionToFollowUpStateTask.IsCompleted);
                completeActionStateEvent.Set();

                await transitionToFollowUpStateTask;
            }
        }
    }
}