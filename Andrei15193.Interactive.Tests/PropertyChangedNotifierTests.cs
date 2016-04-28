using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class PropertyChangedNotifierTests
    {
        private sealed class MockPropertyChangedNotifier
            : PropertyChangedNotifier
        {
            new public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            {
                base.NotifyPropertyChanged(propertyName);
            }
        }

        private MockPropertyChangedNotifier PropertyChangedNotifier { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            PropertyChangedNotifier = new MockPropertyChangedNotifier();
        }

        [TestMethod]
        public void TestPropertyChangedRaisesEvent()
        {
            var raiseCount = 0;

            PropertyChangedNotifier.PropertyChanged +=
                delegate
                {
                    raiseCount++;
                };
            PropertyChangedNotifier.NotifyPropertyChanged("test");

            Assert.AreEqual(1, raiseCount);
        }

        [TestMethod]
        public void TestSenderIsSameWithNotifier()
        {
            object expectedSender = PropertyChangedNotifier;
            object actualSender = null;
            PropertyChangedNotifier.PropertyChanged +=
                (sender, e) =>
                {
                    actualSender = sender;
                };

            PropertyChangedNotifier.NotifyPropertyChanged("test");

            Assert.AreSame(expectedSender, actualSender);
        }

        [TestMethod]
        public void TestPropertyNameIsEqualToTheNotifiedOne()
        {
            string expectedPropertyName = "test";
            string actualPropertyName = null;
            PropertyChangedNotifier.PropertyChanged +=
                (sender, e) =>
                {
                    actualPropertyName = e.PropertyName;
                };

            PropertyChangedNotifier.NotifyPropertyChanged(expectedPropertyName);

            Assert.AreEqual(expectedPropertyName, actualPropertyName, ignoreCase: false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCannotRaiseEventWithNullPropertyName()
            => PropertyChangedNotifier.NotifyPropertyChanged(null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestCannotRaiseEventWithEmptyStringPropertyName()
            => PropertyChangedNotifier.NotifyPropertyChanged(string.Empty);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestCannotRaiseEventInvalidIdentifierAsPropertyName()
            => PropertyChangedNotifier.NotifyPropertyChanged("1");

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestPropertyNameIsNotTrimmed()
            => PropertyChangedNotifier.NotifyPropertyChanged(" test ");
    }
}