using System;

namespace Pyrra.Application.Common.Exceptions {
    public class EmailAlreadyRegisteredException : Exception {
        public EmailAlreadyRegisteredException(string email) : base($"O e-mail '{email}' já está cadastrado.") { }
    }
}
