using System;
using System.Security.Cryptography;
using System.Text;

namespace Pyrra.Infrastructure.Data.Seed {
    /// <summary>
    /// GUID estável derivado de uma chave textual. Usado pelo seed dos templates: o HasData do EF
    /// exige valores de chave CONSTANTES entre execuções — se fossem Guid.NewGuid(), toda geração de
    /// migration veria "novas" linhas e produziria um diff espúrio. Derivar de uma string fixa
    /// (ex.: "template-5-day-2-ex-1") dá o mesmo GUID em qualquer máquina, para sempre.
    ///
    /// MD5 aqui não é escolha de segurança — é só um hash rápido e determinístico de 128 bits, que é
    /// exatamente o tamanho de um GUID.
    /// </summary>
    internal static class DeterministicGuid {
        public static Guid From(string key) {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(key));
            return new Guid(bytes);
        }
    }
}
