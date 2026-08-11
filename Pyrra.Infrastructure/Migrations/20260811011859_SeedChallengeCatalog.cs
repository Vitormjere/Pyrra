using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedChallengeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ChallengeCategories",
                columns: new[] { "Id", "Color", "CreatedAt", "Description", "Icon", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), 1, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Um capítulo por vez, sem pressa.", "book-open", "Leitura", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), 2, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Suor, disciplina e progresso.", "dumbbell", "Academia", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), 1, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Constância nos estudos, desafio a desafio.", "graduation-cap", "Estudo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), 0, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Sai de casa, respira, repara no mundo lá fora.", "trees", "Natureza / Ar livre", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), 3, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Desafios que só valem com companhia.", "users", "Social", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), 4, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Pra quem vive de tênis no pé.", "footprints", "Corrida", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), 2, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "De tudo um pouco, pra sair da rotina.", "shuffle", "Aleatórios", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), 5, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Organização e cuidado com o espaço onde você vive.", "home", "Casa", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), 0, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Comer bem também é treino.", "apple", "Nutrição", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Challenges",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Deadline", "Description", "Points", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("000d8b4d-e8f2-9eac-7a08-4279f8041ab4"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto logo depois de correr", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("06cb0a84-b4df-2f3f-0cd5-3a7bd9afa387"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto fazendo careta", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0aacf029-74d4-cc04-2d08-0327930e4484"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do maior halter que você conseguir levantar", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0cf6515a-13ed-3041-e3c1-cb128331e6dd"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma paisagem que te surpreendeu", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0d97a43f-f8f3-ee23-c3f2-9c50c5e256eb"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do micro-ondas", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0fd3b552-ded1-a416-51b2-3bbe94e51767"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto em pose de modelo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("129d4f5c-3aa6-2b9a-550d-7ee19052dc58"), new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do seu material de estudo organizado", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("188c2b28-2489-fb77-cde3-645d1cb22aec"), new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto estudando", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1e035a61-22e0-c4c9-138a-ae1093a35008"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de um carro branco", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1f50da8b-fd11-4a23-731c-d10e4ed4558b"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma anotação ou resenha sobre o que você leu", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2309631b-d46a-5b23-1b7f-0cd94868c6fe"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de um copo d'água", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("27321d61-8cee-d3f3-7147-3ee6e1810f79"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto no parque", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("280787d1-51e0-eafe-788b-1ec714c196cf"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto se jogando pra cima (pulando)", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("28cfd586-cb6b-7ceb-4b32-63e165dc4da7"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do mapa/percurso da corrida", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2d019993-6a3d-22c7-6fed-fecfef4e5ee6"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo que comece com a letra B", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2e60c522-d724-0535-c456-a2c70cf23bc5"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto na pista", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30f7c3c3-29f0-213b-d74b-7a10b6d6cbbb"), new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da tela do computador estudando", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("34ab7030-9804-4e33-13c1-7144ac7d1087"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de um sabonete", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("35cf094f-122f-a345-8028-e0f195991b77"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto no aparelho de supino", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3dd8b5d0-a677-d4b4-6190-790414bde79f"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto fazendo alongamento", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3f314638-ccec-b51a-4c1b-eaf7eb3f0a11"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da sua ficha de treino", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3fc0d3c9-ad66-db65-018b-f6d0485eced2"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do sofá", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40fda704-5cf2-a858-2390-2990b607603e"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto segurando um halter", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("41204d99-b3bf-0a85-7b4b-4d780a4c9b55"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de roupa lavada e estendida", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("42d93cef-bf02-f2b9-2f86-34ee0c89d5ea"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma planta regada", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4d302594-d26d-be36-076e-13c0ff031e0f"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do quarto organizado", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4fde0a36-dd9c-1ff7-2bf6-8eaef6d404e9"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do pôr do sol ou nascer do sol", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("516e3b2d-9721-f6b5-2bbf-3c05eb5b85f0"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da pia limpa", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5744cfd3-600f-7966-068f-8903f3f01f01"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do guarda-roupa organizado", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("61f4f737-a4a5-92b6-c2fa-a66d0f31c4c1"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto no espelho da academia", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("685d7320-2ac1-9a9b-5a7a-9706bbfd955e"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da vista da sua janela", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("68f24918-1e31-27ab-c98f-060e13822222"), new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do seu caderno", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("691e91fc-c247-d278-087f-cce0a00f6c55"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma página marcada ou grifada", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("693bd89f-5a55-de85-c8cc-d97a1b3631cb"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma refeição saudável", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("6ab9104b-da07-0f73-b4f0-5948b2f557c4"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto treinando com um amigo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("6b5cdc1a-832f-af25-d71e-fa24ae779287"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da cama arrumada", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("72866daf-6b5e-d340-d851-34fc10ae75c0"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do céu", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("80e2f848-406b-13b2-6231-82c74890b5d9"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do relógio/app depois de correr", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("81a77bdd-e995-3858-f1e8-d6f16b5fa794"), new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto fazendo joinha com alguém", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8210c020-f5c2-15ad-413a-c24e139ba933"), new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto das suas anotações do dia", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("823f16d8-5acd-7e67-c610-02cae08ecaca"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do livro que você está lendo agora", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("85771985-39a2-f0ad-a0d0-cdb2060660cf"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma salada", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8da23b2d-e400-b59c-d4e2-91e0712c32d2"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto tirada de cabeça pra baixo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8ee018fa-a562-85df-4da0-822a580d0774"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma árvore ou planta que chamou sua atenção", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8f210950-7a96-2515-d414-081524598123"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo amarelo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("97bc5397-aa89-62a1-6cc5-fef3b31646aa"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da mesa limpa", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("98517718-ca11-ae94-30f5-37f516352896"), new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto dando um abraço", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("9b594017-adfb-b62e-6bab-72f43199830d"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto descalço na grama ou na areia", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("9e8bfbeb-063d-9463-3afc-7ed66fa84f4f"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de um trecho que te marcou", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("9fb7cfa7-5281-d2b8-ce05-899e44fdd6bc"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do objeto mais inútil da sua casa", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a3ec5c45-a168-ef28-b805-8c0d067e5545"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto no leg press", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a9355982-cfd6-40f7-5023-c5fbd3f1a8ff"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da sua estante de livros", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ab44b026-fe7e-609e-4a03-b0195dcf7e78"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da sua garrafinha de água", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("abbcb9b9-80ac-ccba-4a2d-993f80365eb8"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo da mesma cor da sua camiseta", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b19a9c6b-89d4-50c2-c101-baba3a302563"), new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto com um amigo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b49377d6-0e6d-3a56-1fd7-27158778111f"), new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do seu resumo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b8e0302a-0444-c7e9-8c3b-02d0ff16b0eb"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da capa de um livro que você acabou de terminar", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b964a349-51d4-d704-524d-a1393db2a06c"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do pôr do sol ou nascer do sol durante a corrida", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b9fbef3a-87c6-d375-cb9d-b45d7dd22347"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto se preparando pra cozinhar", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("bc3731cc-ed59-a1c1-5a64-639634178bb0"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de um animal que você encontrou ao ar livre", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("be4dadf4-f44a-6848-fc61-578363b283eb"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto depois de terminar o treino", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("bfeb9daa-0ffc-e9b1-c2d9-43cd9370d096"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do tênis sujo de barro ou poeira", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c01337a2-4a90-3108-41b1-a3c959214b6d"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo vermelho", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c2ed0ad8-4d33-31f2-2727-d4a18ed76b7b"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto fazendo prancha", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("cae97a01-e9f0-ba16-758c-99f6076ffedc"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do banheiro limpo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("cf216d15-6ba8-af67-9415-4b6a0634aaf7"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do lixo levado pra fora", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d443045a-7035-6f1b-cce0-198381182f80"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo que comece com a letra A", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d68071ab-3146-c159-d8f8-7459ec7eb27f"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto com meias diferentes", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d71e57ea-bf79-c509-840a-5a1f9b52ab4e"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do seu tênis de corrida", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d74fad2e-1221-fb41-2d0e-ce9ff5d52478"), new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto com alguém usando roupa da mesma cor que a sua", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d946723d-f3ff-a01a-6b83-855ec60fe8f8"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto em pose de \"acabei de ganhar na loteria\"", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d9d5a541-4547-a9de-ef76-5fc3961d512b"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto segurando uma colher, faca, caneca ou escova de dente", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("dbe2bc5b-b69d-0dff-ad91-c0ea6e24f8d1"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto antes de começar o treino", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("de209f1b-49e1-a434-371a-d900f49962b8"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo azul", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("de9cf16c-85a6-e61a-ee2d-f6da1ce0ac1b"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto na esteira", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e1b74596-7ce0-a67a-c366-6574eb983f86"), new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto lendo em um lugar diferente do normal", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e1fbbf6b-f4b0-5aba-a845-86a5a9ad3b75"), new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto fazendo aquecimento", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e2563ae0-ba85-3404-bd0c-75bffc79617f"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do objeto mais velho que você encontrar", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e32f80b0-2c93-066f-a7a4-52ff3d8827f1"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de um pedido de iFood", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e95baa1b-29fb-f065-ebfb-5a55e01764d3"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do primeiro objeto que você olhar ao seu redor", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ec7381b0-63d7-1bf9-c3e5-217b95a487de"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma vista de algum lugar alto", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ef2ae982-e0d6-2550-4589-66c38d32fd28"), new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto surpreendendo um amigo", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f07eb719-1281-9624-6adc-04653cdad5a0"), new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto da geladeira", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f54bfc2f-5c37-1348-3b88-bec8ad9f41c0"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto com uma fruta", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f62e2c4e-64e2-8123-5fb1-04d7d000343f"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo que te faz feliz", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f7127d87-af06-a43e-af5c-2b47022d9d5f"), new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto correndo com alguém", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f73a8610-e5cf-5a78-3118-c3f34c7654a4"), new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de uma trilha ou caminhada", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f863e1f5-4591-0bec-91af-b3ce517a46a5"), new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto em grupo (3 ou mais pessoas)", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f8fd1bfc-5233-30f9-78d6-434dab6167ef"), new Guid("ae2325fa-1818-2130-6d2d-79182583f717"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto de algo que comece com a letra C", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fbf1a715-acba-d285-45b8-76cd6ae8a4a8"), new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"), new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 10, "Foto do seu café da manhã", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("0136f75d-8c9a-248d-17f4-76a00dbc9126"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("243ab577-c0cd-8f4a-2038-3cf561734a7b"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("44fa2c74-4749-575d-7806-f1d4cf6ce9e3"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("7be8bcd0-e3e6-e447-71e3-c6ad8dd36987"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("8802da89-fb55-47d2-57fc-c9ad653efb79"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("8b888e00-40e8-1f6e-201b-f59b9f94883a"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("ae2325fa-1818-2130-6d2d-79182583f717"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("c76a88a3-74c9-754e-01a6-a551f845dc4c"));

            migrationBuilder.DeleteData(
                table: "ChallengeCategories",
                keyColumn: "Id",
                keyValue: new Guid("c9bc3f5d-0900-20ad-3a00-9bb031b931cb"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("000d8b4d-e8f2-9eac-7a08-4279f8041ab4"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("06cb0a84-b4df-2f3f-0cd5-3a7bd9afa387"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("0aacf029-74d4-cc04-2d08-0327930e4484"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("0cf6515a-13ed-3041-e3c1-cb128331e6dd"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("0d97a43f-f8f3-ee23-c3f2-9c50c5e256eb"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("0fd3b552-ded1-a416-51b2-3bbe94e51767"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("129d4f5c-3aa6-2b9a-550d-7ee19052dc58"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("188c2b28-2489-fb77-cde3-645d1cb22aec"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("1e035a61-22e0-c4c9-138a-ae1093a35008"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("1f50da8b-fd11-4a23-731c-d10e4ed4558b"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("2309631b-d46a-5b23-1b7f-0cd94868c6fe"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("27321d61-8cee-d3f3-7147-3ee6e1810f79"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("280787d1-51e0-eafe-788b-1ec714c196cf"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("28cfd586-cb6b-7ceb-4b32-63e165dc4da7"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("2d019993-6a3d-22c7-6fed-fecfef4e5ee6"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("2e60c522-d724-0535-c456-a2c70cf23bc5"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("30f7c3c3-29f0-213b-d74b-7a10b6d6cbbb"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("34ab7030-9804-4e33-13c1-7144ac7d1087"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("35cf094f-122f-a345-8028-e0f195991b77"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("3dd8b5d0-a677-d4b4-6190-790414bde79f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("3f314638-ccec-b51a-4c1b-eaf7eb3f0a11"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("3fc0d3c9-ad66-db65-018b-f6d0485eced2"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("40fda704-5cf2-a858-2390-2990b607603e"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("41204d99-b3bf-0a85-7b4b-4d780a4c9b55"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("42d93cef-bf02-f2b9-2f86-34ee0c89d5ea"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("4d302594-d26d-be36-076e-13c0ff031e0f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("4fde0a36-dd9c-1ff7-2bf6-8eaef6d404e9"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("516e3b2d-9721-f6b5-2bbf-3c05eb5b85f0"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("5744cfd3-600f-7966-068f-8903f3f01f01"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("61f4f737-a4a5-92b6-c2fa-a66d0f31c4c1"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("685d7320-2ac1-9a9b-5a7a-9706bbfd955e"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("68f24918-1e31-27ab-c98f-060e13822222"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("691e91fc-c247-d278-087f-cce0a00f6c55"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("693bd89f-5a55-de85-c8cc-d97a1b3631cb"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("6ab9104b-da07-0f73-b4f0-5948b2f557c4"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("6b5cdc1a-832f-af25-d71e-fa24ae779287"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("72866daf-6b5e-d340-d851-34fc10ae75c0"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("80e2f848-406b-13b2-6231-82c74890b5d9"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("81a77bdd-e995-3858-f1e8-d6f16b5fa794"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("8210c020-f5c2-15ad-413a-c24e139ba933"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("823f16d8-5acd-7e67-c610-02cae08ecaca"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("85771985-39a2-f0ad-a0d0-cdb2060660cf"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("8da23b2d-e400-b59c-d4e2-91e0712c32d2"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("8ee018fa-a562-85df-4da0-822a580d0774"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("8f210950-7a96-2515-d414-081524598123"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("97bc5397-aa89-62a1-6cc5-fef3b31646aa"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("98517718-ca11-ae94-30f5-37f516352896"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("9b594017-adfb-b62e-6bab-72f43199830d"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("9e8bfbeb-063d-9463-3afc-7ed66fa84f4f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("9fb7cfa7-5281-d2b8-ce05-899e44fdd6bc"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("a3ec5c45-a168-ef28-b805-8c0d067e5545"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("a9355982-cfd6-40f7-5023-c5fbd3f1a8ff"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("ab44b026-fe7e-609e-4a03-b0195dcf7e78"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("abbcb9b9-80ac-ccba-4a2d-993f80365eb8"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("b19a9c6b-89d4-50c2-c101-baba3a302563"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("b49377d6-0e6d-3a56-1fd7-27158778111f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("b8e0302a-0444-c7e9-8c3b-02d0ff16b0eb"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("b964a349-51d4-d704-524d-a1393db2a06c"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("b9fbef3a-87c6-d375-cb9d-b45d7dd22347"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("bc3731cc-ed59-a1c1-5a64-639634178bb0"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("be4dadf4-f44a-6848-fc61-578363b283eb"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("bfeb9daa-0ffc-e9b1-c2d9-43cd9370d096"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("c01337a2-4a90-3108-41b1-a3c959214b6d"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("c2ed0ad8-4d33-31f2-2727-d4a18ed76b7b"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("cae97a01-e9f0-ba16-758c-99f6076ffedc"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("cf216d15-6ba8-af67-9415-4b6a0634aaf7"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("d443045a-7035-6f1b-cce0-198381182f80"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("d68071ab-3146-c159-d8f8-7459ec7eb27f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("d71e57ea-bf79-c509-840a-5a1f9b52ab4e"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("d74fad2e-1221-fb41-2d0e-ce9ff5d52478"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("d946723d-f3ff-a01a-6b83-855ec60fe8f8"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("d9d5a541-4547-a9de-ef76-5fc3961d512b"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("dbe2bc5b-b69d-0dff-ad91-c0ea6e24f8d1"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("de209f1b-49e1-a434-371a-d900f49962b8"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("de9cf16c-85a6-e61a-ee2d-f6da1ce0ac1b"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("e1b74596-7ce0-a67a-c366-6574eb983f86"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("e1fbbf6b-f4b0-5aba-a845-86a5a9ad3b75"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("e2563ae0-ba85-3404-bd0c-75bffc79617f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("e32f80b0-2c93-066f-a7a4-52ff3d8827f1"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("e95baa1b-29fb-f065-ebfb-5a55e01764d3"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("ec7381b0-63d7-1bf9-c3e5-217b95a487de"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("ef2ae982-e0d6-2550-4589-66c38d32fd28"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f07eb719-1281-9624-6adc-04653cdad5a0"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f54bfc2f-5c37-1348-3b88-bec8ad9f41c0"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f62e2c4e-64e2-8123-5fb1-04d7d000343f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f7127d87-af06-a43e-af5c-2b47022d9d5f"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f73a8610-e5cf-5a78-3118-c3f34c7654a4"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f863e1f5-4591-0bec-91af-b3ce517a46a5"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("f8fd1bfc-5233-30f9-78d6-434dab6167ef"));

            migrationBuilder.DeleteData(
                table: "Challenges",
                keyColumn: "Id",
                keyValue: new Guid("fbf1a715-acba-d285-45b8-76cd6ae8a4a8"));
        }
    }
}
