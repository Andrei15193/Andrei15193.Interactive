using System.Threading.Tasks;
using System.Windows.Input;

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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            TransitionToAsync("State1");
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public ICommand BeginTransitionCommand { get; }
    }
}