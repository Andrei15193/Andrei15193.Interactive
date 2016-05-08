using System.Windows.Input;

namespace Andrei15193.Interactive.Tests.WindowsPhone
{
    public class TestViewModel
        : ViewModel<object>
    {
        public TestViewModel()
            : base(new object())
        {
            GoToState1Command = GetTransitionCommand("State1");
            GoToState2Command = GetTransitionCommand("State2");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            TransitionToAsync("State1");
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public ICommand GoToState1Command { get; }
        public ICommand GoToState2Command { get; }
    }
}