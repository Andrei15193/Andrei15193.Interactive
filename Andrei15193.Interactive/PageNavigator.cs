using System;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Performs a page navigation.
    /// </summary>
    public class PageNavigator
        : DependencyObject, IPageNavigator
    {
        /// <summary>
        /// The <see cref="DependencyProperty"/> for page navigation parameter.
        /// </summary>
        public static readonly DependencyProperty ParameterProperty =
            DependencyProperty.Register(
                nameof(Parameter),
                typeof(object),
                typeof(PageNavigator),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the page navigation parameter.
        /// </summary>
        public object Parameter
        {
            get
            {
                return GetValue(ParameterProperty);
            }
            set
            {
                SetValue(ParameterProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the page transition override.
        /// </summary>
        public static readonly DependencyProperty NavigationTransitionInfoOverrideProperty =
            DependencyProperty.Register(
                nameof(NavigationTransitionInfoOverride),
                typeof(NavigationTransitionInfo),
                typeof(PageNavigator),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the page transition override.
        /// </summary>
        public NavigationTransitionInfo NavigationTransitionInfoOverride
        {
            get
            {
                return (NavigationTransitionInfo)GetValue(NavigationTransitionInfoOverrideProperty);
            }
            set
            {
                SetValue(NavigationTransitionInfoOverrideProperty, value);
            }
        }

        private Lazy<Type> _pageType = null;
        private string _pageTypeName;

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
        /// The full name of the page type to navigate to.
        /// </summary>
        public string PageTypeName
        {
            get
            {
                return _pageTypeName;
            }
            set
            {
                if (_pageTypeName != value)
                    if (value == null)
                        _pageType = null;
                    else
                        _pageType = new Lazy<Type>(() => Application.Current.GetType().GetTypeInfo().Assembly.GetType(value));
                _pageTypeName = value;
            }
        }

        /// <summary>
        /// The type of the page to navigate to.
        /// </summary>
        public Type PageType
        {
            get
            {
                return _pageType?.Value;
            }
            set
            {
                if (value == null)
                {
                    _pageType = null;
                    _pageTypeName = null;
                }
                else
                {
                    _pageType = new Lazy<Type>(() => value);
                    if (string.IsNullOrWhiteSpace(value.Namespace))
                        _pageTypeName = value.Name;
                    else
                        _pageTypeName = string.Join(".", value.Namespace, value.Name);
                }
            }
        }

        /// <summary>
        /// Triggers a page navigation to <see cref="PageType"/> using the optional
        /// <see cref="Parameter"/> and <see cref="NavigationTransitionInfoOverride"/>.
        /// </summary>
        /// <param name="frame">
        /// The <see cref="Frame"/> unto which to perform the forward page navigation.
        /// </param>
        public void Navigate(Frame frame)
        {
            frame.Navigate(PageType, Parameter, NavigationTransitionInfoOverride);
        }
    }
}