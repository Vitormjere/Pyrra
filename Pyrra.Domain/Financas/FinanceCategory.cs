using System;

namespace Pyrra.Domain.Financas {
    public class FinanceCategory {
        public Guid Id { get; set; }

        // nulo é categoria padrão do sistema, visível pra todo mundo 
        public Guid? UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        // redundante com UserId == null nas categorias semeadas, mas explícito: permite marcar uma padrão futura sem depender da leitura implícita do null
        public bool IsDefault { get; set; }
    }
}
