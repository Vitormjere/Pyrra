using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidWorkoutException : Exception {
        public InvalidWorkoutException(string message) : base(message) { }
    }
}
