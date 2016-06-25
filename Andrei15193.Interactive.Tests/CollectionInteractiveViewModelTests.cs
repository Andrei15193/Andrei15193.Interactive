using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Andrei15193.Interactive.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests.Windows
{
    [TestClass]
    public class CollectionInteractiveViewModelTests
    {
        private sealed class MockCollectionInteractiveViewModel<TItem>
            : CollectionInteractiveViewModel<TItem>
        {
            public MockCollectionInteractiveViewModel(IEnumerable<TItem> items)
                : base(items)
            {
            }
            public MockCollectionInteractiveViewModel(params TItem[] items)
                : base(items)
            {
            }
            public MockCollectionInteractiveViewModel()
                : base()
            {
            }

            new public ObservableCollection<TItem> Items
                => base.Items;

            new public ObservableCollection<ValidationError> Errors
                => base.Errors;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCollectionInteractiveViewModelCannotBeCreatedWithNullCollection()
        {
            new CollectionInteractiveViewModel<object>((IEnumerable<object>)null);
        }

        [TestMethod]
        public void TestProvidingItemsToCollectionInteractiveViewModelConstructorWillInitializeTheDataContextWithThem()
        {
            var items =
                new[]
                {
                    new object(),
                    new object()
                };
            var collectionInteractiveViewModel = new CollectionInteractiveViewModel<object>(items);

            Assert.IsTrue(items.SequenceEqual(collectionInteractiveViewModel.Context.DataModel));
        }

        [TestMethod]
        public void TestCollectionInteractiveViewModelCanBeCreatedWithNullItems()
        {
            var collectionInteractiveViewModel = new CollectionInteractiveViewModel<object>(new object[] { null, null });

            Assert.IsTrue(collectionInteractiveViewModel.Context.DataModel.All(value => value == null));
        }

        [TestMethod]
        public void TestChaningTheItemsCollectionChangesTheContextDataModel()
        {
            var items =
                new[]
                {
                    new object(),
                    new object()
                };
            var collectionInteractiveViewModel = new MockCollectionInteractiveViewModel<object>(items);

            collectionInteractiveViewModel.Items.Clear();

            Assert.AreEqual(0, collectionInteractiveViewModel.Context.DataModel.Count);
        }

        [TestMethod]
        public void TestTheCollectionOfErrorsIsInitiallyEmpty()
        {
            var collectionInteractiveViewModel = new CollectionInteractiveViewModel<object>();

            Assert.IsFalse(collectionInteractiveViewModel.Context.Errors.Any());
        }

        [TestMethod]
        public void TestChaningTheErrorsCollectionChangesTheContextErrors()
        {
            var collectionInteractiveViewModel = new MockCollectionInteractiveViewModel<object>();
            var error = new ValidationError("test");

            collectionInteractiveViewModel.Errors.Add(error);

            Assert.AreSame(error, collectionInteractiveViewModel.Context.Errors.Single());
        }
    }
}