using System;

namespace Pyrra.Application.Common.Exceptions {
    // Estourou o teto diário de perguntas ao Zelo. O controller a traduz em 429 com esta mensagem,
    // que é escrita para o usuário final — nada de detalhe técnico.
    public class ZeloRateLimitExceededException : Exception {
        public ZeloRateLimitExceededException()
            : base("Você atingiu o limite de perguntas ao Zelo por hoje, volte amanhã!") { }
    }
}
