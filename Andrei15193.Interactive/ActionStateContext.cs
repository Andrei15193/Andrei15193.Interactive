using System;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents the context of an action state. Instances of this type are provided as
    /// parameters to methods mapped to an action state is invoked in order to provide
    /// collaborate with the <see cref="Andrei15193.Interactive.InteractiveViewModel"/>
    /// performing the action.
    /// </summary>
    public class ActionStateContext
    {
        /// <summary>
        /// Creates a new <see cref="ActionStateContext"/> instance.
        /// </summary>
        /// <param name="interactiveViewModel">
        /// The <see cref="Andrei15193.Interactive.InteractiveViewModel"/> performing the action.
        /// </param>
        /// <param name="sourceState">
        /// The name of the state from which the <see cref="Andrei15193.Interactive.InteractiveViewModel"/> has transitioned.
        /// </param>
        /// <param name="parameter">
        /// An optional parameter that the operation can use.
        /// </param>
        internal ActionStateContext(InteractiveViewModel interactiveViewModel, string sourceState, object parameter)
        {
            if (interactiveViewModel == null)
                throw new ArgumentNullException(nameof(interactiveViewModel));

            InteractiveViewModel = interactiveViewModel;
            PreviousState = sourceState;
            Parameter = parameter;
        }

        /// <summary>
        /// The <see cref="Andrei15193.Interactive.InteractiveViewModel"/> that performs the operation.
        /// </summary>
        public InteractiveViewModel InteractiveViewModel { get; }

        /// <summary>
        /// An optional parameter that the context can use.
        /// </summary>
        /// <remarks>
        /// State transitions can be triggered using <see cref="System.Windows.Input.ICommand"/>, the
        /// parameter is provided to the respective command is made available to the operation through
        /// this property.
        /// </remarks>
        public object Parameter { get; }

        /// <summary>
        /// The name of the state from which the <see cref="Andrei15193.Interactive.InteractiveViewModel"/>
        /// has transitioned from.
        /// </summary>
        public string PreviousState { get; }

        /// <summary>
        /// The name of the state the <see cref="InteractiveViewModel"/> should transition t given that
        /// no exception (cancellation included) are thrown.
        /// </summary>
        public string NextState { get; set; }
    }
}