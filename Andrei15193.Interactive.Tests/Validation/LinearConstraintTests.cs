using System;
using System.Linq;
using System.Threading.Tasks;
using Andrei15193.Interactive.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests.Validation
{
    [TestClass]
    public class LinearConstraintTests
    {
        [TestMethod]
        public async Task TestLinearConstraintStartingWithAnUnsatisfiedConstraintAndEndedInASatisfiedOne()
        {
            var expectedError = new ValidationError("test error");
            var constraint = LinearConstraint
                .StartingWith(Constraint.From((object value) => new[] { expectedError }))
                .AndEndedWith(Constraint.From((object value) => new ValidationError[0]));

            var actualErrors = await constraint.CheckAsync(new object());

            Assert.AreSame(expectedError, actualErrors.Single());
        }
        [TestMethod]
        public async Task TestLinearConstraintStartingAndEndedWithAnUnsatisfiedConstraint()
        {
            var error1 = new ValidationError("test error 1");
            var error2 = new ValidationError("test error 2");
            var constraint = LinearConstraint
                .StartingWith(Constraint.From((object value) => new[] { error1 }))
                .AndEndedWith(Constraint.From((object value) => new[] { error2 }));
            var expectedErrors =
                new[]
                {
                    error1,
                    error2
                };

            var actialErrors = await constraint.CheckAsync(new object());

            Assert.IsTrue(expectedErrors.SequenceEqual(actialErrors));
        }
        [TestMethod]
        public async Task TestLinearConstraintReturnsValidationErrorsForAllUnsatisfiedConstraintsInABatch()
        {
            var error1 = new ValidationError("test error 1");
            var error2 = new ValidationError("test error 2");
            var error3 = new ValidationError("test error 3");
            var constraint = LinearConstraint
                .StartingWith(Constraint.From((object value) => new[] { error1 }))
                .FollowedBy(Constraint.From((object value) => new[] { error2 }))
                .AndEndedWith(Constraint.From((object value) => new[] { error3 }));
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
        public async Task TestLinearConstraintReturnsValidationErrorsOnlyUntilFirstCheckWhenThereAreAny()
        {
            var error1 = new ValidationError("test error 1");
            var error2 = new ValidationError("test error 2");
            var error3 = new ValidationError("test error 3");
            var constraint = LinearConstraint
                .StartingWith(Constraint.From((object value) => new[] { error1 }))
                .FollowedBy(Constraint.From((object value) => new[] { error2 }))
                .CheckedAndEndedWith(Constraint.From((object value) => new[] { error3 }));
            var expectedErrors =
                new[]
                {
                    error1,
                    error2
                };

            var actualErrors = await constraint.CheckAsync(new object());

            Assert.IsTrue(expectedErrors.SequenceEqual(actualErrors));
        }
        [TestMethod]
        public async Task TestLinearConstraintReturnsValidationErrorsFromSecondBatchWhenTheFirstOneHasNone()
        {
            var error1 = new ValidationError("test error 1");
            var error2 = new ValidationError("test error 2");
            var constraint = LinearConstraint
                .StartingWith(Constraint.From((object value) => new ValidationError[] { }))
                .FollowedBy(Constraint.From((object value) => new ValidationError[] { }))
                .CheckedAndFollowedBy(Constraint.From((object value) => new[] { error1 }))
                .AndEndedWith(Constraint.From((object value) => new[] { error2 }));
            var expectedErrors =
                new[]
                {
                    error1,
                    error2
                };

            var actualErrors = await constraint.CheckAsync(new object());

            Assert.IsTrue(expectedErrors.SequenceEqual(actualErrors));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintStartingWithNullConstraint()
        {
            LinearConstraint.StartingWith((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintStartingWithNullCallback()
        {
            LinearConstraint.StartingWith((Constraint.Callback<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintStartingWithNullAsyncCallback()
        {
            LinearConstraint.StartingWith((Constraint.AsyncCallback<object>)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintFollowedByNullConstraint()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .FollowedBy((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintFollowedByNullCallback()
        {
            LinearConstraint
                .StartingWith((object value) => Enumerable.Empty<ValidationError>())
                .FollowedBy((Constraint.Callback<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintFollowedByNullAsyncCallback()
        {
            LinearConstraint
                .StartingWith((object value) => Enumerable.Empty<ValidationError>())
                .FollowedBy((Constraint.AsyncCallback<object>)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintCheckedAndFollowedByNullConstraint()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndFollowedBy((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintCheckedAndFollowedByNullCallback()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndFollowedBy((Constraint.Callback<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintCheckedAndFollowedByNullAsyncCallback()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndFollowedBy((Constraint.AsyncCallback<object>)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintEndedWithNullConstraint()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndEndedWith((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintEndedWithNullCallback()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndEndedWith((Constraint.Callback<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintEndedWithNullAsyncCallback()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndEndedWith((Constraint.AsyncCallback<object>)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintCheckedAndEndedWithNullConstraint()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndEndedWith((IConstraint<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintCheckedAndEndedWithNullCallback()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndEndedWith((Constraint.Callback<object>)null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLinearConstraintCheckedAndEndedWithNullAsyncCallback()
        {
            LinearConstraint
                .StartingWith(Constraint.From((object value) => Enumerable.Empty<ValidationError>()))
                .CheckedAndEndedWith((Constraint.AsyncCallback<object>)null);
        }
    }
}