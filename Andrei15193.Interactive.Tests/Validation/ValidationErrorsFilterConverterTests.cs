using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Andrei15193.Interactive.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Data;

namespace Andrei15193.Interactive.Tests.Validation
{
    [TestClass]
    public class ValidationErrorsFilterConverterTests
    {
        private static IValueConverter Converter { get; } = new ValidationErrorsFilterConverter();
        private static ValidationError validationError1 { get; } = new ValidationError("test error 1");
        private static ValidationError validationError2 { get; } = new ValidationError("test error 2", "test member 1");
        private static ValidationError validationError3 { get; } = new ValidationError("test error 3", "test member 2");
        private static IEnumerable<ValidationError> validationErrors { get; } =
            new[]
            {
                validationError1,
                validationError2,
                validationError3
            };

        [TestMethod]
        public void TestConvertingNullReturnsNull()
        {
            var result = Converter.Convert(null, typeof(object), null, null);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestFilterValidationErrorsWithNullParameter()
        {
            var result = Convert(null);

            Assert.IsTrue(
                new[]
                {
                    validationError1
                }.SequenceEqual(result));
        }

        [TestMethod]
        public void TestFilterValidationErrorsWithStringParameter()
        {
            var result = Convert(validationError2.MemberName);

            Assert.IsTrue(
                new[]
                {
                    validationError2
                }.SequenceEqual(result));
        }

        [TestMethod]
        public void TestFilterValidationErrorsWithStringCollectionParameter()
        {
            var result = Convert(new[] { validationError2.MemberName, validationError3.MemberName });

            Assert.IsTrue(
                new[]
                {
                    validationError2,
                    validationError3
                }.SequenceEqual(result));
        }

        [TestMethod]
        public void TestFilterValidationErrorsWithStringCollectionParameterContainingAlsoNull()
        {
            var result = Convert(new[] { validationError2.MemberName, validationError3.MemberName, null });

            Assert.IsTrue(
                new[]
                {
                    validationError1,
                    validationError2,
                    validationError3
                }.SequenceEqual(result));
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestConvertBackIsNotImplemented()
            => Converter.ConvertBack(null, null, null, null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestWithInvalidParameterTypeThrowsException()
            => Convert(default(int));

        [TestMethod]
        public void TestConvertingAnObseravleCollectionReturnsAnObservableCollection()
        {
            var observableCollection = new ObservableCollection<ValidationError>();

            var result = Converter.Convert(observableCollection, typeof(object), null, null);

            Assert.IsInstanceOfType(result, typeof(IEnumerable<ValidationError>));
            Assert.IsInstanceOfType(result, typeof(INotifyCollectionChanged));
        }

        private static IEnumerable<ValidationError> Convert(object paramter)
            => (IEnumerable<ValidationError>)Converter.Convert(validationErrors, typeof(object), paramter, null);
    }
}