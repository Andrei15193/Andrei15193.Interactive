using System.Threading.Tasks;
using System.Windows.Input;
using Andrei15193.Interactive.Validation;

namespace Andrei15193.Interactive.Tests.WindowsPhone
{
    public class TestInteractiveViewModel
        : InteractiveViewModel<object>
    {
        private int transitionCount = 0;

        public TestInteractiveViewModel()
            : base(new object())
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
                    transitionCount++;
                    Errors.Clear();
                    Errors.Add(new ValidationError("Error " + transitionCount));
                });

            BeginTransitionCommand = GetTransitionCommand("State2").BindTo("State1", "State3");

            TransitionToAsync("State1");
        }

        public ICommand BeginTransitionCommand { get; }
    }
}