using System;

namespace Pyrra.Application.Common {
    // regra compartilhada de semana do sistema, de segunda a domingo
    public static class WeekRange {
        // segunda-feira da semana que contém a data informada
        public static DateOnly StartOfWeek(DateOnly date) =>
            date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        // domingo da semana que começa na data informada
        public static DateOnly EndOfWeek(DateOnly weekStart) => weekStart.AddDays(6);
    }
}