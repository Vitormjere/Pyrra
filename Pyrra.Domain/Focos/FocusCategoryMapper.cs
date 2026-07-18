using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Pyrra.Domain.Focos {
    public static class FocusCategoryMapper {
        private static readonly Dictionary<FocusCategory, int> PesoPadrao = new()
        {
            { FocusCategory.Hidratacao, 5 },
            { FocusCategory.Medicacao, 5 },
            { FocusCategory.Exercicio, 20 },
            { FocusCategory.Sono, 15 },
            { FocusCategory.Estudo, 20 },
            { FocusCategory.Leitura, 10 },
            { FocusCategory.Mental, 10 },
            { FocusCategory.Outro, 10 }
        };

        private static readonly Dictionary<FocusCategory, string[]> PalavrasChave = new()
        {
            { FocusCategory.Hidratacao, new[] { "agua", "hidrat" } },
            { FocusCategory.Medicacao, new[] { "remedio", "medicament", "vitamin" } },
            { FocusCategory.Exercicio, new[] { "trein", "academia", "malh", "corr", "musculacao" } },
            { FocusCategory.Sono, new[] { "dorm", "sono", "descans" } },
            { FocusCategory.Estudo, new[] { "estud", "faculdade", "materia" } },
            { FocusCategory.Leitura, new[] { "ler", "leitura", "livro" } },
            { FocusCategory.Mental, new[] { "medit", "respir" } }
        };

        public static (FocusCategory Category, int Weight) Categorize(string focusName) {
            string textoNormalizado = Normalizar(focusName);

            string[] palavrasDoTexto = textoNormalizado
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var (categoria, palavrasChave) in PalavrasChave) {
                if (palavrasChave.Any(chave => palavrasDoTexto.Any(palavra => palavra.StartsWith(chave)))) {
                    return (categoria, PesoPadrao[categoria]);
                }
            }

            return (FocusCategory.Outro, PesoPadrao[FocusCategory.Outro]);
        }

        private static string Normalizar(string texto) {
            string semAcento = texto
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .Aggregate("", (atual, c) => atual + c);

            return semAcento.ToLowerInvariant().Trim();
        }
    }
}