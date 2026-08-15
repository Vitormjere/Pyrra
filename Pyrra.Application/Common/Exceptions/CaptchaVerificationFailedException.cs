using System;

namespace Pyrra.Application.Common.Exceptions {
    public class CaptchaVerificationFailedException : Exception {
        public CaptchaVerificationFailedException()
            : base("Não foi possível confirmar que você não é um robô. Tente novamente.") { }
    }
}
