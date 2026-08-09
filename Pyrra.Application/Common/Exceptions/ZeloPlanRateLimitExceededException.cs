using System;

namespace Pyrra.Application.Common.Exceptions {
    // quando o limite diário do Zelo conversacional (separado do Zelo geral) foi atingido
    public class ZeloPlanRateLimitExceededException : Exception {
        public ZeloPlanRateLimitExceededException()
            : base("Você atingiu o limite de interações com o Zelo conversacional por hoje, volte amanhã!") { }
    }
}
