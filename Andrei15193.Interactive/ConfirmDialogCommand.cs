using System;
using System.Diagnostics;
using System.Windows.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents an <see cref="ICommand"/> wrapper that will first display a confirm
    /// dialog. If the user proceeds then the wrapped <see cref="ICommand"/> will be
    /// executed.
    /// </summary>
    public class ConfirmDialogCommand
        : DependencyObject, ICommand
    {
        /// <summary>
        /// The <see cref="DependencyProperty"/> for the title of the configm <see cref="MessageDialog"/>.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ConfirmDialogCommand),
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
        /// The <see cref="DependencyProperty"/> for the content of the configm <see cref="MessageDialog"/>.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                nameof(Content),
                typeof(string),
                typeof(ConfirmDialogCommand),
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
        /// The <see cref="DependencyProperty"/> for the submit button text.
        /// </summary>
        public static readonly DependencyProperty SubmitTextProperty =
            DependencyProperty.Register(
                nameof(SubmitText),
                typeof(string),
                typeof(ConfirmDialogCommand),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text for the submit button.
        /// </summary>
        public string SubmitText
        {
            get
            {
                return (string)GetValue(SubmitTextProperty);
            }
            set
            {
                SetValue(SubmitTextProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the cancel button text.
        /// </summary>
        public static readonly DependencyProperty CancelTextProperty =
            DependencyProperty.Register(
                nameof(CancelText),
                typeof(string),
                typeof(ConfirmDialogCommand),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text for the submit button.
        /// </summary>
        public string CancelText
        {
            get
            {
                return (string)GetValue(CancelTextProperty);
            }
            set
            {
                SetValue(CancelTextProperty, value);
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
                typeof(ConfirmDialogCommand),
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
                typeof(ConfirmDialogCommand),
                new PropertyMetadata(null, _CommandPropertyChanged));

        private static void _CommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var confirmDialogCommand = (ConfirmDialogCommand)d;

            var command = e.OldValue as ICommand;
            if (command != null)
                command.CanExecuteChanged -= confirmDialogCommand._RaiseCanExecuteChanged;

            command = e.NewValue as ICommand;
            if (command != null)
                command.CanExecuteChanged += confirmDialogCommand._RaiseCanExecuteChanged;

            confirmDialogCommand._RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Gets or sets the wrapped <see cref="ICommand"/> to be executed when the
        /// user confirms the action.
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

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Indicates whether the command can be executed in the current context.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        /// <returns>
        /// Returns true if the command can be executed; false otherwise.
        /// </returns>
        public bool CanExecute(object parameter)
            => (Command?.CanExecute(parameter) ?? false);

        /// <summary>
        /// Shows a confirm <see cref="MessageDialog"/>. If the user confirms the
        /// operation then the wrapped <see cref="ICommand"/> will be executed.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public void Execute(object parameter)
        {
            var operationCanceled = false;
            var command = Command;
            var messageDialog = _GetMessageDialog(
                delegate
                {
                    Debug.WriteLineIf(operationCanceled, "Operation has been canceled, yet it was requested to execute the command.");
                    if (!operationCanceled)
                        command.Execute(parameter);
                });
            var showOperation = messageDialog.ShowAsync();

            EventHandler canExecuteChanged =
                delegate
                {
                    if (!command.CanExecute(parameter))
                    {
                        operationCanceled = true;
                        showOperation.Cancel();
                        Debug.WriteLine("MessageDialog.ShowAsync() has been canceled");
                    }
                };

            command.CanExecuteChanged += canExecuteChanged;
            Debug.WriteLine($"Subscribed to {nameof(ICommand.CanExecuteChanged)}");
            showOperation.Completed =
                delegate
                {
                    operationCanceled = true;
                    command.CanExecuteChanged -= canExecuteChanged;
                    Debug.WriteLine($"Unsubscribed from {nameof(ICommand.CanExecuteChanged)}");
                };

        }

        private MessageDialog _GetMessageDialog(UICommandInvokedHandler submitHandler)
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
                    Label = (SubmitText ?? string.Empty),
                    Invoked = submitHandler
                });
            messageDialog.Commands.Add(
                new UICommand
                {
                    Label = (CancelText ?? string.Empty),
                    Invoked = delegate { }
                });
            messageDialog.DefaultCommandIndex = messageDialog.CancelCommandIndex = 1;

            return messageDialog;
        }

        private void _RaiseCanExecuteChanged(object sender = null, EventArgs e = null)
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}