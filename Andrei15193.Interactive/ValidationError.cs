using System;

namespace Andrei15193.Interactive
{
    public class ValidationError
    {
        public ValidationError(string message, string memberName)
        {
            if (string.IsNullOrWhiteSpace(message))
                if (message == null)
                    throw new ArgumentNullException(nameof(message));
                else
                    throw new ArgumentException("Cannot be empty or white space!", nameof(message));
            if (memberName != null && string.IsNullOrWhiteSpace(memberName))
                throw new ArgumentException("Cannot be empty or white space!", nameof(memberName));

            Message = message;
            MemberName = memberName;
        }
        public ValidationError(string message)
            : this(message, null)
        {
        }

        public string Message { get; }

        public string MemberName { get; }
    }
}