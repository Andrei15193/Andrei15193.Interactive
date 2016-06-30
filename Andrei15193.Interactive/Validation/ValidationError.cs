using System;

namespace Andrei15193.Interactive.Validation
{
    /// <summary>
    /// Represents a validation error having a message and an optional member name.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Creates a new <see cref="ValidationError"/> instance.
        /// </summary>
        /// <param name="message">
        /// The message describing the error.
        /// </param>
        /// <param name="memberName">
        /// The name of the member to which the error message relates to.
        /// </param>
        /// <remarks>
        /// This class is inspired by the ValidationResult one, however ValidationResult
        /// is not portable and thus cannot be used in this library.
        /// </remarks>
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
        /// <summary>
        /// Creates a new <see cref="ValidationError"/> instance.
        /// </summary>
        /// <param name="message">
        /// The message describing the error.
        /// </param>
        public ValidationError(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// The message describing the error.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The name of the member to which the error message relates to.
        /// </summary>
        public string MemberName { get; }
    }
}