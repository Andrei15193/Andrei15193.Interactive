using System;
using System.Collections.ObjectModel;
using Andrei15193.Interactive.Validation;

namespace Andrei15193.Interactive
{
    public sealed class ViewModelContext<TDataModel>
    {
        internal ViewModelContext(TDataModel dataModel, ReadOnlyObservableCollection<ValidationError> errors)
        {
            if (dataModel == null)
                throw new ArgumentNullException(nameof(dataModel));
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            DataModel = dataModel;
            Errors = errors;
        }

        public TDataModel DataModel { get; }

        public ReadOnlyObservableCollection<ValidationError> Errors { get; }
    }
}