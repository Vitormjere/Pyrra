using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidPasswordResetTokenException : Exception {
        public InvalidPasswordResetTokenException()
            : base("Link inválido ou expirado. Solicite um novo.") { }
    }
}
