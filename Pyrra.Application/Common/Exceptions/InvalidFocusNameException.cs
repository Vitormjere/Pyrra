using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidFocusNameException : Exception {
        public InvalidFocusNameException() : base("O nome do foco é obrigatório.") { }
    }
}
