using System;
using Pyrra.Application.Common.Interfaces;

namespace Pyrra.Infrastructure.Common {
    public class SystemClockService : IClockService {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateOnly TodayIn(string timezoneId) => ToLocalDate(DateTime.UtcNow, timezoneId);

        public DateOnly ToLocalDate(DateTime utc, string timezoneId) {
            var timezone = ResolveTimezone(timezoneId);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, timezone));
        }

        // Timezone inválido cai para UTC em vez de derrubar a requisição: o campo é texto livre
        // no User e um valor ruim não deve impedir o usuário de fazer check-in.
        private static TimeZoneInfo ResolveTimezone(string timezoneId) {
            if (string.IsNullOrWhiteSpace(timezoneId)) {
                return TimeZoneInfo.Utc;
            }

            try {
                return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            } catch (TimeZoneNotFoundException) {
                return TimeZoneInfo.Utc;
            } catch (InvalidTimeZoneException) {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
