using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class MappingConverterTests
    {
        private MappingConverter _Converter { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            _Converter = new MappingConverter();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _Converter = null;
        }

        [TestMethod]
        public void TestConvertingWithoutAnyMappingYieldsTheSameValue()
        {
            var value = new object();

            var result = _Converter.Convert(value, null);

            Assert.AreSame(value, result);
        }

        [TestMethod]
        public void TestConvertingBasedOnProvidedMapping()
        {
            var value = Guid.NewGuid().ToString();
            var destination = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = value, To = destination });

            var result = _Converter.Convert(value, typeof(object));

            Assert.AreSame(destination, result);
        }

        [TestMethod]
        public void TestConvertingBackBasedOnProvidedMapping()
        {
            var source = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = source, To = value });

            var result = _Converter.ConvertBack(value, typeof(object));

            Assert.AreSame(source, result);
        }

        [TestMethod]
        public void TestConvertingBasedOnProvidedMappingButWithMismatchedTargetTypeProvidesSameValue()
        {
            var value = Guid.NewGuid().ToString();
            var destination = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = value, To = destination });

            var result = _Converter.Convert(value, typeof(int));

            Assert.AreSame(value, result);
        }

        [TestMethod]
        public void TestConvertingBackBasedOnProvidedMappingButWithMismatchedTargetTypeProvidesSameValue()
        {
            var source = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = source, To = value });

            var result = _Converter.ConvertBack(value, typeof(int));

            Assert.AreSame(value, result);
        }

        [TestMethod]
        public void TestConvertingBasedOnProvidedMappingWithNullDestinationAndValueTargetTypeProvidesSameValue()
        {
            var value = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = value, To = null });

            var result = _Converter.Convert(value, typeof(int));

            Assert.AreSame(value, result);
        }

        [TestMethod]
        public void TestConvertingBackBasedOnProvidedMappingWithNullDestinationAndValueTargetTypeProvidesSameValue()
        {
            var value = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = value, To = null });

            var result = _Converter.ConvertBack(value, typeof(int));

            Assert.AreSame(value, result);
        }

        [TestMethod]
        public void TestConvertingBasedOnProvidedMappingWithNullDestinationAndNullableValueTargetTypeProvidesNull()
        {
            var value = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = value, To = null });

            var result = _Converter.Convert(value, typeof(int?));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestConvertingBackBasedOnProvidedMappingWithNullDestinationAndNullableValueTargetTypeProvidesNull()
        {
            var value = Guid.NewGuid().ToString();

            _Converter.Mappings.Add(new Mapping { From = null, To = value });

            var result = _Converter.ConvertBack(value, typeof(int?));

            Assert.IsNull(result);
        }
    }
}