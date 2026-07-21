using System;

namespace Pyrra.Domain.Focos {
    // Marcos: 3, 10, 20, 30, 45, 60, 100, 150, 200, depois de 100 em 100 até 1000,
    // e a partir de 1000, de 200 em 200.
    public static class StreakMilestones {
        private static readonly int[] FixedMilestones = { 3, 10, 20, 30, 45, 60, 100, 150, 200 };

        public static bool IsMilestone(int count) {
            if (count < FixedMilestones[0]) {
                return false;
            }

            if (Array.IndexOf(FixedMilestones, count) >= 0) {
                return true;
            }

            // De 1000 em diante o passo dobra: 1000, 1200, 1400...
            if (count >= 1000) {
                return (count - 1000) % 200 == 0;
            }

            // Entre 200 e 1000: 300, 400, ... 900. (250 não é marco.)
            return count > 200 && count % 100 == 0;
        }
    }
}
