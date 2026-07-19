using System;

namespace Pyrra.Application.Common.Exceptions {
    public class WeakPasswordException : Exception {
        public WeakPasswordException() : base("A senha deve ter no mínimo 8 caracteres.") { }
    }
}
