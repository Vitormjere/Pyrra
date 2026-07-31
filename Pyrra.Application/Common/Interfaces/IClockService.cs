using System;

namespace Pyrra.Application.Common.Interfaces {
    public interface IClockService {
        DateTime UtcNow { get; }

        // Retorna a data atual no fuso horário informado
        DateOnly TodayIn(string timezoneId);

        // Converte uma data UTC para o fuso horário informado
        DateOnly ToLocalDate(DateTime utc, string timezoneId);
    }
}