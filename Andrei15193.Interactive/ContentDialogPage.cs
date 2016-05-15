using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    public class ContentDialogPage
        : Page
    {
        private sealed class AppBatButtonCommand
            : ICommand
        {
            private ICommand _command = null;
            private bool _canExecute = false;

            public AppBatButtonCommand(AppBarButton button)
            {
                if (button == null)
                    throw new ArgumentNullException(nameof(button));

                button.Loaded +=
                    delegate
                    {
                        _canExecute = false;
                        _RaiseCanExecuteChanged(this, EventArgs.Empty);
                        _canExecute = true;
                        _RaiseCanExecuteChanged(this, EventArgs.Empty);
                    };
            }

            public ICommand Command
            {
                get
                {
                    return _command;
                }
                set
                {
                    if (_command != null)
                        _command.CanExecuteChanged -= _RaiseCanExecuteChanged;

                    _command = value;
                    if (_command != null)
                        _command.CanExecuteChanged += _RaiseCanExecuteChanged;

                    _RaiseCanExecuteChanged(this, EventArgs.Empty);
                }
            }

            private void _RaiseCanExecuteChanged(object sender, EventArgs e)
                => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
                => _canExecute && (_command?.CanExecute(parameter) ?? true);

            public void Execute(object parameter)
                => _command?.Execute(null);
        }

        public static Symbol PrimaryButtonIconDefaultValue { get; } = Symbol.Accept;
        public static DependencyProperty PrimaryButtonIconProperty { get; } =
            DependencyProperty.Register(
                nameof(PrimaryButtonIcon),
                typeof(Symbol),
                typeof(ContentDialogPage),
                new PropertyMetadata(PrimaryButtonIconDefaultValue, PrimaryButtonIconPropertyChanged));
        private static void PrimaryButtonIconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.PrimaryButton.Icon = new SymbolIcon(e.NewValue as Symbol? ?? PrimaryButtonIconDefaultValue);
        }
        public Symbol PrimaryButtonIcon
        {
            get
            {
                return (Symbol)GetValue(PrimaryButtonIconProperty);
            }
            set
            {
                SetValue(PrimaryButtonIconProperty, value);
            }
        }

        public static string PrimaryButtonTextDefaultValue { get; } = string.Empty;
        public static DependencyProperty PrimaryButtonTextProperty { get; } =
            DependencyProperty.Register(
                nameof(PrimaryButtonText),
                typeof(string),
                typeof(ContentDialogPage),
                new PropertyMetadata(PrimaryButtonTextDefaultValue, PrimaryButtonTextChanged));
        private static void PrimaryButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.PrimaryButton.Label = e.NewValue as string ?? PrimaryButtonTextDefaultValue;
        }
        public string PrimaryButtonText
        {
            get
            {
                return (string)GetValue(PrimaryButtonTextProperty);
            }
            set
            {
                SetValue(PrimaryButtonTextProperty, value);
            }
        }

        public static ICommand PrimaryButtonCommandDefaultValue { get; } = null;
        public static DependencyProperty PrimaryButtonCommandProperty { get; } =
            DependencyProperty.Register(
                nameof(PrimaryButtonCommand),
                typeof(ICommand),
                typeof(ContentDialogPage),
                new PropertyMetadata(PrimaryButtonCommandDefaultValue, PrimaryButtonCommandChanged));
        private static void PrimaryButtonCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                ((AppBatButtonCommand)contentDialogPage.PrimaryButton.Command).Command = e.NewValue as ICommand ?? PrimaryButtonCommandDefaultValue;
        }
        public ICommand PrimaryButtonCommand
        {
            get
            {
                return (ICommand)GetValue(PrimaryButtonCommandProperty);
            }
            set
            {
                SetValue(PrimaryButtonCommandProperty, value);
            }
        }

        public static object PrimaryButtonCommandParameterDefaultValue { get; } = null;
        public static DependencyProperty PrimaryButtonCommandParameterProperty { get; } =
            DependencyProperty.Register(
                nameof(PrimaryButtonCommandParameter),
                typeof(object),
                typeof(ContentDialogPage),
                new PropertyMetadata(PrimaryButtonCommandParameterDefaultValue, PrimaryButtonCommandParameterChanged));
        private static void PrimaryButtonCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.PrimaryButton.CommandParameter = e.NewValue as object ?? PrimaryButtonCommandParameterDefaultValue;
        }
        public object PrimaryButtonCommandParameter
        {
            get
            {
                return GetValue(PrimaryButtonCommandParameterProperty);
            }
            set
            {
                SetValue(PrimaryButtonCommandParameterProperty, value);
            }
        }

        public static Symbol SecondaryButtonIconDefaultValue { get; } = Symbol.Cancel;
        public static DependencyProperty SecondaryButtonIconProperty { get; } =
            DependencyProperty.Register(
                nameof(SecondaryButtonIcon),
                typeof(Symbol),
                typeof(ContentDialogPage),
                new PropertyMetadata(SecondaryButtonIconDefaultValue, SecondaryButtonIconPropertyChanged));
        private static void SecondaryButtonIconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.SecondaryButton.Icon = new SymbolIcon(e.NewValue as Symbol? ?? SecondaryButtonIconDefaultValue);
        }
        public Symbol SecondaryButtonIcon
        {
            get
            {
                return (Symbol)GetValue(SecondaryButtonIconProperty);
            }
            set
            {
                SetValue(SecondaryButtonIconProperty, value);
            }
        }

        public static string SecondaryButtonTextDefaultValue { get; } = string.Empty;
        public static DependencyProperty SecondaryButtonTextProperty { get; } =
            DependencyProperty.Register(
                nameof(SecondaryButtonText),
                typeof(string),
                typeof(ContentDialogPage),
                new PropertyMetadata(SecondaryButtonTextDefaultValue, SecondaryButtonTextChanged));
        private static void SecondaryButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.SecondaryButton.Label = e.NewValue as string ?? SecondaryButtonTextDefaultValue;
        }
        public string SecondaryButtonText
        {
            get
            {
                return (string)GetValue(SecondaryButtonTextProperty);
            }
            set
            {
                SetValue(SecondaryButtonTextProperty, value);
            }
        }

        public static ICommand SecondaryButtonCommandDefaultValue { get; } = null;
        public static DependencyProperty SecondaryButtonCommandProperty { get; } =
            DependencyProperty.Register(
                nameof(SecondaryButtonCommand),
                typeof(ICommand),
                typeof(ContentDialogPage),
                new PropertyMetadata(SecondaryButtonCommandDefaultValue, SecondaryButtonCommandChanged));
        private static void SecondaryButtonCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                ((AppBatButtonCommand)contentDialogPage.SecondaryButton.Command).Command = e.NewValue as ICommand ?? SecondaryButtonCommandDefaultValue;
        }
        public ICommand SecondaryButtonCommand
        {
            get
            {
                return (ICommand)GetValue(SecondaryButtonCommandProperty);
            }
            set
            {
                SetValue(SecondaryButtonCommandProperty, value);
            }
        }

        public static object SecondaryButtonCommandParameterDefaultValue { get; } = null;
        public static DependencyProperty SecondaryButtonCommandParameterProperty { get; } =
            DependencyProperty.Register(
                nameof(SecondaryButtonCommandParameter),
                typeof(object),
                typeof(ContentDialogPage),
                new PropertyMetadata(SecondaryButtonCommandParameterDefaultValue, SecondaryButtonCommandParameterChanged));
        private static void SecondaryButtonCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.SecondaryButton.CommandParameter = e.NewValue as object ?? SecondaryButtonCommandParameterDefaultValue;
        }
        public object SecondaryButtonCommandParameter
        {
            get
            {
                return GetValue(SecondaryButtonCommandParameterProperty);
            }
            set
            {
                SetValue(SecondaryButtonCommandParameterProperty, value);
            }
        }

        public static Visibility SecondaryButtonVisibilityDefaultValue { get; } = Visibility.Collapsed;
        public static DependencyProperty SecondaryButtonVisibilityProperty { get; } =
            DependencyProperty.Register(
                nameof(SecondaryButtonVisibility),
                typeof(Visibility),
                typeof(ContentDialogPage),
                new PropertyMetadata(SecondaryButtonVisibilityDefaultValue, SecondaryButtonVisibilityChanged));
        private static void SecondaryButtonVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentDialogPage = d as ContentDialogPage;
            if (contentDialogPage != null)
                contentDialogPage.SecondaryButton.Visibility = (e.NewValue as Visibility?) ?? SecondaryButtonVisibilityDefaultValue;
        }
        public Visibility SecondaryButtonVisibility
        {
            get
            {
                return (Visibility)GetValue(SecondaryButtonVisibilityProperty);
            }
            set
            {
                SetValue(SecondaryButtonVisibilityProperty, value);
            }
        }

        private AppBarButton PrimaryButton { get; }
        private AppBarButton SecondaryButton { get; }

        public ContentDialogPage()
        {
            PrimaryButton = GetPrimaryButton();
            SecondaryButton = GetSecondaryButton();
            BottomAppBar =
                new CommandBar
                {
                    PrimaryCommands =
                    {
                        PrimaryButton,
                        SecondaryButton
                    }
                };
        }

        private AppBarButton GetPrimaryButton()
        {
            var primaryButton =
                new AppBarButton
                {
                    Icon = new SymbolIcon(Symbol.Accept),
                    Label = PrimaryButtonTextDefaultValue,
                    Command = PrimaryButtonCommandDefaultValue
                };
            primaryButton.Command = new AppBatButtonCommand(primaryButton) { Command = PrimaryButtonCommandDefaultValue };
            primaryButton.Click +=
                        delegate
                        {
                            if (Frame.CanGoBack && (PrimaryButtonCommand?.CanExecute(PrimaryButtonCommandParameter) ?? true))
                                Frame.GoBack();
                        };

            return primaryButton;
        }
        private AppBarButton GetSecondaryButton()
        {
            var secondaryButton =
                new AppBarButton
                {
                    Icon = new SymbolIcon(Symbol.Cancel),
                    Label = SecondaryButtonTextDefaultValue,
                    CommandParameter = SecondaryButtonCommandParameterDefaultValue,
                    Visibility = SecondaryButtonVisibilityDefaultValue
                };
            secondaryButton.Command = new AppBatButtonCommand(secondaryButton) { Command = SecondaryButtonCommandDefaultValue };
            secondaryButton.Click +=
                delegate
                {
                    if (Frame.CanGoBack)
                        Frame.GoBack();
                };

            return secondaryButton;
        }
    }
}