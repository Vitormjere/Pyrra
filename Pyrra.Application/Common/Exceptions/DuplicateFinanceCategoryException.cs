using System;

namespace Pyrra.Application.Common.Exceptions {
    public class DuplicateFinanceCategoryException : Exception {
        public DuplicateFinanceCategoryException(string name)
            : base($"Já existe uma categoria chamada '{name}'.") { }
    }
}
