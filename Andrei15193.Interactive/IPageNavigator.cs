using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents an object capable of navigating to a page.
    /// </summary>
    public interface IPageNavigator
    {
        /// <summary>
        /// The state of the <see cref="InteractiveViewModel"/> for which this
        /// navigator should be triggered.
        /// </summary>
        string State
        {
            get;
        }

        /// <summary>
        /// Triggers a page navigation.
        /// </summary>
        /// <param name="frame">
        /// The <see cref="Frame"/> unto which to perform the page navigation.
        /// </param>
        void Navigate(Frame frame);
    }
}