using System;
using System.Collections.Generic;
using Pyrra.Domain.Desafios;

namespace Pyrra.Infrastructure.Data.Seed {
    /// <summary>
    /// Fonte de verdade do catálogo inicial de categorias e desafios (mesmo espírito do
    /// WorkoutTemplateSeed). Pra acrescentar um desafio no futuro: some um item em
    /// <see cref="Definitions"/> e gere uma nova migration — a chave determinística usa
    /// categoria+título, então os já existentes não mudam de Id.
    ///
    /// Todos os desafios aqui entram com 10 pontos: é uma leva de tarefas do tipo "uma foto de X",
    /// sem meta numérica (correr 10km, streak de N dias) que justifique pontuação maior — dificuldade
    /// homogênea, pontuação homogênea. Ajustar manualmente depois é responsabilidade do admin.
    /// </summary>
    internal static class ChallengeCatalogSeed {
        private const int DefaultPoints = 10;

        private sealed record Ch(string Title);
        private sealed record Cat(string Name, string Description, string Icon, ChallengeCategoryColor Color, Ch[] Challenges);

        // valor fixo (não DateTime.UtcNow) — HasData exige constantes, senão toda geração de
        // migration veria "mudança" nas linhas semeadas e produziria um diff espúrio
        private static readonly DateTime SeedTimestamp = new(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);

        private static Ch[] Titles(params string[] titles) {
            var result = new Ch[titles.Length];
            for (var i = 0; i < titles.Length; i++) result[i] = new Ch(titles[i]);
            return result;
        }

