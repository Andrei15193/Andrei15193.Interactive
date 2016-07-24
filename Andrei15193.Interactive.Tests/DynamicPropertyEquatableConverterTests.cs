using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Data;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class DynamicPropertyEquatableConverterTests
    {
        private IValueConverter Converter { get; } = new DynamicPropertyEquatableConverter();

        [TestMethod]
        public void TestConvertingNonCollectionObjectRetunsDynamicPropertyEquatable()
        {
            var value = new object();

            var result = Converter.Convert(value, typeof(object), null, null);

            Assert.IsInstanceOfType(result, typeof(DynamicPropertyEquatable));
        }

        [TestMethod]
        public void TestConvertingCollectionObjectRetunsACollection()
        {
            var value = new List<object>();

            var result = Converter.Convert(value, typeof(object), null, null);

            Assert.IsInstanceOfType(result, typeof(IEnumerable));
        }

        [TestMethod]
        public void TestConvertingObservableCollectionObjectRetunsAnObservableCollection()
        {
            var value = new ObservableCollection<object>();

            var result = Converter.Convert(value, typeof(object), null, null);

            Assert.IsInstanceOfType(result, typeof(IEnumerable));
            Assert.IsInstanceOfType(result, typeof(INotifyCollectionChanged));
        }

        [TestMethod]
        public void TestConvertingBackReturnsTheSameInstance()
        {
            var value = new object();

            var result = Converter.Convert(value, typeof(object), null, null);
            var convertedBackValue = Converter.ConvertBack(result, typeof(object), null, null);

            Assert.AreSame(value, convertedBackValue);
        }

        [TestMethod]
        public void TestConvertingBackACollectionReturnsTheSameInstance()
        {
            var value = new List<object>();

            var result = Converter.Convert(value, typeof(object), null, null);
            var convertedBackValue = Converter.ConvertBack(result, typeof(object), null, null);

            Assert.AreSame(value, convertedBackValue);
        }

        [TestMethod]
        public void TestConvertingBackAnObservableCollectionReturnsTheSameInstance()
        {
            var value = new ObservableCollection<object>();

            var result = Converter.Convert(value, typeof(object), null, null);
            var convertedBackValue = Converter.ConvertBack(result, typeof(object), null, null);

            Assert.AreSame(value, convertedBackValue);
        }

        [TestMethod]
        public void TestConvertingNullReturnsDynamicPropertyEquatableWithWrappedNull()
        {
            var result = (DynamicPropertyEquatable)Converter.Convert(null, typeof(object), null, null);

            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void TestConvertingBackNullReturnsNull()
        {
            var result = Converter.ConvertBack(null, typeof(object), null, null);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestConvertingNullAndThenBackReturnsNull()
        {
            var convertedValue = Converter.Convert(null, typeof(object), null, null);
            var convertedBackValue = Converter.ConvertBack(convertedValue, typeof(object), null, null);

            Assert.IsNull(convertedBackValue);
        }
    }
}