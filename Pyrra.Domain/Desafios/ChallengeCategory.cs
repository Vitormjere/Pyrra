using System;

namespace Pyrra.Domain.Desafios {
    // categoria curada por admin (ex.: "Corrida", "Academia")
    public class ChallengeCategory {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // nome do ícone (lucide-react), mesmo espírito do BannerTheme: o front mapeia o nome pro componente de ícone real
        public string Icon { get; set; } = string.Empty;
        public ChallengeCategoryColor Color { get; set; } = ChallengeCategoryColor.Verde;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // mesma paleta de TeamBannerTheme, por consistência visual 
    public enum ChallengeCategoryColor {
        Verde,
        Azul,
        Roxo,
        Laranja,
        Vermelho,
        Dourado
    }
}
