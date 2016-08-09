using System.Diagnostics;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Performs forward page navigation.
    /// </summary>
    public sealed class ForwardPageNavigator
        : IPageNavigator
    {
        /// <summary>
        /// The state of the <see cref="InteractiveViewModel"/> for which this
        /// navigator should be triggered.
        /// </summary>
        public string State
        {
            get;
            set;
        }

        /// <summary>
        /// Triggers a back page navigation.
        /// </summary>
        /// <param name="frame">
        /// The <see cref="Frame"/> unto which to perform the forward page navigation.
        /// </param>
        public void Navigate(Frame frame)
        {
            Debug.WriteLineIf(!frame.CanGoForward, "Cannot perform forward navigation.");
            if (frame.CanGoForward)
                frame.GoForward();
        }
    }
}