using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidTaskException : Exception {
        public InvalidTaskException(string message) : base(message) { }
    }
}
