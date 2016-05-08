using System;

namespace Andrei15193.Interactive
{
    public class ActionStateContext
    {
        internal ActionStateContext(ViewModel viewModel, string sourceState)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            ViewModel = viewModel;
            PreviousState = sourceState;
        }

        public ViewModel ViewModel { get; }

        public string PreviousState { get; }

        public string NextState { get; set; }
    }
}