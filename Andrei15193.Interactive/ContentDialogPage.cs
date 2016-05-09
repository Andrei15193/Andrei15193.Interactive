using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    public class ContentDialogPage
        : Page
    {
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
            {
                ICommand command = e.OldValue as ICommand;

                if (command != null)
                    command.CanExecuteChanged -=
                        delegate
                        {
                            contentDialogPage.PrimaryButton.IsEnabled = contentDialogPage.PrimaryButtonCommand.CanExecute(contentDialogPage.PrimaryButtonCommandParameter);
                        };

                command = e.NewValue as ICommand;
                if (command != null)
                {
                    command.CanExecuteChanged +=
                        delegate
                        {
                            contentDialogPage.PrimaryButton.IsEnabled = contentDialogPage.PrimaryButtonCommand.CanExecute(contentDialogPage.PrimaryButtonCommandParameter);
                        };
                    contentDialogPage.PrimaryButton.Command = command;
                }
                else
                    contentDialogPage.PrimaryButton.Command = PrimaryButtonCommandDefaultValue;
            }
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
            {
                ICommand command = e.OldValue as ICommand;

                if (command != null)
                    command.CanExecuteChanged -=
                        delegate
                        {
                            contentDialogPage.SecondaryButton.IsEnabled = contentDialogPage.SecondaryButtonCommand.CanExecute(contentDialogPage.SecondaryButtonCommandParameter);
                        };

                command = e.NewValue as ICommand;
                if (command != null)
                {
                    command.CanExecuteChanged +=
                        delegate
                        {
                            contentDialogPage.SecondaryButton.IsEnabled = contentDialogPage.SecondaryButtonCommand.CanExecute(contentDialogPage.SecondaryButtonCommandParameter);
                        };
                    contentDialogPage.SecondaryButton.Command = command;
                }
                else
                    contentDialogPage.SecondaryButton.Command = SecondaryButtonCommandDefaultValue;
            }
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
                    Command = PrimaryButtonCommandDefaultValue,
                    CommandParameter = PrimaryButtonCommandParameterDefaultValue
                };
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
                    Command = SecondaryButtonCommandDefaultValue,
                    CommandParameter = SecondaryButtonCommandParameterDefaultValue,
                    Visibility = SecondaryButtonVisibilityDefaultValue
                };
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