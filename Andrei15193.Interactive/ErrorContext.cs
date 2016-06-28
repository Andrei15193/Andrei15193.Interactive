using System;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents the context in which an action has faulted or was canceled.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// Creates a new <see cref="ErrorContext"/> instance.
        /// </summary>
        /// <param name="interactiveViewModel">
        /// The <see cref="Andrei15193.Interactive.InteractiveViewModel"/> that was performing the canceled action.
        /// </param>
        /// <param name="canceledState">
        /// The name of the action state that was canceled.
        /// </param>
        internal ErrorContext(InteractiveViewModel interactiveViewModel, string canceledState)
        {
            InteractiveViewModel = interactiveViewModel;
            CanTransition = true;
            FaultedState = canceledState;
            IsCanceled = true;
            AggregateException = null;
        }
        /// <summary>
        /// Creates a new <see cref="ErrorContext"/> instance.
        /// </summary>
        /// <param name="interactiveViewModel">
        /// The <see cref="Andrei15193.Interactive.InteractiveViewModel"/> that was performing the faulted action.
        /// </param>
        /// <param name="faultedState">
        /// The name of the action state that was faulted.
        /// </param>
        /// <param name="canTransition">
        /// A flag indicating whether the <see cref="Andrei15193.Interactive.InteractiveViewModel"/> can transition.
        /// </param>
        /// <param name="exception">
        /// The <see cref="System.AggregateException"/> containing details about the error.
        /// </param>
        internal ErrorContext(InteractiveViewModel interactiveViewModel, string faultedState, bool canTransition, AggregateException exception)
        {
            InteractiveViewModel = interactiveViewModel;
            CanTransition = canTransition;
            FaultedState = faultedState;
            IsCanceled = false;
            AggregateException = exception;
        }

        /// <summary>
        /// The <see cref="Andrei15193.Interactive.InteractiveViewModel"/> performing the faulted or canceled action.
        /// </summary>
        public InteractiveViewModel InteractiveViewModel { get; }

        /// <summary>
        /// A flag indicating whether the <see cref="InteractiveViewModel"/> can transition.
        /// </summary>
        public bool CanTransition { get; }

        /// <summary>
        /// The name of the state in which an exception was thrown (faulted or canceled).
        /// </summary>
        public string FaultedState { get; }

        /// <summary>
        /// The name of the state the <see cref="InteractiveViewModel"/> should transition to.
        /// </summary>
        public string NextState { get; set; }

        /// <summary>
        /// A flag indicating whether the exception that was through in fact signifies that the
        /// operation was canceled and not faulted.
        /// </summary>
        public bool IsCanceled { get; }

        /// <summary>
        /// The inner <see cref="System.Exception"/> of the <see cref="AggregateException"/>.
        /// </summary>
        public Exception Exception
            => AggregateException?.InnerException;

        /// <summary>
        /// When faulted, a <see cref="System.AggregateException"/> containg details about the error.
        /// </summary>
        /// <remarks>
        /// An action is faulted when the <see cref="ErrorContext"/> has the <see cref="IsCanceled"/>
        /// flag set to false; otherwise this property returns <value>null</value>.
        /// </remarks>
        public AggregateException AggregateException { get; set; }
    }
}