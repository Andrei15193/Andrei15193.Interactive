using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andrei15193.Interactive.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests.Validation
{
    [TestClass]
    public class AnyConstraintTests
    {
        [TestMethod]
        public async Task TestCreateAnyConstraintThatReturnsProvidedValidationError()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From(Constraint.From((object value) => new[] { new ValidationError("constraint") }))
                .Return(expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreateStartingWithPredicate()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From((object value) => false)
                .Return(expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreateStartingWithAsyncPredicate()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From((object value, CancellationToken cancellationToken) => Task.FromResult(false))
                .Return(expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreateContinuingConstraint()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From((object value) => false)
                .Or(Constraint.From((object value) => new[] { new ValidationError("test error") }))
                .Return(expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreateContinuingWithPredicate()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From((object value) => false)
                .Or(value => false)
                .Return(expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreateContinuingWithAsyncPredicate()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From((object value) => false)
                .Or((value, cancellationToken) => Task.FromResult(false))
                .Return(expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreatingWithValidationErrorProvider()
        {
            var expectedError = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From((object value) => false)
                .Return(() => expectedError);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestCreatingWithValidationErrorsProvider()
        {
            var expectedErrors =
                new[]
                {
                    new ValidationError("test error 1"),
                    new ValidationError("test error 2")
                };
            var anyConstraint = AnyConstraint
                .From((object value) => false)
                .Return(() => expectedErrors);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.IsTrue(expectedErrors.SequenceEqual(actualErrors));
        }

        [TestMethod]
        public async Task TestAnyConstraintIsNotSatisfiedIfAllConstraintsAreNotSatisfied()
        {
            var expectedErrors = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From(Constraint.From((object value) => new[] { new ValidationError("constraint") }))
                .Or((object value) => false)
                .Or((object value, CancellationToken cancellationToken) => Task.FromResult(false))
                .Return(expectedErrors);

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.AreSame(expectedErrors, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestAnyConstraintIsSatisfiedIfAtLeastOneIsSatisfied()
        {
            var error = new ValidationError("test error");
            var anyConstraint = AnyConstraint
                .From(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .Or((object value) => false)
                .Or((object value, CancellationToken cancellationToken) => Task.FromResult(false))
                .Return();

            var actualErrors = await anyConstraint.CheckAsync(new object());

            Assert.IsFalse(actualErrors.Any());
        }

        [TestMethod]
        public async Task ValidationErrorsAreReturnedInTheSameOrderTheyAreProvided()
        {
            var error1 = new ValidationError("test error 1");
            var error2 = new ValidationError("test error 2");
            var error3 = new ValidationError("test error 3");
            var constraint = AnyConstraint
                .From((object value) => false)
                .Return(error1, error2, error3);
            var expectedErrors =
                new[]
                {
                    error1,
                    error2,
                    error3
                };

            var actualErrors = await constraint.CheckAsync(new object());

            Assert.IsTrue(expectedErrors.SequenceEqual(actualErrors));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateStartingWithNullConstraint()
        {
            AnyConstraint.From((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateStartingWithNullPredicate()
        {
            AnyConstraint.From((AnyConstraint.Predicate<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateStartingWithNullAsyncPredicate()
        {
            AnyConstraint.From((AnyConstraint.AsyncPredicate<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateContinuingWithNullConstraint()
        {
            AnyConstraint
                .From((object value) => false)
                .Or((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateContinuingWithNullPredicate()
        {
            AnyConstraint
                .From((object value) => false)
                .Or((AnyConstraint.Predicate<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreateContinuingWithNullAsyncPredicate()
        {
            AnyConstraint
                .From((object value) => false)
                .Or((AnyConstraint.AsyncPredicate<object>)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreatingWithNullValidationError()
        {
            AnyConstraint
                .From((object value) => false)
                .Return((ValidationError)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreatingWithNullValidationErrors()
        {
            AnyConstraint
                .From((object value) => false)
                .Return((IEnumerable<ValidationError>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreatingWithNullValidationErrorProvider()
        {
            AnyConstraint
                .From((object value) => false)
                .Return((AnyConstraint.ValidationErrorProvider)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCreatingWithNullValidationErrorsProvider()
        {
            AnyConstraint
                .From((object value) => false)
                .Return((AnyConstraint.ValidationErrorsProvider)null);
        }
    }
}