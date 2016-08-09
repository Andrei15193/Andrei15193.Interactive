using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// A container for attached <see cref="DependencyProperty"/>es for implemnting interactive applications.
    /// </summary>
    public static class Interactive
    {
        /// <summary>
        /// The <see cref="DependencyProperty"/> for specifying whether to use transitions or not.
        /// </summary>
        public static readonly DependencyProperty UseTransitionsProperty =
            DependencyProperty.RegisterAttached(
                "UseTransitions",
                typeof(bool),
                typeof(Interactive),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets the value of the <see cref="UseTransitionsProperty"/> for a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">
        /// The <see cref="DependencyObject"/> for which to get the <see cref="UseTransitionsProperty"/> value.
        /// </param>
        /// <returns>
        /// Returns a flag indicating whether to use transitions or not.
        /// </returns>
        public static bool GetUseTransitions(DependencyObject dependencyObject)
            => (bool)dependencyObject.GetValue(UseTransitionsProperty);

        /// <summary>
        /// Sets the value of the <see cref="UseTransitionsProperty"/> for a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">
        /// The <see cref="DependencyObject"/> for which to set the <see cref="UseTransitionsProperty"/> value.
        /// </param>
        /// <param name="useTransitions">
        /// The value to set for the <see cref="UseTransitionsProperty"/>.
        /// </param>
        public static void SetUseTransitions(DependencyObject dependencyObject, bool useTransitions)
            => dependencyObject.SetValue(UseTransitionsProperty, useTransitions);

        /// <summary>
        /// The <see cref="DependencyProperty"/> indicating whether a <see cref="FrameworkElement"/>
        /// has an event handler subsscribed for its <see cref="FrameworkElement.Loaded"/> event.
        /// </summary>
        /// <remarks>
        /// This <see cref="DependencyProperty"/> is used internally, it is better not to change its value.
        /// </remarks>
        internal static readonly DependencyProperty IsSubscribedProperty =
            DependencyProperty.RegisterAttached(
                "IsSubscribed",
                typeof(bool),
                typeof(Interactive),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets the value of the <see cref="IsSubscribedProperty"/> for a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">
        /// The <see cref="DependencyObject"/> for which to get the <see cref="IsSubscribedProperty"/> value.
        /// </param>
        /// <returns>
        /// Returns a flag indicating whether an event handler was subscribed to the <see cref="DependencyObject"/>.
        /// </returns>
        internal static bool GetIsSubscribed(DependencyObject dependencyObject)
            => (bool)dependencyObject.GetValue(IsSubscribedProperty);

        /// <summary>
        /// Sets the value of the <see cref="IsSubscribedProperty"/> for a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">
        /// The <see cref="DependencyObject"/> for which to set the <see cref="IsSubscribedProperty"/> value.
        /// </param>
        /// <param name="subscribed">
        /// The value to set for the <see cref="IsSubscribedProperty"/>.
        /// </param>
        internal static void SetIsSubscribed(DependencyObject dependencyObject, bool subscribed)
            => dependencyObject.SetValue(IsSubscribedProperty, subscribed);

        /// <summary>
        /// The <see cref="DependencyProperty"/> for specifying page navigators when an
        /// <see cref="InteractiveViewModel"/> transitions to a state.
        /// </summary>
        public static readonly DependencyProperty StatePageNavigatorsProperty =
            DependencyProperty.RegisterAttached(
                "StatePageNavigators",
                typeof(List<IPageNavigator>),
                typeof(Interactive),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets the value of the <see cref="StatePageNavigatorsProperty"/> for the given <see cref="Page"/>.
        /// </summary>
        /// <param name="page">
        /// The <see cref="Page"/> for which to get the <see cref="StatePageNavigatorsProperty"/> value.
        /// </param>
        /// <returns>
        /// Returns an <see cref="List{IPageNavigator}"/> that can be modified to contain the <see cref="IPageNavigator"/>
        /// to be invoked when <see cref="FrameworkElement.DataContext"/> (when it is an <see cref="InteractiveViewModel"/>)
        /// of the provided <paramref name="page"/> transitions to a specific state.
        /// </returns>
        public static List<IPageNavigator> GetStatePageNavigators(Page page)
        {
            var pageNavigators = (List<IPageNavigator>)page.GetValue(StatePageNavigatorsProperty);
            if (pageNavigators == null)
            {
                var pageNavigatorList = new PageNavigatorList(page);
                page.DataContextChanged += (sender, e) => pageNavigatorList.InteractiveViewModel = (e.NewValue as InteractiveViewModel);

                page.SetValue(StatePageNavigatorsProperty, pageNavigatorList);
                pageNavigators = pageNavigatorList;
            }

            return pageNavigators;
        }

        private sealed class PageNavigatorList
            : List<IPageNavigator>
        {
            private readonly Page _page;
            private InteractiveViewModel _interactiveViewModel = null;

            internal PageNavigatorList(Page page)
            {
                if (page == null)
                    throw new ArgumentNullException(nameof(page));

                _page = page;
            }

            internal InteractiveViewModel InteractiveViewModel
            {
                get
                {
                    return _interactiveViewModel;
                }
                set
                {
                    if (_interactiveViewModel != null)
                        _interactiveViewModel.PropertyChanged -= _InteractiveViewModelPropertyChanged;

                    _interactiveViewModel = value;

                    if (_interactiveViewModel != null)
                        _interactiveViewModel.PropertyChanged += _InteractiveViewModelPropertyChanged;
                }
            }

            private void _InteractiveViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (nameof(InteractiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    var pageNavigator = Find(existingPageNavigator => (existingPageNavigator.State == _interactiveViewModel.State));

                    Debug.WriteLineIf(pageNavigator == null, $"No page navigator was found for {_interactiveViewModel.State} state.");
                    if (pageNavigator != null)
                        pageNavigator.Navigate(_page.Frame);
                }
            }
        }

        /// <summary>
        /// <para>
        /// The <see cref="DependencyProperty"/> for specifying the current <see cref="VisualState"/>
        /// of a control.
        /// </para>
        /// <para>
        /// When the <see cref="DependencyObject"/> to which the <see cref="VisualState"/> is set is
        /// not a <see cref="Control"/> then the <see cref="VisualState"/> will be set to the first
        /// <see cref="Control"/> that is found as parent of the <see cref="DependencyObject"/>.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty VisualStateProperty =
            DependencyProperty.RegisterAttached(
                "VisualState",
                typeof(string),
                typeof(Interactive),
                new PropertyMetadata(null, VisualStateChanged));

        /// <summary>
        /// Gets the value of the <see cref="VisualStateProperty"/> for a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">
        /// The <see cref="DependencyObject"/> for which to get the <see cref="VisualStateProperty"/> value.
        /// </param>
        /// <returns>
        /// Returns a string representing the name of the <see cref="VisualState"/> that was previously
        /// set for a <see cref="DependencyObject"/>.
        /// </returns>
        public static string GetVisualState(DependencyObject dependencyObject)
            => (string)dependencyObject.GetValue(VisualStateProperty);

        /// <summary>
        /// Sets the value of the <see cref="VisualStateProperty"/> for a given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="dependencyObject">
        /// The <see cref="DependencyObject"/> for which to set the <see cref="VisualStateProperty"/> value.
        /// </param>
        /// <param name="visualState">
        /// The name of the <see cref="VisualState"/> to set for the given <see cref="DependencyObject"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// Even though the <see cref="DependencyProperty"/> is set on a <see cref="DependencyObject"/>, the
        /// actual <see cref="VisualState"/> is set through the <see cref="VisualStateManager"/> for the first
        /// <see cref="Control"/> that is found starting with the provided <paramref name="dependencyObject"/>
        /// and continuing with its parents.
        /// </para>
        /// <para>
        /// If no <see cref="Control"/> is found then the <see cref="VisualState"/> is not set through the
        /// <see cref="VisualStateManager"/>, however that is unlikely as it is expected to have a
        /// <see cref="Control"/> as some point in the hierarchy, may it be even a <see cref="Page"/>.
        /// </para>
        /// <para>
        /// The reasoning behind the search for a <see cref="Control"/> is that in some cases a <see cref="Control"/>
        /// may use multiple <see cref="InteractiveViewModel"/>s and thus some of them will be declared in the
        /// Resources section of the <see cref="Control"/>. One cannot reference these resources on any attribute
        /// of the <see cref="Control"/>. This is place where one would set the <see cref="VisualStateProperty"/>
        /// on a <see cref="DependencyObject"/> contained by the <see cref="Control"/> as it will ultimatelly be
        /// set on the <see cref="Control"/> itself.
        /// </para>
        /// <code>
        /// &lt;Page
        ///     x:Class=&quot;MyNamespace.SomePage&quot;
        ///     xmlns=&quot;http://schemas.microsoft.com/winfx/2006/xaml/presentation&quot;
        ///     xmlns:x=&quot;http://schemas.microsoft.com/winfx/2006/xaml&quot;
        ///     xmlns:local=&quot;using:MyNamespace&quot;
        ///     xmlns:i=&quot;using:Andrei15193.Interactive&quot;
        ///     &lt;Page.Resources&gt;
        ///         &lt;local:AnotherInteractiveViewModel
        ///             x:Name=&quot;ViewModel&quot; /&gt;
        ///     &lt;/Page.Resources&gt;
        /// 
        ///     &lt;Page.DataContext&gt;
        ///         &lt;local:OneInteractiveViewModel /&gt;
        ///     &lt;/Page.DataContext&gt;
        /// 
        ///     &lt;Grid
        ///         i:Interactive.VisualState=&quot;{Binding State, Source = { StaticResource ViewModel }}&quot;&gt;
        ///     &lt;/Grid&gt;
        /// &lt;/Page&gt;
        /// </code>
        /// </remarks>
        public static void SetVisualState(DependencyObject dependencyObject, string visualState)
            => dependencyObject.SetValue(VisualStateProperty, visualState);

        private static void VisualStateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var frameworkElement = dependencyObject as FrameworkElement;
            Debug.WriteLineIf(frameworkElement == null, "The provided DependencyObject is not a FrameworkElement.");

            if (frameworkElement != null)
            {
                if (!GetIsSubscribed(dependencyObject))
                {
                    SetIsSubscribed(dependencyObject, true);
                    frameworkElement.Loaded += FrameworkElementLoadedEventHandler;
                }

                var control = TryFindControlFrom(frameworkElement);
                if (control != null)
                    control.GoTo(eventArgs.NewValue as string, GetUseTransitions(frameworkElement));
            }
        }

        private static void FrameworkElementLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            var frameworkElement = (FrameworkElement)sender;
            var control = TryFindControlFrom(frameworkElement);

            Debug.WriteLineIf(control == null, "Could not find a control to set the visual state to.");

            if (control != null)
                control.GoTo(GetVisualState(frameworkElement), GetUseTransitions(frameworkElement));
        }

        private static Control TryFindControlFrom(FrameworkElement frameworkElement)
        {
            if (frameworkElement == null)
                return null;

            Control control;

            do
            {
                control = frameworkElement as Control;
                frameworkElement = frameworkElement.Parent as FrameworkElement;
            } while (frameworkElement != null && control == null);

            return control;
        }

        private static void GoTo(this Control control, string state, bool useTransitions)
        {
            if (!VisualStateManager.GoToState(control, state, useTransitions))
                Debug.WriteLine($"Could not navigate to {state} state [SynchronizationContext: {SynchronizationContext.Current?.ToString() ?? "<null>"}].");
        }
    }
}