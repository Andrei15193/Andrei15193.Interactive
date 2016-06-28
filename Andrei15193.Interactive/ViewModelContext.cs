using System;
using System.Collections.ObjectModel;
using Andrei15193.Interactive.Validation;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// Represents the context of an <see cref="InteractiveViewModel{TDataModel}"/>.
    /// </summary>
    /// <typeparam name="TDataModel">
    /// The type of the data model that the <see cref="InteractiveViewModel{TDataModel}"/> exposes.
    /// </typeparam>
    public sealed class ViewModelContext<TDataModel>
    {
        /// <summary>
        /// Creates a new <see cref="ViewModelContext{TDataModel}"/> instance.
        /// </summary>
        /// <param name="dataModel">
        /// An instance representing the data model, cannot be null.
        /// </param>
        /// <param name="errors">
        /// An observable collection of <see cref="ValidationError"/>s that is managed externally.
        /// </param>
        internal ViewModelContext(TDataModel dataModel, ReadOnlyObservableCollection<ValidationError> errors)
        {
            if (dataModel == null)
                throw new ArgumentNullException(nameof(dataModel));
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            DataModel = dataModel;
            Errors = errors;
        }

        /// <summary>
        /// The instance representing the data model.
        /// </summary>
        public TDataModel DataModel { get; }

        /// <summary>
        /// An observable collection of <see cref="ValidationError"/>s concerning the <see cref="DataModel"/>.
        /// </summary>
        public ReadOnlyObservableCollection<ValidationError> Errors { get; }
    }
}