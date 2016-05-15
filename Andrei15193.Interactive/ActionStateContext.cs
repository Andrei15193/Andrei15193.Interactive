using System;

namespace Andrei15193.Interactive
{
    public class ActionStateContext
    {
        internal ActionStateContext(InteractiveViewModel interactiveViewModel, string sourceState, object parameter)
        {
            if (interactiveViewModel == null)
                throw new ArgumentNullException(nameof(interactiveViewModel));

            InteractiveViewModel = interactiveViewModel;
            PreviousState = sourceState;
            Parameter = parameter;
        }

        public InteractiveViewModel InteractiveViewModel { get; }

        public object Parameter { get; }

        public string PreviousState { get; }

        public string NextState { get; set; }
    }
}