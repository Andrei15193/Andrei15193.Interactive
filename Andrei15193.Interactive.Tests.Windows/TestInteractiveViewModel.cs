using System.Threading.Tasks;
using System.Windows.Input;

namespace Andrei15193.Interactive.Tests.Windows
{
    public class TestInteractiveViewModel
        : InteractiveViewModel
    {
        public TestInteractiveViewModel()
        {
            CreateActionState(
                "State2",
                async context =>
                {
                    if (context.PreviousState == "State1")
                        context.NextState = "State3";
                    else
                        context.NextState = "State1";
                    await Task.Delay(2000);
                });

            BeginTransitionCommand = GetTransitionCommand("State2").BindTo("State1", "State3");

            TransitionToAsync("State1");
        }

        public ICommand BeginTransitionCommand { get; }
    }
}