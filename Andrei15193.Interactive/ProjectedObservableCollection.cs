using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// An observable collection that contains projected elements from a source collection.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type to which items are mapped.
    /// </typeparam>
    public class ProjectedObservableCollection<TResult>
        : ReadOnlyCollection<TResult>, IReadOnlyObservableCollection<TResult>
    {
        private readonly Func<object, TResult> _selector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectedObservableCollection{TResult}"/> class.
        /// </summary>
        /// <param name="selector">
        /// A mapping function to use when projecting elements from the provided <paramref name="collection"/>.
        /// </param>
        /// <param name="collection">
        /// A collection with which to initialize the new instance.
        /// </param>
        /// <remarks>
        /// If the provided <paramref name="collection"/> implements <see cref="INotifyCollectionChanged"/>
        /// then the projected collection will remain synchronized with all changes that are notified.
        /// </remarks>
        public ProjectedObservableCollection(Func<object, TResult> selector, IEnumerable collection)
            : base((collection?.Cast<object>().Select(selector) ?? Enumerable.Empty<TResult>()).ToList())
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _selector = selector;

            var collectionChangedNotifier = collection as INotifyCollectionChanged;
            if (collectionChangedNotifier != null)
                collectionChangedNotifier.CollectionChanged += _ProjectedCollectionChanged;
        }

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void _ProjectedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems?.Cast<object>()?.Select(_selector)?.ToList() ?? null;
            List<TResult> oldItems = null;
            if (e.OldItems != null)
                oldItems = Items.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();
            NotifyCollectionChangedEventArgs eventArgs = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (var newItemIndex = 0; newItemIndex < newItems.Count; newItemIndex++)
                        Items.Insert(e.NewStartingIndex + newItemIndex, newItems[newItemIndex]);

                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, e.NewStartingIndex);
                    NotifyPropertyChanged(nameof(Count));
                    NotifyPropertyChanged("Item[]");
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < e.NewStartingIndex)
                        for (var index = e.OldStartingIndex; index < e.NewStartingIndex; index++)
                            Items[index] = Items[index + oldItems.Count];
                    else
                        for (var index = e.OldStartingIndex; index > e.NewStartingIndex; index--)
                            Items[index] = Items[index - oldItems.Count];

                    for (var itemOffset = 0; itemOffset < oldItems.Count; itemOffset++)
                        Items[e.NewStartingIndex + itemOffset] = oldItems[itemOffset];

                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, oldItems, e.NewStartingIndex, e.OldStartingIndex);
                    NotifyPropertyChanged("Item[]");
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in oldItems)
                        Items.Remove(oldItem);

                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, e.OldStartingIndex);
                    NotifyPropertyChanged(nameof(Count));
                    NotifyPropertyChanged("Item[]");
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (int oldIndex = e.OldStartingIndex, newIndex = e.NewStartingIndex, index = 0; index < newItems.Count; oldIndex++, newIndex++, index++)
                    {
                        Items.RemoveAt(e.OldStartingIndex);
                        Items.Insert(e.NewStartingIndex, newItems[index]);
                    }

                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, e.NewStartingIndex);
                    NotifyPropertyChanged("Item[]");
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    foreach (var item in ((IEnumerable)sender)?.Cast<object>().Select(_selector))
                        Items.Add(item);

                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    NotifyPropertyChanged(nameof(Count));
                    NotifyPropertyChanged("Item[]");
                    break;

                default:
                    Debug.WriteLine($"Unknown NotifyCollectionChangedAction value {e.Action}.");
                    break;
            }

            if (eventArgs != null)
                CollectionChanged?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for given property name.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that has changed.
        /// </param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// An observable collection that contains projected elements from a source collection.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of the mapped object.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type to which items are mapped.
    /// </typeparam>
    public class ProjectedObservableCollection<TSource, TResult>
        : ProjectedObservableCollection<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectedObservableCollection{TSource, TResult}"/> class.
        /// </summary>
        /// <param name="selector">
        /// A mapping function to use when projecting elements from the provided <paramref name="collection"/>.
        /// </param>
        /// <param name="collection">
        /// A collection with which to initialize the new instance.
        /// </param>
        /// <remarks>
        /// If the provided <paramref name="collection"/> implements <see cref="INotifyCollectionChanged"/>
        /// then the projected collection will remain synchronized with all changes that are notified.
        /// </remarks>
        public ProjectedObservableCollection(Func<TSource, TResult> selector, IEnumerable<TSource> collection)
            : base(value => selector((TSource)value), collection)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
        }
    }
}