﻿using System;
using System.Collections.Generic;
using System.Linq;
using Andrei15193.Interactive.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests.Validation
{
    [TestClass]
    public class ConstraintsTests
    {
        private readonly ICollection<string> _constraintNamesToDeregister
            = new HashSet<string>(StringComparer.Ordinal);

        [TestInitialize]
        public void TestInitialize()
        {
            _constraintNamesToDeregister.Clear();
        }

        [TestCleanup]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestCleanup()
        {
            Constraints.DeregisterFor<object>();
            foreach (var constraintNameToDeregister in _constraintNamesToDeregister)
                Constraints.DeregisterFor<object>(constraintNameToDeregister);
        }

        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestRegisteringAConstraintWillMakeItRetrievable()
        {
            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint);

            var actualConstraint = Constraints.GetFor<object>();

            Assert.AreSame(expectedConstraint, actualConstraint);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestTryingToRegisterAConstraintForTheSameTypeWithoutNameThrowsException()
        {
            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint);
            Constraints.RegisterFor(expectedConstraint);
        }

        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestRegisteringAConstraintWithANameWillMakeItRetrievableByThatName()
        {
            var constraintName = "test";
            _constraintNamesToDeregister.Add(constraintName);

            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint, constraintName);

            var actualConstraint = Constraints.GetFor<object>(constraintName);

            Assert.AreSame(expectedConstraint, actualConstraint);
        }
        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestRegisteringTwoConstraintsWithDifferentNamesForTheSameTypeMakeThemRetrievableAccordingly()
        {
            var constraintName1 = "test 1";
            _constraintNamesToDeregister.Add(constraintName1);

            var expectedConstraint1 = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint1, constraintName1);

            var constraintName2 = "test 2";
            _constraintNamesToDeregister.Add(constraintName2);

            var expectedConstraint2 = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint2, constraintName2);

            var actualConstraint1 = Constraints.GetFor<object>(constraintName1);
            var actualConstraint2 = Constraints.GetFor<object>(constraintName2);

            Assert.AreNotSame(expectedConstraint1, expectedConstraint2);
            Assert.AreSame(expectedConstraint1, actualConstraint1);
            Assert.AreSame(expectedConstraint2, actualConstraint2);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestTryingToRegisterAConstraintForTheSameTypeAndNameThrowsException()
        {
            var constraintName = "test";
            _constraintNamesToDeregister.Add(constraintName);

            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint, constraintName);
            Constraints.RegisterFor(expectedConstraint, constraintName);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestNamesAreTrimmedWhenRegisteringConstraints()
        {
            var constraintName = "test";
            _constraintNamesToDeregister.Add(constraintName);

            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(expectedConstraint, constraintName);
            Constraints.RegisterFor(expectedConstraint, constraintName + " ");
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestGettingAConstraintForATypeThatDoesNotExistsThrowsException()
        {
            Constraints.GetFor<object>();
        }
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestGettingAConstraintForATypeAndNameThatDoesNotExistsThrowsException()
        {
            var constraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(constraint);

            Constraints.GetFor<object>("test");
        }

        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestTryingToGetAConstraintForATypeThatDoesNotExistsReturnsNull()
        {
            var expectedConstraint = Constraints.TryGetFor<object>();

            Assert.IsNull(expectedConstraint);
        }
        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestTryingToGetAConstraintForATypeAndNameThatDoesNotExistsThrowsException()
        {
            var constraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            Constraints.RegisterFor(constraint);

            var expectedConstraint = Constraints.TryGetFor<object>("test");

            Assert.IsNull(expectedConstraint);
        }

        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestDeregisteringAConstraintMakesItUnavailable()
        {
            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());

            Constraints.RegisterFor(expectedConstraint);
            Constraints.DeregisterFor<object>();

            var actualConstraint = Constraints.TryGetFor<object>();

            Assert.IsNull(actualConstraint);
        }
        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestDeregisteringAConstraintWithANameMakesItUnavailable()
        {
            var constraintName = "test";
            _constraintNamesToDeregister.Add(constraintName);
            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());

            Constraints.RegisterFor(expectedConstraint, constraintName);
            Constraints.DeregisterFor<object>(constraintName);

            var actualConstraint = Constraints.TryGetFor<object>(constraintName);

            Assert.IsNull(actualConstraint);
        }
        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestDeregisteringAConstraintAllowsADifferentOneToBeRegistered()
        {
            var deregisteredConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());

            Constraints.RegisterFor(deregisteredConstraint);
            Constraints.DeregisterFor<object>();
            Constraints.RegisterFor(expectedConstraint);

            var actualConstraint = Constraints.TryGetFor<object>();

            Assert.AreNotSame(deregisteredConstraint, expectedConstraint);
            Assert.AreSame(expectedConstraint, actualConstraint);
        }
        [TestMethod]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestDeregisteringAConstraintWithANameAllowsADifferentOneWithTheSameNameToBeRegistered()
        {
            var constraintName = "test";
            _constraintNamesToDeregister.Add(constraintName);

            var deregisteredConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());
            var expectedConstraint = Constraint.From<object>(value => Enumerable.Empty<ValidationError>());

            Constraints.RegisterFor(deregisteredConstraint, constraintName);
            Constraints.DeregisterFor<object>(constraintName);
            Constraints.RegisterFor(expectedConstraint, constraintName);

            var actualConstraint = Constraints.TryGetFor<object>(constraintName);

            Assert.AreNotSame(deregisteredConstraint, expectedConstraint);
            Assert.AreSame(expectedConstraint, actualConstraint);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestRegisteringANullConstraintThrowsException()
        {
            Constraints.RegisterFor<object>(null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [Obsolete("Having a static class to act as a container for constraints raises more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint provided to consumers (dependency injection is one way to do it).")]
        public void TestRegisteringANullConstraintWithNameThrowsException()
        {
            Constraints.RegisterFor<object>(null, "test");
        }
    }
}