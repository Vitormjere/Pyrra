using System.Text.RegularExpressions;
using Pyrra.Application.Common.Exceptions;

namespace Pyrra.Application.Common {
    // regra de força de senha usada em todo lugar que cria ou troca senha (registro, troca de
    // senha, criação de conta admin) — mudar a regra é uma edição só, e cada chamador continua
    // decidindo o que fazer com WeakPasswordException (os controllers já mapeiam pra 400)
    public static class PasswordPolicy {
        private static readonly Regex HasUppercase = new("[A-Z]", RegexOptions.Compiled);
        private static readonly Regex HasDigit     = new("[0-9]", RegexOptions.Compiled);

        public static void Validate(string password) {
            if (string.IsNullOrEmpty(password)
                || password.Length < 8
                || !HasUppercase.IsMatch(password)
                || !HasDigit.IsMatch(password)) {
                throw new WeakPasswordException();
            }
        }
    }
}
