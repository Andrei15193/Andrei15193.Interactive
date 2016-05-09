using System;

namespace Andrei15193.Interactive
{
    public class ActionStateContext
    {
        internal ActionStateContext(InteractiveViewModel interactiveViewModel, string sourceState)
        {
            if (interactiveViewModel == null)
                throw new ArgumentNullException(nameof(interactiveViewModel));

            InteractiveViewModel = interactiveViewModel;
            PreviousState = sourceState;
        }

        public InteractiveViewModel InteractiveViewModel { get; }

        public string PreviousState { get; }

        public string NextState { get; set; }
    }
}