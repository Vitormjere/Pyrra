using System;

namespace Pyrra.Application.Common.Exceptions {
    // Não deixa remover categorias que possuem lançamentos vinculados
    public class CategoryInUseException : Exception {
        public CategoryInUseException()
            : base("Esta categoria tem lançamentos vinculados e não pode ser removida.") { }
    }
}
