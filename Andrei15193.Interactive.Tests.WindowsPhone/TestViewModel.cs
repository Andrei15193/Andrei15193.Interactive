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
        }

        public ICommand GoToState1Command { get; }
        public ICommand GoToState2Command { get; }
    }
}