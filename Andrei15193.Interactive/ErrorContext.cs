using System;

namespace Andrei15193.Interactive
{
    public class ErrorContext
    {
        internal ErrorContext(ViewModel viewModel, string faultedState)
        {
            ViewModel = viewModel;
            FaultedState = faultedState;
            IsCanceled = true;
            AggregateException = null;
        }
        internal ErrorContext(ViewModel viewModel, string faultedState, AggregateException exception)
        {
            ViewModel = viewModel;
            FaultedState = faultedState;
            IsCanceled = false;
            AggregateException = exception;
        }

        public ViewModel ViewModel { get; }

        public string FaultedState { get; }

        public string NextState { get; set; }

        public bool IsCanceled { get; }

        public Exception Exception
            => AggregateException?.InnerException;

        public AggregateException AggregateException { get; set; }
    }
}