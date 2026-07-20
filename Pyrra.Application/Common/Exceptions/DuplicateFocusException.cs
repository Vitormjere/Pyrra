using System;

namespace Pyrra.Application.Common.Exceptions {
    public class DuplicateFocusException : Exception {
        public DuplicateFocusException(string name) : base($"Já existe um foco ativo chamado '{name}'.") { }
    }
}
