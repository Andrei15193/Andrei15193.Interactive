using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    public static class Interactive
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.RegisterAttached("ViewModel",
                typeof(ViewModel),
                typeof(Interactive),
                new PropertyMetadata(null, ViewModelChanged));

        private static void ViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Control;
            if (control != null)
            {
                var viewModel = e.OldValue as ViewModel;
                if (viewModel != null)
                {
                    viewModel.PropertyChanged -= GetPropertyChangedEventHandler(control, viewModel);
                    control.Loaded -=
                        delegate
                        {
                            _TryGoToCurrentState(control, viewModel);
                        };
                }

                viewModel = e.NewValue as ViewModel;
                if (viewModel != null)
                {
                    viewModel.PropertyChanged += GetPropertyChangedEventHandler(control, viewModel);
                    if (!_TryGoToCurrentState(control, viewModel, "Registering to Control.Loaded event."))
                        control.Loaded +=
                            delegate
                            {
                                _TryGoToCurrentState(control, viewModel);
                            };
                }
            }
        }

        private static PropertyChangedEventHandler GetPropertyChangedEventHandler(Control control, ViewModel viewModel)
            => (sender, e) =>
            {
                if (nameof(viewModel.State).Equals(e.PropertyName, StringComparison.OrdinalIgnoreCase))
                    _TryGoToCurrentState(control, viewModel);
            };

        private static bool _TryGoToCurrentState(Control control, ViewModel viewModel, string debugMessage = null)
        {
            var viewModelState = viewModel.State;
            if (VisualStateManager.GoToState(control, viewModelState, GetUseTransitions(control)))
                return true;

            Debug.WriteLine($"Could not navigated to {viewModelState}. " + (debugMessage ?? string.Empty));
            return false;
        }

        public static ViewModel GetViewModel(DependencyObject dependencyObject)
            => (ViewModel)dependencyObject.GetValue(ViewModelProperty);
        public static void SetViewModel(DependencyObject dependencyObject, ViewModel viewModel)
            => dependencyObject.SetValue(ViewModelProperty, viewModel);

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