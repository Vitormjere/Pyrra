using System;

namespace Pyrra.Domain.Desafios {
    // Categoria curada por admin (ex.: "Corrida", "Academia"). Só admin cria/edita/remove — ver
    // ChallengeCatalogService. Times ativam categorias (TeamActiveCategory, próxima etapa) para
    // liberar os desafios dela aos membros.
    public class ChallengeCategory {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Nome do ícone (lucide-react), mesmo espírito do BannerTheme: o front mapeia o nome pro
        // componente de ícone real.
        public string Icon { get; set; } = string.Empty;
        public ChallengeCategoryColor Color { get; set; } = ChallengeCategoryColor.Verde;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Mesma paleta de TeamBannerTheme, por consistência visual — tipos separados porque são
    // conceitos de domínios diferentes (Comunidade vs Desafios), não a mesma cor reaproveitada.
    public enum ChallengeCategoryColor {
        Verde,
        Azul,
        Roxo,
        Laranja,
        Vermelho,
        Dourado
    }
}
