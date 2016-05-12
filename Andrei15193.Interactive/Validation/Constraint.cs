using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andrei15193.Interactive.Validation
{
    /// <summary>
    /// Represents a default implementation of the <see cref="IConstraint{TValue}"/> interface.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of the object that will be checked.
    /// </typeparam>
    public abstract class Constraint<TValue>
        : IConstraint<TValue>
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
        public Task<IEnumerable<ValidationError>> CheckAsync(TValue value)
            => CheckAsync(value, CancellationToken.None);

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
        public async Task<IEnumerable<ValidationError>> CheckAsync(TValue value, CancellationToken cancellationToken)
        {
            var task = OnCheckAsync(value, cancellationToken);
            if (task == null)
                return Enumerable.Empty<ValidationError>();

            var validationErrors = await task;
            return validationErrors ?? Enumerable.Empty<ValidationError>();
        }

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
        protected abstract Task<IEnumerable<ValidationError>> OnCheckAsync(TValue value, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Exposes a number of static methods that help create constraints out of delegates.
    /// </summary>
    public static class Constraint
    {
        /// <summary>
        /// Represents a callback for inline (non-asynchronous) constraints.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="value">
        /// The object that will be checked.
        /// </param>
        /// <returns>
        /// Returns a collection of <see cref="ValidationError"/>s that contain details about the errors.
        /// </returns>
        public delegate IEnumerable<ValidationError> Callback<TValue>(TValue value);

        /// <summary>
        /// Represents a callback for asynchronous constraints.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="value">
        /// The object that will be checked.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// Returns a collection of <see cref="ValidationError"/>s that contain details about the errors.
        /// </returns>
        public delegate Task<IEnumerable<ValidationError>> AsyncCallback<TValue>(TValue value, CancellationToken cancellationToken);

        /// <summary>
        /// Creates an inline (non-asynchronous) constraint.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="callback">
        /// The callback that will perform that actual check.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IConstraint{TValue}"/> implementation that will
        /// delegate to the callback in order to determine whether an object satisfies
        /// a constraint.
        /// </returns>
        public static IConstraint<TValue> From<TValue>(Callback<TValue> callback)
            => new InlineDelegateConstraint<TValue>(callback);

        /// <summary>
        /// Creates an asynchronous constraint.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the object that will be checked.
        /// </typeparam>
        /// <param name="asyncCallback">
        /// The callback that will perform that actual check.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IConstraint{TValue}"/> implementation that will
        /// delegate to the callback in order to determine whether an object satisfies
        /// a constraint.
        /// </returns>
        public static IConstraint<TValue> From<TValue>(AsyncCallback<TValue> asyncCallback)
            => new AsyncDelegateConstraint<TValue>(asyncCallback);

        private sealed class InlineDelegateConstraint<TValue>
            : Constraint<TValue>
        {
            private readonly Callback<TValue> _callback;

            public InlineDelegateConstraint(Callback<TValue> callback)
            {
                if (callback == null)
                    throw new ArgumentNullException(nameof(callback));

                _callback = callback;
            }

            protected override Task<IEnumerable<ValidationError>> OnCheckAsync(TValue value, CancellationToken cancellationToken)
                => Task.FromResult(_callback(value));
        }

        private sealed class AsyncDelegateConstraint<TValue>
            : Constraint<TValue>
        {
            private readonly AsyncCallback<TValue> _callback;

            public AsyncDelegateConstraint(AsyncCallback<TValue> callback)
            {
                if (callback == null)
                    throw new ArgumentNullException(nameof(callback));

                _callback = callback;
            }

            protected override Task<IEnumerable<ValidationError>> OnCheckAsync(TValue value, CancellationToken cancellationToken)
                => _callback(value, cancellationToken) ?? Task.FromResult(Enumerable.Empty<ValidationError>());
        }
    }
}