using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    public static class Interactive
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.RegisterAttached("ViewModel",
                typeof(InteractiveViewModel),
                typeof(Interactive),
                new PropertyMetadata(null, ViewModelChanged));

        private static void ViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Control;
            if (control != null)
            {
                var interactiveViewModel = e.OldValue as InteractiveViewModel;
                if (interactiveViewModel != null)
                {
                    interactiveViewModel.PropertyChanged -= GetPropertyChangedEventHandler(control, interactiveViewModel);
                    control.Loaded -=
                        delegate
                        {
                            GoToCurrentState(control, interactiveViewModel);
                        };
                }

                interactiveViewModel = e.NewValue as InteractiveViewModel;
                if (interactiveViewModel != null)
                {
                    interactiveViewModel.PropertyChanged += GetPropertyChangedEventHandler(control, interactiveViewModel);
                    control.Loaded +=
                        delegate
                        {
                            Debug.WriteLine("Control loaded.");
                            GoToCurrentState(control, interactiveViewModel);
                        };
                    GoToCurrentState(control, interactiveViewModel);
                }
            }
        }

        private static PropertyChangedEventHandler GetPropertyChangedEventHandler(Control control, InteractiveViewModel interactiveViewModel)
            => (sender, e) =>
            {
                if (nameof(interactiveViewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                    GoToCurrentState(control, interactiveViewModel);
            };

        private static void GoToCurrentState(Control control, InteractiveViewModel interactiveViewModel)
        {
            var viewModelState = interactiveViewModel.State;
            if (!VisualStateManager.GoToState(control, viewModelState, GetUseTransitions(control)))
                Debug.WriteLine($"Could not navigate to {viewModelState} state [SynchronizationContext: {SynchronizationContext.Current?.ToString() ?? "<null>"}].");
        }

        public static InteractiveViewModel GetViewModel(DependencyObject dependencyObject)
            => (InteractiveViewModel)dependencyObject.GetValue(ViewModelProperty);
        public static void SetViewModel(DependencyObject dependencyObject, InteractiveViewModel interactiveViewModel)
            => dependencyObject.SetValue(ViewModelProperty, interactiveViewModel);

        public static DependencyProperty UseTransitionsProperty { get; } =
            DependencyProperty.RegisterAttached("UseTransitions",
                typeof(bool),
                typeof(Interactive),
                new PropertyMetadata(true));

        public static bool GetUseTransitions(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
                throw new ArgumentNullException(nameof(dependencyObject));

            return (bool)dependencyObject.GetValue(UseTransitionsProperty);
        }
        public static void SetUseTransitions(DependencyObject dependencyObject, bool useTransitions)
            => dependencyObject.SetValue(UseTransitionsProperty, useTransitions);
    }
}