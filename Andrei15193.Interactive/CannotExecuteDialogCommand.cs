using System;
using System.Diagnostics;
using System.Windows.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents an <see cref="ICommand"/> wrapper that when executed will attempt
    /// to execute the wrapped <see cref="Command"/>. If it is unavailable then it
    /// shows a <see cref="MessageDialog"/> informing the user that the action cannot
    /// be carried out.
    /// </summary>
    public class CannotExecuteDialogCommand
        : DependencyObject, ICommand
    {
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the title of the confirm <see cref="MessageDialog"/>.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(CannotExecuteDialogCommand),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the title of the confirm <see cref="MessageDialog"/>.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the content of the confirm <see cref="MessageDialog"/>.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                nameof(Content),
                typeof(string),
                typeof(CannotExecuteDialogCommand),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content of the confirm <see cref="MessageDialog"/>.
        /// </summary>
        public string Content
        {
            get
            {
                return (string)GetValue(ContentProperty);
            }
            set
            {
                SetValue(ContentProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the acknowledged button text.
        /// </summary>
        public static readonly DependencyProperty AcknowledgedTextProperty =
            DependencyProperty.Register(
                nameof(AcknowledgedText),
                typeof(string),
                typeof(CannotExecuteDialogCommand),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text for the acknowledged button.
        /// </summary>
        public string AcknowledgedText
        {
            get
            {
                return (string)GetValue(AcknowledgedTextProperty);
            }
            set
            {
                SetValue(AcknowledgedTextProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="DependencyProperty"/> for <see cref="MessageDialogOptions"/>
        /// when showing the confirm <see cref="MessageDialog"/>.
        /// The default is <see cref="MessageDialogOptions.AcceptUserInputAfterDelay"/>.
        /// </summary>
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register(
                nameof(Options),
                typeof(MessageDialogOptions),
                typeof(CannotExecuteDialogCommand),
                new PropertyMetadata(MessageDialogOptions.AcceptUserInputAfterDelay));

        /// <summary>
        /// Gets or sets <see cref="MessageDialogOptions"/> for the confirm
        /// <see cref="MessageDialogOptions"/>.
        /// The default is <see cref="MessageDialogOptions.AcceptUserInputAfterDelay"/>.
        /// </summary>
        public MessageDialogOptions Options
        {
            get
            {
                return (MessageDialogOptions)GetValue(OptionsProperty);
            }
            set
            {
                SetValue(OptionsProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the wrapped <see cref="ICommand"/>.
        /// </summary>
        public static readonly DependencyProperty CommandProperty
            = DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(CannotExecuteDialogCommand),
                new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets the wrapped <see cref="ICommand"/> to be executed.
        /// </summary>
        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        event EventHandler ICommand.CanExecuteChanged { add { } remove { } }

        bool ICommand.CanExecute(object parameter)
            => true;

        /// <summary>
        /// Checks whether the wrapped <see cref="Command"/> can be executed, if so then
        /// it executes it; otherwise a <see cref="MessageDialog"/> is shown informing
        /// the user that the action cannot be carried out.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public void Execute(object parameter)
        {
            if (Command.CanExecute(parameter))
                Command.Execute(parameter);
            else
            {
                var messageDialog = _GetMessageDialog();
                var showOperation = messageDialog.ShowAsync();
                showOperation.Completed =
                    (asyncInfo, asyncStatus) =>
                    {
                        Debug.WriteLine($"Cannot Execute Dialog closed with {asyncStatus} status.");
                    };
            }
        }

        private MessageDialog _GetMessageDialog()
        {
            var messageDialog =
                new MessageDialog(Content)
                {
                    Options = Options
                };

            if (!string.IsNullOrWhiteSpace(Title))
                messageDialog.Title = Title;

            messageDialog.Commands.Add(
                new UICommand
                {
                    Label = (AcknowledgedText ?? string.Empty),
                    Invoked = delegate { }
                });
            messageDialog.DefaultCommandIndex = 0;
            return messageDialog;
        }
    }
}