using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Andrei15193.Interactive.Validation;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents an <see cref="InteractiveViewModel"/> that operates on a collection
    /// of items.
    /// </summary>
    /// <typeparam name="TItem">
    /// The type of the items within the collection.
    /// </typeparam>
    public class CollectionInteractiveViewModel<TItem>
        : InteractiveViewModel
    {
        /// <summary>
        /// Creates a new <see cref="CollectionInteractiveViewModel{TItem}"/> instance.
        /// </summary>
        /// <param name="items">
        /// The items with which to initialize the <see cref="Context"/> with.
        /// </param>
        public CollectionInteractiveViewModel(IEnumerable<TItem> items)
        {
            Items = new ObservableCollection<TItem>(items);
            Errors = new ObservableCollection<ValidationError>();
            Context = new ViewModelContext<ReadOnlyObservableCollection<TItem>>(
                new ReadOnlyObservableCollection<TItem>(Items),
                new ReadOnlyObservableCollection<ValidationError>(Errors));
        }
        /// <summary>
        /// Creates a new <see cref="CollectionInteractiveViewModel{TItem}"/> instance.
        /// </summary>
        /// <param name="items">
        /// The items with which to initialize the <see cref="Context"/> with.
        /// </param>
        public CollectionInteractiveViewModel(params TItem[] items)
            : this(items.AsEnumerable())
        {
        }
        /// <summary>
        /// Creates a new <see cref="CollectionInteractiveViewModel{TItem}"/> instance.
        /// </summary>
        public CollectionInteractiveViewModel()
            : this(Enumerable.Empty<TItem>())
        {
        }

        /// <summary>
        /// The context on which the current <see cref="CollectionInteractiveViewModel{TItem}"/> relies on.
        /// </summary>
        public ViewModelContext<ReadOnlyObservableCollection<TItem>> Context { get; }

        /// <summary>
        /// A mutable collection of <typeparamref name="TItem"/>s.
        /// </summary>
        protected ObservableCollection<TItem> Items { get; }

        /// <summary>
        /// A mutable collection of <see cref="ValidationError"/>s concerning the <see cref="Items"/>.
        /// </summary>
        protected ObservableCollection<ValidationError> Errors { get; }
    }
}