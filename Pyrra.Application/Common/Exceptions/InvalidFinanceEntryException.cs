using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidFinanceEntryException : Exception {
        public InvalidFinanceEntryException(string message) : base(message) { }
    }
}
