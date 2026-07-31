using System;

namespace Pyrra.Application.Common.Exceptions {
    // Indica que o limite diário de perguntas foi atingido
    public class ZeloRateLimitExceededException : Exception {
        public ZeloRateLimitExceededException()
            : base("Você atingiu o limite de perguntas ao Zelo por hoje, volte amanhã!") { }
    }
}
