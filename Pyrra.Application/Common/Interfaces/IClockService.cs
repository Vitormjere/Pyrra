using System;

namespace Pyrra.Application.Common.Interfaces {
    public interface IClockService {
        DateTime UtcNow { get; }

        DateOnly TodayIn(string timezoneId);

        DateOnly ToLocalDate(DateTime utc, string timezoneId);
    }
}