using System;

namespace Pyrra.Domain.Nutricao {
    /// <summary>
    /// Marca que o plano semanal já foi copiado para um usuário/dia. Existe para que a
    /// semeadura não dependa de o dia estar vazio: apagar todos os itens passa a produzir um
    /// dia vazio de verdade, em vez de disparar a cópia de novo na próxima carga.
    ///
    /// Tabela própria, e não um campo no DailyScore, por dois motivos: o DailyScore é do
    /// módulo de focos e só ganha linha quando há check-in — quem não usa focos nunca teria
    /// onde guardar essa marca; e misturar um controle de nutrição na entidade de pontuação
    /// acoplaria dois módulos que hoje não se conhecem.
    /// </summary>
    public class NutritionPlanSeedLog {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Data no fuso do usuário — o dia que recebeu a cópia.</summary>
        public DateOnly Date { get; set; }

        public DateTime SeededAt { get; set; }
    }
}
