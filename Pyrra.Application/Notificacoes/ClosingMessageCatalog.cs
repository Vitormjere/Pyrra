using System;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Notificacoes {
    // Catálogo puro de mensagens: (tom x situação) -> texto. Sem I/O, o que o torna trivial de
    // testar e o único lugar a mexer para ajustar copy. {0} é o percentual inteiro do dia.
    //
    // 5 situações x 3 tons = 15 mensagens, cobrindo com folga o mínimo pedido (9). Direto é
    // objetivo; Acolhedor é gentil; Desafiador provoca no bom sentido.
    public static class ClosingMessageCatalog {
        public static string Compose(CommunicationTone tone, ClosingSituation situation, int percent) =>
            string.Format(Template(tone, situation), percent);

        private static string Template(CommunicationTone tone, ClosingSituation situation) => (tone, situation) switch {
            // --- Meta batida (>= 70%) ---
            (CommunicationTone.Direto,      ClosingSituation.MetaBatida) => "Meta batida hoje, {0}% do dia. Feche e descanse. Amanhã de novo.",
            (CommunicationTone.Acolhedor,   ClosingSituation.MetaBatida) => "Você bateu sua meta hoje, {0}%. Foi bonito te ver cuidando de você. Agora descanse, você merece.",
            (CommunicationTone.Desafiador,  ClosingSituation.MetaBatida) => "Meta batida, {0}%. Tá confortável demais? Amanhã eu subo a régua e quero ver.",

            // --- Perto (50% a 70%) ---
            (CommunicationTone.Direto,      ClosingSituation.Perto) => "{0}% hoje. Faltou pouco pra meta. Ainda dá pra fechar antes de dormir.",
            (CommunicationTone.Acolhedor,   ClosingSituation.Perto) => "Chegou bem perto hoje, {0}%. Já é bastante, viu? Se ainda tiver ânimo, um último passo fecha o dia.",
            (CommunicationTone.Desafiador,  ClosingSituation.Perto) => "{0}% e parou no quase? A meta tá logo ali. Vai encarar agora ou vai dormir devendo?",

            // --- Longe (1% a 49%) ---
            (CommunicationTone.Direto,      ClosingSituation.Longe) => "{0}% hoje. Longe da meta, mas o dia não acabou. Um foco agora já muda o número.",
            (CommunicationTone.Acolhedor,   ClosingSituation.Longe) => "O dia foi difícil, e tudo bem. Você fez {0}%, e isso conta. Amanhã é uma chance nova.",
            (CommunicationTone.Desafiador,  ClosingSituation.Longe) => "Só {0}%? O dia ainda tá de pé. Prova pra você mesmo que consegue mais que isso.",

            // --- Nada feito (0%, mas com focos) ---
            (CommunicationTone.Direto,      ClosingSituation.Nada) => "Nenhum foco concluído hoje. Ainda dá pra fazer pelo menos um antes de dormir.",
            (CommunicationTone.Acolhedor,   ClosingSituation.Nada) => "Ainda não deu pra começar hoje, e não tem problema. Que tal um foco só, bem pequeno, agora?",
            (CommunicationTone.Desafiador,  ClosingSituation.Nada) => "Zero até agora. Vai deixar o dia inteiro passar em branco ou vai reagir?",

            // --- Sem focos ativos ---
            (CommunicationTone.Direto,      ClosingSituation.SemFocos) => "Você ainda não tem focos ativos. Cadastre um pra começar a pontuar.",
            (CommunicationTone.Acolhedor,   ClosingSituation.SemFocos) => "Você ainda não definiu seus focos. Quando quiser, comece com um só, no seu ritmo.",
            (CommunicationTone.Desafiador,  ClosingSituation.SemFocos) => "Sem nenhum foco cadastrado? Difícil vencer um jogo que você não começou. Bora criar o primeiro.",

            _ => throw new ArgumentOutOfRangeException(nameof(situation), $"Combinação de tom '{tone}' e situação '{situation}' sem mensagem definida.")
        };
    }
}
