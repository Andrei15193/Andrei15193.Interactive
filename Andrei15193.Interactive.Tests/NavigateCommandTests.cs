using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class NavigateCommandTests
    {
        [TestMethod]
        public void TestCannotExecuteNavigateCommandIfPageIsNotSet()
        {
            var navigateCommand = new NavigateCommand();

            Assert.IsFalse(navigateCommand.CanExecute(null));
        }

        [TestMethod]
        public void TestCannotExecuteNavigateCommandIfPageIsSetToAnInvalidType()
        {
            var navigateCommand = new NavigateCommand { Page = "someTypeThatDoesNotExist" };

            Assert.IsFalse(navigateCommand.CanExecute(null));
        }
    }
}