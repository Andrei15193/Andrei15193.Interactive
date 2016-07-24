using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents a read-only observable list.
    /// </summary>
    /// <typeparam name="T">
    /// The type of items the collection contains.
    /// </typeparam>
    public interface IReadOnlyObservableCollection<out T>
        : IReadOnlyList<T>, IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}