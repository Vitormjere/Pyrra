using System;

namespace Pyrra.Domain.Achievements {
    // catálogo fixo de conquistas (seed), cada linha é um marco que desbloqueia
    public class Achievement {
        public Guid Id { get; set; }
        public AchievementType Type { get; set; }
        public int Milestone { get; set; }

        // só se aplica a Type == Streak; nulo nos demais tipos
        public AchievementRarity? Rarity { get; set; }

        public int Xp { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
    }

    public enum AchievementType {
        Streak,
        DesafioCompleto,
        TorneioPodio
    }

    public enum AchievementRarity {
        Bronze,
        Prata,
        Ouro,
        Esmeralda,
        Ametista
    }
}
