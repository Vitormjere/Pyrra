using System;

namespace Pyrra.Domain.Financas {
    public class FinanceCategory {
        public Guid Id { get; set; }

        // Null = categoria padrão do sistema, visível para todos os usuários. Preenchido = categoria
        // criada por um usuário, visível só para ele. É o que separa o compartilhado do privado.
        public Guid? UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        // Redundante com UserId == null nas categorias semeadas, mas explícito: permite marcar uma
        // padrão futura sem depender da leitura implícita do null.
        public bool IsDefault { get; set; }
    }
}
