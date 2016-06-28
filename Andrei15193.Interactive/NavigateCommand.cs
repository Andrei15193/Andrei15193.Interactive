using System;
using System.Reflection;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents an <see cref="ICommand"/> implementation to use for <see cref="Page"/> navigation.
    /// </summary>
    public class NavigateCommand
        : ICommand
    {
        private string _page = null;
        private Lazy<Type> _pageType;

        /// <summary>
        /// The full name of the <see cref="Page"/> to navigate to.
        /// </summary>
        public string Page
        {
            get
            {
                return _page;
            }
            set
            {
                _page = value;
                _pageType = new Lazy<Type>(GetPageType);
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The <see cref="Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo"/> to use when executing the command.
        /// </summary>
        public NavigationTransitionInfo NavigationTransitionInfo { get; set; }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Indicates whether the command can be executed in the current context.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        /// <returns>
        /// Returns true if the command can be executed; false otherwise.
        /// </returns>
        public bool CanExecute(object parameter)
            => _page != null && Window.Current?.Content is Frame;

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public void Execute(object parameter)
            => ((Frame)Window.Current.Content).Navigate(_pageType.Value, parameter, NavigationTransitionInfo);

        private Type GetPageType()
            => Application.Current.GetType().GetTypeInfo().Assembly.GetType(_page);
    }
}