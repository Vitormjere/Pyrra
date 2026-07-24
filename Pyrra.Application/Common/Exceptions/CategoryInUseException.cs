using System;

namespace Pyrra.Application.Common.Exceptions {
    // Categoria com lançamentos vinculados não pode ser removida: apagar deixaria os
    // lançamentos apontando para um id inexistente. Vira 409 no controller.
    public class CategoryInUseException : Exception {
        public CategoryInUseException()
            : base("Esta categoria tem lançamentos vinculados e não pode ser removida.") { }
    }
}
