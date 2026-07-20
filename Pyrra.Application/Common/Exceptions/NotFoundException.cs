using System;

namespace Pyrra.Application.Common.Exceptions {
    public class NotFoundException : Exception {
        public NotFoundException(string message) : base(message) { }
    }
}