        private static readonly Cat[] Definitions = {
            new("Aleatórios", "De tudo um pouco, pra sair da rotina.", "shuffle", ChallengeCategoryColor.Roxo, Titles(
                "Foto segurando uma colher, faca, caneca ou escova de dente",
                "Foto de algo vermelho",
                "Foto de algo azul",
                "Foto de algo amarelo",
                "Foto de um carro branco",
                "Foto do objeto mais inútil da sua casa",
                "Foto do objeto mais velho que você encontrar",
                "Foto fazendo careta",
                "Foto em pose de modelo",
                "Foto em pose de \"acabei de ganhar na loteria\"",
                "Foto se jogando pra cima (pulando)",
                "Foto de algo que comece com a letra A",
                "Foto de algo que comece com a letra B",
                "Foto de algo que comece com a letra C",
                "Foto com meias diferentes",
                "Foto de algo da mesma cor da sua camiseta",
                "Foto de algo que te faz feliz",
                "Foto tirada de cabeça pra baixo",
                "Foto do primeiro objeto que você olhar ao seu redor"
            )),

            new("Social", "Desafios que só valem com companhia.", "users", ChallengeCategoryColor.Laranja, Titles(
                "Foto com um amigo",
                "Foto dando um abraço",
                "Foto com alguém usando roupa da mesma cor que a sua",
                "Foto em grupo (3 ou mais pessoas)",
                "Foto fazendo joinha com alguém",
                "Foto surpreendendo um amigo"
            )),

            new("Estudo", "Constância nos estudos, desafio a desafio.", "graduation-cap", ChallengeCategoryColor.Azul, Titles(
                "Foto estudando",
                "Foto do seu caderno",
                "Foto do seu resumo",
                "Foto das suas anotações do dia",
                "Foto da tela do computador estudando",
                "Foto do seu material de estudo organizado"
            )),

            new("Casa", "Organização e cuidado com o espaço onde você vive.", "home", ChallengeCategoryColor.Dourado, Titles(
                "Foto da mesa limpa",
                "Foto da cama arrumada",
                "Foto do quarto organizado",
                "Foto da pia limpa",
                "Foto do guarda-roupa organizado",
                "Foto do banheiro limpo",
                "Foto da geladeira",
                "Foto do micro-ondas",
                "Foto do sofá",
                "Foto de um sabonete",
                "Foto da vista da sua janela",
                "Foto de uma planta regada",
                "Foto do lixo levado pra fora",
                "Foto de roupa lavada e estendida"
            )),

            new("Nutrição", "Comer bem também é treino.", "apple", ChallengeCategoryColor.Verde, Titles(
                "Foto com uma fruta",
                "Foto se preparando pra cozinhar",
                "Foto de uma refeição saudável",
                "Foto de uma salada",
                "Foto do seu café da manhã",
                "Foto de um pedido de iFood",
                "Foto de um copo d'água",
                "Foto da sua garrafinha de água"
            )),

            new("Corrida", "Pra quem vive de tênis no pé.", "footprints", ChallengeCategoryColor.Vermelho, Titles(
                "Foto do seu tênis de corrida",
                "Foto no parque",
                "Foto na pista",
                "Foto do relógio/app depois de correr",
                "Foto logo depois de correr",
                "Foto correndo com alguém",
                "Foto do mapa/percurso da corrida",
                "Foto do tênis sujo de barro ou poeira",
                "Foto do pôr do sol ou nascer do sol durante a corrida"
            )),

            new("Academia", "Suor, disciplina e progresso.", "dumbbell", ChallengeCategoryColor.Roxo, Titles(
                "Foto fazendo prancha",
                "Foto segurando um halter",
                "Foto no aparelho de supino",
                "Foto no leg press",
                "Foto na esteira",
                "Foto no espelho da academia",
                "Foto fazendo alongamento",
                "Foto fazendo aquecimento",
                "Foto treinando com um amigo",
                "Foto do maior halter que você conseguir levantar",
                "Foto da sua ficha de treino",
                "Foto antes de começar o treino",
                "Foto depois de terminar o treino"
            )),

            new("Leitura", "Um capítulo por vez, sem pressa.", "book-open", ChallengeCategoryColor.Azul, Titles(
                "Foto do livro que você está lendo agora",
                "Foto de uma página marcada ou grifada",
                "Foto da sua estante de livros",
                "Foto lendo em um lugar diferente do normal",
                "Foto da capa de um livro que você acabou de terminar",
                "Foto de uma anotação ou resenha sobre o que você leu",
                "Foto de um trecho que te marcou"
            )),

            new("Natureza / Ar livre", "Sai de casa, respira, repara no mundo lá fora.", "trees", ChallengeCategoryColor.Verde, Titles(
                "Foto do céu",
                "Foto do pôr do sol ou nascer do sol",
                "Foto de uma trilha ou caminhada",
                "Foto de uma árvore ou planta que chamou sua atenção",
                "Foto de uma vista de algum lugar alto",
                "Foto descalço na grama ou na areia",
                "Foto de um animal que você encontrou ao ar livre",
                "Foto de uma paisagem que te surpreendeu"
            )),
        };

        private static readonly (List<ChallengeCategory> Categories, List<Challenge> Challenges) Built = Build();

        public static IReadOnlyList<ChallengeCategory> Categories => Built.Categories;
        public static IReadOnlyList<Challenge> Challenges => Built.Challenges;

        private static (List<ChallengeCategory>, List<Challenge>) Build() {
            var categories = new List<ChallengeCategory>();
            var challenges = new List<Challenge>();

            foreach (var cat in Definitions) {
                var categoryId = DeterministicGuid.From($"challenge-category-{cat.Name}");

                categories.Add(new ChallengeCategory {
                    Id          = categoryId,
                    Name        = cat.Name,
                    Description = cat.Description,
                    Icon        = cat.Icon,
                    Color       = cat.Color,
                    CreatedAt   = SeedTimestamp,
                    UpdatedAt   = SeedTimestamp
                });

                foreach (var ch in cat.Challenges) {
                    challenges.Add(new Challenge {
                        Id         = DeterministicGuid.From($"challenge-{cat.Name}-{ch.Title}"),
                        CategoryId = categoryId,
                        Title      = ch.Title,
                        Description = null,
                        Points     = DefaultPoints,
                        Deadline   = null,
                        CreatedAt  = SeedTimestamp,
                        UpdatedAt  = SeedTimestamp
                    });
                }
            }

            return (categories, challenges);
        }
    }
}
