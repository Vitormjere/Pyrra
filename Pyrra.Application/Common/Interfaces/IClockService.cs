using System;

namespace Pyrra.Application.Common.Interfaces {
    public interface IClockService {
        DateTime UtcNow { get; }

        // "Hoje" do ponto de vista do usuário: 21h em São Paulo ainda é o mesmo dia,
        // embora já seja o dia seguinte em UTC.
        DateOnly TodayIn(string timezoneId);
    }
}
