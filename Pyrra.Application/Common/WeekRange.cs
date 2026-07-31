using System;

namespace Pyrra.Application.Common {
    // Define a regra compartilhada de semana do sistema (segunda a domingo)
    public static class WeekRange {
        // Retorna a segunda-feira da semana que contém a data informada
        public static DateOnly StartOfWeek(DateOnly date) =>
            date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        // Retorna o domingo da semana iniciada na data informada
        public static DateOnly EndOfWeek(DateOnly weekStart) => weekStart.AddDays(6);
    }
}