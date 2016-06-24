using System.Diagnostics;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    public static class Interactive
    {
        public static readonly DependencyProperty UseTransitionsProperty =
            DependencyProperty.RegisterAttached("UseTransitions",
                typeof(bool),
                typeof(Interactive),
                new PropertyMetadata(true));

        public static bool GetUseTransitions(DependencyObject dependencyObject)
            => (bool)dependencyObject.GetValue(UseTransitionsProperty);

        public static void SetUseTransitions(DependencyObject dependencyObject, bool useTransitions)
            => dependencyObject.SetValue(UseTransitionsProperty, useTransitions);

        internal static readonly DependencyProperty IsSubscribedProperty =
            DependencyProperty.RegisterAttached("IsSubscribed",
                typeof(bool),
                typeof(Interactive),
                new PropertyMetadata(false));

        internal static bool GetIsSubscribed(DependencyObject dependencyObject)
            => (bool)dependencyObject.GetValue(IsSubscribedProperty);

        internal static void SetIsSubscribed(DependencyObject dependencyObject, bool subscribed)
            => dependencyObject.SetValue(IsSubscribedProperty, subscribed);

        public static readonly DependencyProperty VisualStateProperty =
            DependencyProperty.RegisterAttached("VisualState",
                typeof(string),
                typeof(Interactive),
                new PropertyMetadata(null, VisualStateChanged));

        public static string GetVisualState(DependencyObject dependencyObject)
            => (string)dependencyObject.GetValue(VisualStateProperty);

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