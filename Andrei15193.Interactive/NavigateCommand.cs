using System;
using System.Reflection;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Andrei15193.Interactive
{
    public class NavigateCommand
        : ICommand
    {
        private string _page = null;
        private Lazy<Type> _pageType;

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

        public NavigationTransitionInfo NavigationTransitionInfo { get; set; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
            => _page != null && Window.Current?.Content is Frame;

        public void Execute(object parameter)
            => ((Frame)Window.Current.Content).Navigate(_pageType.Value, parameter, NavigationTransitionInfo);

        private Type GetPageType()
            => Application.Current.GetType().GetTypeInfo().Assembly.GetType(_page);
    }
}