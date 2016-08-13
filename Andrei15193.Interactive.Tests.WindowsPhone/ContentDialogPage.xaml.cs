using System;

namespace Andrei15193.Interactive.Tests.WindowsPhone
{
    [Obsolete("Asynchronous commands may trigger page navigation when it is not desired. Use StatePageNavigators instead to trigger page navigation when an InteractiveViewModel reaches a specific state.")]
    public sealed partial class ContentDialogPage
        : Andrei15193.Interactive.ContentDialogPage
    {
        public ContentDialogPage()
        {
            InitializeComponent();
        }
    }
}