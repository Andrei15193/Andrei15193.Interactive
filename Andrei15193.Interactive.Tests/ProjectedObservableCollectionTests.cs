using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class ProjectedObservableCollectionTests
    {
        private ObservableCollection<int> ObservableCollection { get; set; }

        private IReadOnlyObservableCollection<string> ProjectedObservabvleCollection { get; set; }

        private Func<int, string> Selector { get; set; }

        private int ValueToAdd { get; set; }
        private int IndexToChange { get; set; }
        private int OldIndex { get; set; }
        private int ValueToRemove { get; set; }

        private NotifyCollectionChangedEventArgs OriginalEventArgs { get; set; }

        private NotifyCollectionChangedEventArgs ProjectedEventArgs { get; set; }
        private ICollection<string> OriginalPropertyChanges { get; set; }
        private ICollection<string> ProjectionPropertyChanges { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            OriginalPropertyChanges = new List<string>();
            ProjectionPropertyChanges = new List<string>();

            var random = new Random();
            var initialLength = random.Next(5, 10);
            ObservableCollection = new ObservableCollection<int>(Enumerable.Range(0, initialLength));
            ObservableCollection.CollectionChanged +=
                (sender, e) =>
                {
                    Assert.IsNull(OriginalEventArgs);
                    OriginalEventArgs = e;
                };
            ((INotifyPropertyChanged)ObservableCollection).PropertyChanged += (sender, e) => OriginalPropertyChanges.Add(e.PropertyName);

            ValueToAdd = random.Next(10, int.MaxValue);
            IndexToChange = random.Next(0, initialLength / 2);
            OldIndex = random.Next(initialLength / 2, initialLength);
            ValueToRemove = ObservableCollection[IndexToChange];

            Selector = (value => value.ToString());
            ProjectedObservabvleCollection = ObservableCollection.Select(Selector);
            ProjectedObservabvleCollection.CollectionChanged +=
                (sender, e) =>
                {
                    Assert.IsNull(ProjectedEventArgs);
                    ProjectedEventArgs = e;
                };
            ProjectedObservabvleCollection.PropertyChanged += (sender, e) => ProjectionPropertyChanges.Add(e.PropertyName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ProjectedObservabvleCollection = null;
            ProjectedEventArgs = null;
            Selector = null;
            ObservableCollection = null;
            OriginalEventArgs = null;
            OriginalPropertyChanges = null;
            ProjectedObservabvleCollection = null;
        }

        [TestMethod]
        public void TestCreatingEmptyProjectedObservableCollection()
        {
            var collection = new object[] { }.Select(obj => obj?.ToString());

            Assert.AreEqual(0, collection.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestTryingToCreateAProjectedObservableCollectionWithNullSelectorThrowsException()
        {
            new ObservableCollection<object>().Select<object, string>(null);
        }

        [TestMethod]
        public void TestCreatingAProjectedObservableCollectionFromExistingOneItGetsPopulatedWithTransformedItems()
        {
            Func<int, string> selector = (obj => obj.ToString());
            var projectedCollection = new ObservableCollection<int>(Enumerable.Range(0, 10));
            var collection = projectedCollection.Select(selector);

            for (var index = 0; index < projectedCollection.Count; index++)
                Assert.AreEqual(selector(projectedCollection[index]), collection[index]);
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeAddChangesVisible()
        {
            ObservableCollection.Add(ValueToAdd);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeInsertChangesVisible()
        {
            ObservableCollection.Insert(IndexToChange, ValueToAdd);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeRemoveChangesVisible()
        {
            ObservableCollection.Remove(ValueToRemove);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeRemoveAtChangesVisible()
        {
            ObservableCollection.RemoveAt(OldIndex);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeReplaceChangesVisible()
        {
            ObservableCollection[IndexToChange] = ValueToAdd;

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeMoveToFrontChangesVisible()
        {
            ObservableCollection.Move(OldIndex, IndexToChange);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeMoveToBackChangesVisible()
        {
            ObservableCollection.Move(IndexToChange, OldIndex);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeMoveToSameLocationChangesVisible()
        {
            ObservableCollection.Move(OldIndex, OldIndex);

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        [TestMethod]
        public void TestWrappingAnObservableCollectionWillMakeResetChangesVisible()
        {
            ObservableCollection.Clear();

            AssertCollectionsAreSame();
            AssertEventArgsAreEqual();
            AssertPropertyChangesAreTheSame();
        }

        private void AssertCollectionsAreSame()
        {
            Assert.IsTrue(
                ObservableCollection.Select(Selector).SequenceEqual(ProjectedObservabvleCollection),
                $"Expected ({string.Join(", ", ObservableCollection)}) but received ({string.Join(", ", ProjectedObservabvleCollection)})");
        }

        private void AssertPropertyChangesAreTheSame()
        {
            Assert.IsTrue(
                OriginalPropertyChanges.OrderBy(propertyName => propertyName).SequenceEqual(ProjectionPropertyChanges.OrderBy(propertyName => propertyName)),
                $"Expected ({string.Join(", ", OriginalPropertyChanges)}) but received ({string.Join(", ", ProjectionPropertyChanges)})");
        }

        public void AssertEventArgsAreEqual()
        {
            Assert.IsNotNull(OriginalEventArgs);
            Assert.IsNotNull(ProjectedEventArgs);

            Assert.AreEqual(OriginalEventArgs.Action, ProjectedEventArgs.Action);
            Assert.AreEqual(OriginalEventArgs.NewStartingIndex, ProjectedEventArgs.NewStartingIndex);
            Assert.AreEqual(OriginalEventArgs.OldStartingIndex, ProjectedEventArgs.OldStartingIndex);

            if (OriginalEventArgs.NewItems == null)
                Assert.IsNull(ProjectedEventArgs.NewItems);
            else
                Assert.IsTrue(OriginalEventArgs.NewItems.Cast<int>().Select(Selector).SequenceEqual(ProjectedEventArgs.NewItems.Cast<string>()));

            if (OriginalEventArgs.OldItems == null)
                Assert.IsNull(ProjectedEventArgs.OldItems);
            else
                Assert.IsTrue(OriginalEventArgs.OldItems.Cast<int>().Select(Selector).SequenceEqual(ProjectedEventArgs.OldItems.Cast<string>()));
        }
    }
}