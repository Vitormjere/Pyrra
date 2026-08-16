using System;

namespace Pyrra.Application.Common.Exceptions {
    public class GoogleAuthFailedException : Exception {
        public GoogleAuthFailedException()
            : base("Não foi possível confirmar sua conta do Google. Tente novamente.") { }
    }
}
