using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidPreferencesException : Exception {
        public InvalidPreferencesException(string message) : base(message) { }
    }
}
