using System;

namespace Pyrra.Application.Common.Exceptions {
    public class PastCheckInDateException : Exception {
        public PastCheckInDateException(DateOnly date)
            : base($"Não é possível fazer check-in em '{date:yyyy-MM-dd}': só o dia de hoje aceita check-in.") { }
    }
}
