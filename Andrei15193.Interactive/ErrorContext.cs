using System;

namespace Andrei15193.Interactive
{
    public class ErrorContext
    {
        internal ErrorContext(InteractiveViewModel interactiveViewModel, string faultedState)
        {
            InteractiveViewModel = interactiveViewModel;
            CanTransition = true;
            FaultedState = faultedState;
            IsCanceled = true;
            AggregateException = null;
        }
        internal ErrorContext(InteractiveViewModel interactiveViewModel, string faultedState, bool canTransition, AggregateException exception)
        {
            InteractiveViewModel = interactiveViewModel;
            CanTransition = canTransition;
            FaultedState = faultedState;
            IsCanceled = false;
            AggregateException = exception;
        }

        public InteractiveViewModel InteractiveViewModel { get; }

        public bool CanTransition { get; }

        public string FaultedState { get; }

        public string NextState { get; set; }

        public bool IsCanceled { get; }

        public Exception Exception
            => AggregateException?.InnerException;

        public AggregateException AggregateException { get; set; }
    }
}