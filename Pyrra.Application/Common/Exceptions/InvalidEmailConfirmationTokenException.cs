using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidEmailConfirmationTokenException : Exception {
        public InvalidEmailConfirmationTokenException()
            : base("Link de confirmação inválido ou expirado.") { }
    }
}
