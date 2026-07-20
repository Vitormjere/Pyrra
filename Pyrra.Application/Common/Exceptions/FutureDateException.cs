using System;

namespace Pyrra.Application.Common.Exceptions {
    public class FutureDateException : Exception {
        public FutureDateException(DateOnly date) : base($"A data '{date:yyyy-MM-dd}' está no futuro.") { }
    }
}
