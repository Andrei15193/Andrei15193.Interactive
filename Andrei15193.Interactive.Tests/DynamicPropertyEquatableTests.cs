using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class DynamicPropertyEquatableTests
    {
        private class TestObject
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }

        [TestMethod]
        public void TestComparingTwoInstancesWithNoPropertyPathAndSameWrappedValueReturnsTrue()
        {
            var firstValue = new TestObject();
            var secondValue = firstValue;

            var first = new DynamicPropertyEquatable(firstValue, string.Empty);
            var second = new DynamicPropertyEquatable(secondValue, string.Empty);

            Assert.IsTrue(first.Equals(second));
        }

        [TestMethod]
        public void TestComparingTwoInstancesWithPropertyPathAndSameWrappedValueReturnsTrue()
        {
            var firstValue = new TestObject();
            var secondValue = firstValue;

            var first = new DynamicPropertyEquatable(firstValue, nameof(TestObject.Id));
            var second = new DynamicPropertyEquatable(secondValue, nameof(TestObject.Id));

            Assert.IsTrue(first.Equals(second));
        }

        [TestMethod]
        public void TestComparingTwoInstancesWithPropertyPathReturnsTrueWhenPropertiesHaveEqualValue()
        {
            var firstValue = new TestObject
            {
                Id = new Random().Next()
            };
            var secondValue = new TestObject
            {
                Id = firstValue.Id
            };

            var first = new DynamicPropertyEquatable(firstValue, nameof(TestObject.Id));
            var second = new DynamicPropertyEquatable(secondValue, nameof(TestObject.Id));

            Assert.IsTrue(first.Equals(second));
        }

        [TestMethod]
        public void TestComparingTwoNullWrappedValuesReturnsTrue()
        {
            var first = new DynamicPropertyEquatable(null);
            var second = new DynamicPropertyEquatable(null);

            Assert.IsTrue(first.Equals(second));
        }

        [TestMethod]
        public void TestComparingTwoInstancesWithPropertyPathReturnsFalseWhenPropertiesHaveDistinctValue()
        {
            var firstValue = new TestObject
            {
                Id = new Random().Next()
            };
            var secondValue = new TestObject
            {
                Id = firstValue.Id + 1
            };

            var first = new DynamicPropertyEquatable(firstValue, nameof(TestObject.Id));
            var second = new DynamicPropertyEquatable(secondValue, nameof(TestObject.Id));

            Assert.IsFalse(first.Equals(second));
        }

        [TestMethod]
        public void TestSpecifyingNullAsPropertyPathForOneInstanceWillUseTHePropertyPathOfTheOther()
        {
            var firstValue = new TestObject
            {
                Id = new Random().Next()
            };
            var secondValue = new TestObject
            {
                Id = firstValue.Id
            };

            var first = new DynamicPropertyEquatable(firstValue, null);
            var second = new DynamicPropertyEquatable(secondValue, nameof(TestObject.Id));

            Assert.IsTrue(first.Equals(second));
        }

        [TestMethod]
        public void TestHashCodeOfWrapperIsEqualToHashCodeOfSpecifiedProperty()
        {
            var value = new TestObject
            {
                Id = new Random().Next()
            };

            var wrapper = new DynamicPropertyEquatable(value, nameof(TestObject.Id));

            Assert.AreEqual(value.Id.GetHashCode(), wrapper.GetHashCode());
        }

        [TestMethod]
        public void TestHashCodeIsZeroForNullProperty()
        {
            var value = new TestObject
            {
                Id = new Random().Next(),
                Value = null
            };

            var wrapper = new DynamicPropertyEquatable(value, nameof(TestObject.Value));

            Assert.AreEqual(0, wrapper.GetHashCode());
        }
    }
}