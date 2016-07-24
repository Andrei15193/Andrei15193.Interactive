using System;
using System.Collections.ObjectModel;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Contains a number of extension methods for <see cref="ObservableCollection{T}"/>,
    /// <see cref="ReadOnlyObservableCollection{T}"/> and <see cref="IReadOnlyObservableCollection{T}"/>.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Projects each element of the observable collection into a new one. Any change
        /// in the original collection will be reflected in the result.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of elements in the source collection.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of elements in the projected collection.
        /// </typeparam>
        /// <param name="source">
        /// The original observable collection to project.
        /// </param>
        /// <param name="selector">
        /// A projection function to apply on each element.
        /// </param>
        /// <returns>
        /// A <see cref="IReadOnlyObservableCollection{T}"/> containing the projected elements
        /// from <paramref name="source"/>.
        /// </returns>
        public static IReadOnlyObservableCollection<TResult> Select<TSource, TResult>(this ObservableCollection<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ProjectedObservableCollection<TSource, TResult>(selector, source);
        }

        /// <summary>
        /// Projects each element of the observable collection into a new one. Any change
        /// in the original collection will be reflected in the result.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of elements in the source collection.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of elements in the projected collection.
        /// </typeparam>
        /// <param name="source">
        /// The original observable collection to project.
        /// </param>
        /// <param name="selector">
        /// A projection function to apply on each element.
        /// </param>
        /// <returns>
        /// A <see cref="IReadOnlyObservableCollection{T}"/> containing the projected elements
        /// from <paramref name="source"/>.
        /// </returns>
        public static IReadOnlyObservableCollection<TResult> Select<TSource, TResult>(this ReadOnlyObservableCollection<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ProjectedObservableCollection<TSource, TResult>(selector, source);
        }

        /// <summary>
        /// Projects each element of the observable collection into a new one. Any change
        /// in the original collection will be reflected in the result.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of elements in the source collection.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of elements in the projected collection.
        /// </typeparam>
        /// <param name="source">
        /// The original observable collection to project.
        /// </param>
        /// <param name="selector">
        /// A projection function to apply on each element.
        /// </param>
        /// <returns>
        /// A <see cref="IReadOnlyObservableCollection{T}"/> containing the projected elements
        /// from <paramref name="source"/>.
        /// </returns>
        public static IReadOnlyObservableCollection<TResult> Select<TSource, TResult>(this IReadOnlyObservableCollection<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new ProjectedObservableCollection<TSource, TResult>(selector, source);
        }
    }
}