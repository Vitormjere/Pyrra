using System.Collections.Generic;
using Pyrra.Domain.Achievements;

namespace Pyrra.Infrastructure.Data.Seed {
    /// <summary>
    /// Fonte de verdade do catálogo de conquistas. Pra acrescentar uma conquista no futuro: some um
    /// item em <see cref="Definitions"/> e gere uma nova migration — a chave determinística é
    /// Tipo+Marco, então as existentes não mudam de Id mesmo reordenando a lista.
    ///
    /// Marcos de Streak reaproveitam os valores já existentes em StreakMilestones (Pyrra.Domain.Focos)
    /// pra bater exatamente com os marcos que o StreakService já celebra.
    /// </summary>
    internal static class AchievementSeed {
        private sealed record Def(
            AchievementType Type, int Milestone, AchievementRarity? Rarity, int Xp, string Name, string Description, string IconKey);

        private static readonly Def[] Definitions = {
            new(AchievementType.Streak, 3, AchievementRarity.Bronze, 10, "Primeiros Passos", "Alcance uma sequência de 3 dias.", "streak-bronze-3"),
            new(AchievementType.Streak, 10, AchievementRarity.Bronze, 25, "Constância", "Alcance uma sequência de 10 dias.", "streak-bronze-10"),
            new(AchievementType.Streak, 30, AchievementRarity.Prata, 75, "Um Mês de Foco", "Alcance uma sequência de 30 dias.", "streak-prata-30"),
            new(AchievementType.Streak, 60, AchievementRarity.Prata, 150, "Dois Meses Fortes", "Alcance uma sequência de 60 dias.", "streak-prata-60"),
            new(AchievementType.Streak, 100, AchievementRarity.Ouro, 300, "Centena", "Alcance uma sequência de 100 dias.", "streak-ouro-100"),
            new(AchievementType.Streak, 200, AchievementRarity.Esmeralda, 600, "Imparável", "Alcance uma sequência de 200 dias.", "streak-esmeralda-200"),
            new(AchievementType.Streak, 1000, AchievementRarity.Ametista, 3000, "Lenda", "Alcance uma sequência de 1000 dias.", "streak-ametista-1000"),

            new(AchievementType.DesafioCompleto, 1, null, 15, "Primeiro Desafio", "Complete seu primeiro desafio.", "desafio-1"),
            new(AchievementType.DesafioCompleto, 10, null, 75, "Desafiante", "Complete 10 desafios.", "desafio-10"),
            new(AchievementType.DesafioCompleto, 50, null, 400, "Veterano", "Complete 50 desafios.", "desafio-50"),
            new(AchievementType.DesafioCompleto, 100, null, 900, "Mestre dos Desafios", "Complete 100 desafios.", "desafio-100"),
        };

        private static readonly List<Achievement> Built = Build();

        public static IReadOnlyList<Achievement> Achievements => Built;

        private static List<Achievement> Build() {
            var list = new List<Achievement>();

            foreach (var def in Definitions) {
                list.Add(new Achievement {
                    Id          = DeterministicGuid.From($"achievement-{def.Type}-{def.Milestone}"),
                    Type        = def.Type,
                    Milestone   = def.Milestone,
                    Rarity      = def.Rarity,
                    Xp          = def.Xp,
                    Name        = def.Name,
                    Description = def.Description,
                    IconKey     = def.IconKey
                });
            }

            return list;
        }
    }
}
