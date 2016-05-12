using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Andrei15193.Interactive.Validation
{
    /// <summary>
    /// Represents the interface of a constraint.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of the object that will be checked.
    /// </typeparam>
    public interface IConstraint<in TValue>
    {
        /// <summary>
        /// Checks whether the provided object satisfies the constraint.
        /// </summary>
        /// <param name="value">
        /// The object to check.
        /// </param>
        /// <returns>
        /// Returns a collection of <see cref="ValidationError"/>s that contain details about the error.
        /// </returns>
        Task<IEnumerable<ValidationError>> CheckAsync(TValue value);

        /// <summary>
        /// Checks whether the provided object satisfies the constraint.
        /// </summary>
        /// <param name="value">
        /// The object to check.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// Returns a collection of <see cref="ValidationError"/>s that contain details about the errors.
        /// </returns>
        Task<IEnumerable<ValidationError>> CheckAsync(TValue value, CancellationToken cancellationToken);
    }
}