using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// <para>
    /// A page sharing the same intent as a ContentDialog.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ContentDialog control is used as a modal dialog for a page. The problem with it
    /// is that it actually loads and fills the entirety of the page including the command bar.
    /// </para>
    /// <para>
    /// Because it is a control loading over the page, it does not visually adjust to different
    /// events, such as the on-screen keyboard. When using a Windows Phone this is problematic as
    /// no more than two (sometimes even that is too much) input controls can be used and only at
    /// the top of the ContentDialog. When a user focuses a text control, the on-screen keyboard
    /// pops up. In case of a <see cref="Page"/> the content is pushed upwards and the user is
    /// can see what they are typing, with ContentDialog the keyboard does not push it's content
    /// up and goes over what is shown on the ContentDialog. If the text input is placed where the
    /// keyboard would be, the keyboard would cover that control and the user would not be able to
    /// see what they are typing.
    /// </para>
    /// <para>
    /// The specialized <see cref="Page"/> aims to resolve this problem by providing a <see cref="Page"/>
    /// to use instead of a ContentDialog control.
    /// </para>
    /// </remarks>
    [Obsolete("Asynchronous commands may trigger page navigation when it is not desired. Use StatePageNavigators instead to trigger page navigation when an InteractiveViewModel reaches a specific state.")]
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
            {
                _command?.Execute(null);

                var frame = Window.Current.Content as Frame;
                if (frame != null && frame.CanGoBack)
                    frame.GoBack();
            }
        }

        /// <summary>
        /// The default value of the primary button <see cref="Symbol"/> that is used as icon.
        /// </summary>
        public static Symbol PrimaryButtonIconDefaultValue { get; } = Symbol.Accept;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the primary button <see cref="Symbol"/> that is used as icon.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonIconProperty =
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
        /// <summary>
        /// Gets or sets the primary button <see cref="Symbol"/> to be used as icon.
        /// </summary>
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

        /// <summary>
        /// The default value to use as label for the primary button.
        /// </summary>
        public static string PrimaryButtonTextDefaultValue { get; } = string.Empty;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the value to use as label for the primary button.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonTextProperty =
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
        /// <summary>
        /// Gets or sets the label of the primary button.
        /// </summary>
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

        /// <summary>
        /// The default value for the primary button <see cref="ICommand"/>.
        /// </summary>
        public static ICommand PrimaryButtonCommandDefaultValue { get; } = null;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the primary button <see cref="ICommand"/>.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonCommandProperty =
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
        /// <summary>
        /// Gets or sets the primary button<see cref="ICommand"/>.
        /// </summary>
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

        /// <summary>
        /// The default value of the primary button <see cref="ICommand"/> parameter.
        /// </summary>
        public static object PrimaryButtonCommandParameterDefaultValue { get; } = null;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the primary button <see cref="ICommand"/> parameter.
        /// </summary>
        public static readonly DependencyProperty PrimaryButtonCommandParameterProperty =
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
        /// <summary>
        /// Gets or sets the parameter of the primary button <see cref="ICommand"/>.
        /// </summary>
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

        /// <summary>
        /// The default value of the secondary button <see cref="Symbol"/> that is used as icon.
        /// </summary>
        public static Symbol SecondaryButtonIconDefaultValue { get; } = Symbol.Cancel;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the secondary button <see cref="Symbol"/> that is used as icon.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonIconProperty =
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
        /// <summary>
        /// Gets or sets the secondary button <see cref="Symbol"/> to be used as icon.
        /// </summary>
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

        /// <summary>
        /// The defaul value to use as label for the secondary button.
        /// </summary>
        public static string SecondaryButtonTextDefaultValue { get; } = string.Empty;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the value to use as label for the secondary button.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonTextProperty =
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
        /// <summary>
        /// Gets or sets the label of the secondary button.
        /// </summary>
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

        /// <summary>
        /// The default value for the secondary button <see cref="ICommand"/>.
        /// </summary>
        public static ICommand SecondaryButtonCommandDefaultValue { get; } = null;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the secondary button <see cref="ICommand"/>.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonCommandProperty =
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
        /// <summary>
        /// Gets or sets the secondary button <see cref="ICommand"/>.
        /// </summary>
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

        /// <summary>
        /// The default value for the secondary button <see cref="ICommand"/> parameter.
        /// </summary>
        public static object SecondaryButtonCommandParameterDefaultValue { get; } = null;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the secondary button <see cref="ICommand"/> parameter.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonCommandParameterProperty =
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
        /// <summary>
        /// Gets or sets the parameter of the secondary button <see cref="ICommand"/>.
        /// </summary>
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

        /// <summary>
        /// The default <see cref="Visibility"/> of the secondary button.
        /// </summary>
        public static Visibility SecondaryButtonVisibilityDefaultValue { get; } = Visibility.Collapsed;
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="Visibility"/> of the secondary button.
        /// </summary>
        public static readonly DependencyProperty SecondaryButtonVisibilityProperty =
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
        /// <summary>
        /// Gets or sets the visibility of the secondary button.
        /// </summary>
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

        /// <summary>
        /// Creates a new <see cref="ContentDialogPage"/> instance.
        /// </summary>
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

            return secondaryButton;
        }
    }
}