using System;

namespace Pyrra.Application.Common.Exceptions {
    public class AccountLockedException : Exception {
        public int RetryAfterSeconds { get; }

        public AccountLockedException(DateTime lockedUntilUtc, DateTime nowUtc)
            : base(BuildMessage(lockedUntilUtc, nowUtc, out var retryAfterSeconds)) {
            RetryAfterSeconds = retryAfterSeconds;
        }

        private static string BuildMessage(DateTime lockedUntilUtc, DateTime nowUtc, out int retryAfterSeconds) {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((lockedUntilUtc - nowUtc).TotalSeconds));
            var minutesRemaining = Math.Max(1, (int)Math.Ceiling(retryAfterSeconds / 60.0));
            return $"Muitas tentativas falhas. Tente novamente em {minutesRemaining} minuto{(minutesRemaining == 1 ? "" : "s")}.";
        }
    }
}
