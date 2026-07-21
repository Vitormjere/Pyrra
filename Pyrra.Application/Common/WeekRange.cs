using System;

namespace Pyrra.Application.Common {
    // Definição única de "semana" para todo o sistema: segunda a domingo. Estava só no
    // PriorityTaskService; virou compartilhado quando Finanças passou a precisar da mesma regra —
    // duas cópias divergiriam no dia em que alguém decidisse começar a semana no domingo.
    public static class WeekRange {
        // Normaliza qualquer data para a segunda-feira da SUA semana, em vez de recusar o que não
        // for segunda. Assim ?inicio=2026-07-22 (quarta) devolve a semana que contém essa quarta —
        // que é o que quem digitou a data quis dizer — e o intervalo nunca fica torto, atravessando
        // duas semanas. Quem chama devolve o weekStart efetivo, para o cliente ver a normalização.
        public static DateOnly StartOfWeek(DateOnly date) =>
            date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        // Domingo da mesma semana. Intervalo inclusivo nas duas pontas.
        public static DateOnly EndOfWeek(DateOnly weekStart) => weekStart.AddDays(6);
    }
}
