using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pyrra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TrainingDaysPerWeek = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateDays_WorkoutTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sets = table.Column<int>(type: "int", nullable: true),
                    Reps = table.Column<int>(type: "int", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_WorkoutTemplateDays_TemplateDayId",
                        column: x => x.TemplateDayId,
                        principalTable: "WorkoutTemplateDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "WorkoutTemplates",
                columns: new[] { "Id", "Description", "IsCustom", "Name", "Order", "TrainingDaysPerWeek" },
                values: new object[,]
                {
                    { new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b"), "Sem equipamento, usando o peso do corpo. 4x na semana.", false, "Treino em Casa", 8, 4 },
                    { new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c"), "Prioriza peito, costas, ombro e braço. 4x na semana.", false, "Foco em Superior", 7, 4 },
                    { new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72"), "Push, pull e perna repetidos, 6x na semana.", false, "PPL 6x", 5, 6 },
                    { new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9"), "Superior, inferior e um dia de corpo inteiro. 3x na semana.", false, "Upper/Lower/Full", 2, 3 },
                    { new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e"), "Prioriza pernas com 3 treinos de membro inferior.", false, "Foco em Perna", 6, 4 },
                    { new Guid("88d34b02-d38d-5f7b-d005-b04500eec348"), "Alterna superior e inferior em 4 treinos na semana.", false, "Upper/Lower", 3, 4 },
                    { new Guid("a81086b0-cf0b-a93f-3da9-64447e3de316"), "Monte sua própria semana do zero, dia a dia.", true, "Personalizado", 9, 0 },
                    { new Guid("b7e90323-7d06-e038-e693-18c4854530a0"), "Um grupo muscular por dia, 5x na semana.", false, "Bro Split", 4, 5 },
                    { new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637"), "Corpo inteiro a cada treino. Ideal para quem treina 3x na semana.", false, "Full Body", 1, 3 }
                });

            migrationBuilder.InsertData(
                table: "WorkoutTemplateDays",
                columns: new[] { "Id", "DayOfWeek", "Label", "TemplateId" },
                values: new object[,]
                {
                    { new Guid("07af67d7-abd3-6689-0f70-89b70db5acbc"), 6, "Descanso", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("0bd9e07d-e348-dfc5-901f-f7faba87373e"), 6, "Descanso", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("0f160f8a-a826-2d9f-a9bd-558d0fe63de6"), 3, "Descanso", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("124a9fea-78e9-4160-2bb7-83bc64b594e9"), 0, "Superior", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("15511b0c-49d0-76c7-cd75-a92ef4c9256b"), 3, "Ombro/Braço", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("18f96935-1988-0b4e-e7c7-ae6c6cc1362a"), 3, "Full Body", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("1b994df8-5f58-25cb-e11f-4fe18b06e924"), 5, "Descanso", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") },
                    { new Guid("2a5cc04a-c445-901b-10b8-b1d43db1a82b"), 4, "Braço", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("2af2f60b-b84d-e4ec-b027-f29dccc5b80c"), 0, "Peito/Tríceps", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("2b4a6d3d-1a90-c684-12bd-2c91d1a31272"), 5, "Descanso", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("2e03b09c-a81b-ea43-21c6-eed295424a79"), 6, "Descanso", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("2f3df307-0d88-b533-dcbe-a26ba52c14b7"), 1, "Inferior", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("37bbee21-662f-014b-d7a9-00ab9e344963"), 6, "Descanso", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("3f56c712-8a78-7eee-b341-41c378cd33f3"), 2, "Legs", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("3faaaaa6-c761-1a74-1ff3-a83dab83d77e"), 4, "Inferior B", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") },
                    { new Guid("401587a7-2634-2101-b4ee-bf2e54e05a21"), 2, "Descanso", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("463fa420-c57d-e59e-a6cf-5ecbf95b5c6a"), 5, "Descanso", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("46ed7c27-284a-d370-d8d6-8396b2d441d4"), 3, "Descanso", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("4a3888e0-8aae-ebcc-84e0-037b5abdc14e"), 1, "Descanso", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("4ac2fa0f-32c7-9bee-cf57-be0df59f572e"), 3, "Ombro", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("4aeaf84d-4887-0ce6-20db-b2d000c517ac"), 0, "Peito", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("56a1f7bf-0178-51cf-ecff-ecd0e11dfe11"), 5, "Descanso", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf"), 0, "Full Body A", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("58d91cee-fa71-5b3f-a217-e4979bcf4267"), 6, "Descanso", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") },
                    { new Guid("5e916a67-8707-f002-ca02-c1ae5c7d41e2"), 3, "Descanso", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("60410404-9a58-e5f1-23b7-2c49f79265da"), 6, "Descanso", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("666cf4f3-3b2d-d497-3056-0e1c8f4ae470"), 1, "Costas/Bíceps", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("66bbd7d3-03e1-e79e-5d4d-6e8dde0fff96"), 6, "Descanso", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("6830c187-05cf-b924-3d64-8c76df508b61"), 1, "Descanso", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("6b34a99f-f8c6-6bc8-8503-cfe339f5a992"), 1, "Costas", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("70e75ed8-8f82-f7a9-e400-777cc92fc17a"), 3, "Push", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("741c6153-477f-f523-a912-b7e9d16ebe81"), 2, "Descanso", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") },
                    { new Guid("74b0e1f1-8ad7-0141-ef60-a8cc684ac392"), 6, "Descanso", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("7c5bf9fa-d9b1-6e58-3b29-b6552e6a2136"), 2, "Descanso", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("8121edfd-a972-46e4-0ef3-e81bf0ba13cc"), 5, "Descanso", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("83f858ad-2d72-be8a-991d-fb1c7b139db9"), 2, "Full Body B", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("86f11613-97e9-f034-4ba4-de8839d3c235"), 0, "Superior A", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") },
                    { new Guid("8d7e89df-7c7d-5b0a-2547-6d1542ae1c93"), 4, "Full Body", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("926a35d3-2fbb-7ca8-3cea-1e9b12d17d41"), 2, "Perna", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("95160482-322e-fd3b-292a-de783dcef433"), 4, "Perna C (geral)", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("95f95b7a-79c9-76b9-8874-0e4c4896bbd1"), 5, "Descanso", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("a2d018e8-4b44-5610-a95c-1844a150c473"), 1, "Superior leve", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("a3110bd2-25b9-3d89-5ce6-9fe1ad8576fd"), 1, "Pull", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("a68e2d16-a3ff-b19a-51a8-557b8f83eb88"), 4, "Pull", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("b98fa435-ea44-0e3a-ff03-62482ad07472"), 2, "Inferior", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("c6b31389-c4d9-2cee-83d4-e650c8ac5575"), 0, "Perna A (quadríceps)", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("cc2c1edf-aaee-1669-7066-e2aaca5fbb6e"), 5, "Descanso", new Guid("b7e90323-7d06-e038-e693-18c4854530a0") },
                    { new Guid("d41ed357-08ef-c2a0-5ed4-6a499b862f6c"), 5, "Legs", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("d5d9c914-6c52-5b49-e662-9d7dba2ff484"), 3, "Superior B", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") },
                    { new Guid("d6f67462-35fc-0e21-cf5f-6fcb594191e1"), 2, "Perna B (posterior/glúteo)", new Guid("64d423fd-dc2b-3035-2e4e-0647c3fdfc2e") },
                    { new Guid("d6f96a3a-3dd1-21ff-f8f8-019cfed16569"), 4, "Perna (manutenção)", new Guid("2fa9b80e-7d0f-c9e6-478a-87870df2758c") },
                    { new Guid("d91d0e2e-6762-ad3e-96df-6ebdf72fc73e"), 0, "Push", new Guid("4fc3e12f-ab15-27b8-ea2e-d0012ba11c72") },
                    { new Guid("e7925f2b-65fc-dd8a-b9f1-9ef2a5b83cf1"), 0, "Superior", new Guid("63eb4965-9b4e-70f0-ef2f-08893ac3cee9") },
                    { new Guid("eaec17b1-f39a-c50c-4404-2c1118901a08"), 4, "Full Body C", new Guid("efff6a63-faa5-6b8d-79cb-1f9023ade637") },
                    { new Guid("f4ccb0e4-5cc0-c253-5cd5-2c670f2e5058"), 4, "Core/Cardio", new Guid("267ab9ca-8624-db13-6070-8d1c2e02a50b") },
                    { new Guid("fa959557-1fef-80a9-c4b8-07e2abf7056b"), 1, "Inferior A", new Guid("88d34b02-d38d-5f7b-d005-b04500eec348") }
                });

            migrationBuilder.InsertData(
                table: "WorkoutTemplateExercises",
                columns: new[] { "Id", "ExerciseName", "Order", "Reps", "Sets", "TemplateDayId" },
                values: new object[,]
                {
                    { new Guid("01667ed5-c9f6-61c6-7082-f88fc6a0daf2"), "Desenvolvimento ombro", 3, 12, 3, new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf") },
                    { new Guid("0417b3fb-5701-f6ec-96cd-70b8934e77b8"), "Abdômen", 4, 15, 3, new Guid("83f858ad-2d72-be8a-991d-fb1c7b139db9") },
                    { new Guid("051e501f-665d-865a-cff6-68d17e39dead"), "Abdômen", 4, 15, 3, new Guid("b98fa435-ea44-0e3a-ff03-62482ad07472") },
                    { new Guid("06990d25-0481-1dee-be1d-63dbec717906"), "Crossover", 2, 15, 3, new Guid("70e75ed8-8f82-f7a9-e400-777cc92fc17a") },
                    { new Guid("09e67ade-4392-fb03-2f45-bd42dcac4309"), "Desenvolvimento", 0, 10, 4, new Guid("4ac2fa0f-32c7-9bee-cf57-be0df59f572e") },
                    { new Guid("0a63d06d-ec66-709d-6ad3-3e57414d217d"), "Crucifixo", 1, 12, 3, new Guid("eaec17b1-f39a-c50c-4404-2c1118901a08") },
                    { new Guid("0acfd33a-108d-0fb0-cfd6-67aef8043593"), "Mesa flexora", 1, 12, 4, new Guid("d6f67462-35fc-0e21-cf5f-6fcb594191e1") },
                    { new Guid("0b380a64-162c-55d5-b421-2d6e83576d9f"), "Cadeira extensora", 1, 15, 3, new Guid("d41ed357-08ef-c2a0-5ed4-6a499b862f6c") },
                    { new Guid("0bb483a7-fbc3-ad98-b8b2-62becc731854"), "Remada curvada", 1, 10, 4, new Guid("e7925f2b-65fc-dd8a-b9f1-9ef2a5b83cf1") },
                    { new Guid("0e54aba0-e052-aa4a-ed52-4af425de9908"), "Supino reto", 0, 10, 4, new Guid("86f11613-97e9-f034-4ba4-de8839d3c235") },
                    { new Guid("0eea3063-f462-a22d-b8dc-67c7afbcbcb4"), "Abdômen bicicleta", 1, 20, 3, new Guid("f4ccb0e4-5cc0-c253-5cd5-2c670f2e5058") },
                    { new Guid("1102e3c4-c7db-1e85-a956-6cd3203b7447"), "Panturrilha", 3, 15, 4, new Guid("d41ed357-08ef-c2a0-5ed4-6a499b862f6c") },
                    { new Guid("15024687-1936-a8a6-2f42-03aec2d51fef"), "Elevação lateral", 3, 15, 3, new Guid("8d7e89df-7c7d-5b0a-2547-6d1542ae1c93") },
                    { new Guid("15245294-a02f-1f2d-a839-3df731c0af3e"), "Levantamento terra romeno", 1, 10, 4, new Guid("b98fa435-ea44-0e3a-ff03-62482ad07472") },
                    { new Guid("18428b43-17b1-94be-3d13-4037d4a9e77d"), "Puxada supinada", 1, 10, 3, new Guid("a68e2d16-a3ff-b19a-51a8-557b8f83eb88") },
                    { new Guid("1c529c6b-6f64-1f6c-c9bc-8b32f0efb010"), "Prancha", 2, 40, 3, new Guid("f4ccb0e4-5cc0-c253-5cd5-2c670f2e5058") },
                    { new Guid("23250030-e4ca-7b42-7fd3-279687fc481f"), "Rosca direta", 0, 12, 4, new Guid("2a5cc04a-c445-901b-10b8-b1d43db1a82b") },
                    { new Guid("24ece955-499d-411c-8a79-29c15a704f52"), "Agachamento com salto", 2, 12, 3, new Guid("18f96935-1988-0b4e-e7c7-ae6c6cc1362a") },
                    { new Guid("266983b9-a33e-9f4b-4312-3d4c8a041b89"), "Burpee", 0, 10, 3, new Guid("18f96935-1988-0b4e-e7c7-ae6c6cc1362a") },
                    { new Guid("2783e791-0243-75cd-9e4e-06c0670553fa"), "Panturrilha", 3, 15, 4, new Guid("eaec17b1-f39a-c50c-4404-2c1118901a08") },
                    { new Guid("2a1f3814-8dcd-8399-00fa-ffb41abb5da3"), "Agachamento", 0, 10, 4, new Guid("b98fa435-ea44-0e3a-ff03-62482ad07472") },
                    { new Guid("2b3c5c2c-e1c5-93d1-f166-33f8189fe8b3"), "Agachamento", 0, 10, 4, new Guid("fa959557-1fef-80a9-c4b8-07e2abf7056b") },
                    { new Guid("2c337028-5d7f-ef4b-3dc3-421d2bec5907"), "Tríceps testa", 3, 12, 3, new Guid("15511b0c-49d0-76c7-cd75-a92ef4c9256b") },
                    { new Guid("2c969bef-1c74-6a56-2cd1-dd4e9e9d775a"), "Mountain climber", 0, 20, 4, new Guid("f4ccb0e4-5cc0-c253-5cd5-2c670f2e5058") },
                    { new Guid("2d32039b-6564-ad88-dd47-78e03536c0bd"), "Tríceps testa", 4, 12, 3, new Guid("86f11613-97e9-f034-4ba4-de8839d3c235") },
                    { new Guid("2d6583ba-fbc8-14c3-f021-90f96579c49c"), "Elevação lateral", 3, 15, 3, new Guid("83f858ad-2d72-be8a-991d-fb1c7b139db9") },
                    { new Guid("30e25bd1-ccbb-ce64-1103-ab9dcc44c199"), "Supino inclinado", 1, 10, 4, new Guid("83f858ad-2d72-be8a-991d-fb1c7b139db9") },
                    { new Guid("3100c283-b5f4-28c2-cb02-0a4d35bf6e4e"), "Tríceps no banco/cadeira", 2, 12, 3, new Guid("124a9fea-78e9-4160-2bb7-83bc64b594e9") },
                    { new Guid("33895934-e4b1-2713-cf0d-7fef28470c53"), "Panturrilha", 4, 15, 4, new Guid("926a35d3-2fbb-7ca8-3cea-1e9b12d17d41") },
                    { new Guid("346085a2-3c40-5951-0236-ccfa8eb3b3ba"), "Supino inclinado", 0, 10, 3, new Guid("8d7e89df-7c7d-5b0a-2547-6d1542ae1c93") },
                    { new Guid("3683392b-502d-60bd-a22c-a1f1cc1f1d72"), "Leg press", 1, 12, 3, new Guid("d6f96a3a-3dd1-21ff-f8f8-019cfed16569") },
                    { new Guid("377ed285-dd53-b820-0260-89599272c1d8"), "Puxada frente", 2, 10, 4, new Guid("83f858ad-2d72-be8a-991d-fb1c7b139db9") },
                    { new Guid("37eb9d99-92f9-0f2f-4f84-e1af3582e31c"), "Rosca direta", 2, 12, 3, new Guid("a3110bd2-25b9-3d89-5ce6-9fe1ad8576fd") },
                    { new Guid("38035537-a102-79cc-be8e-9445566cea23"), "Cadeira extensora", 2, 15, 3, new Guid("926a35d3-2fbb-7ca8-3cea-1e9b12d17d41") },
                    { new Guid("3a4aeb6b-de81-f30d-4fa1-844ee4b60246"), "Tríceps corda", 3, 15, 3, new Guid("d91d0e2e-6762-ad3e-96df-6ebdf72fc73e") },
                    { new Guid("3b1bb7b0-4c74-e9c1-713d-ff64faf3e53a"), "Crucifixo", 2, 12, 3, new Guid("d91d0e2e-6762-ad3e-96df-6ebdf72fc73e") },
                    { new Guid("3b8dd436-11c5-2d3f-4f23-f22052f68373"), "Avanço", 3, 12, 3, new Guid("c6b31389-c4d9-2cee-83d4-e650c8ac5575") },
                    { new Guid("3ba8bbcf-f15f-768e-62d0-aa3db4039532"), "Polichinelo", 3, 30, 3, new Guid("f4ccb0e4-5cc0-c253-5cd5-2c670f2e5058") },
                    { new Guid("3baf6845-4acb-7ca5-4014-c88c5d3ee350"), "Agachamento livre", 0, 15, 4, new Guid("2f3df307-0d88-b533-dcbe-a26ba52c14b7") },
                    { new Guid("3dca0508-cba6-77ea-e323-52a40c4604bf"), "Rosca martelo", 1, 12, 3, new Guid("2a5cc04a-c445-901b-10b8-b1d43db1a82b") },
                    { new Guid("403db72a-628f-4926-ae2a-74b54bbd851c"), "Supino reto", 0, 10, 4, new Guid("4aeaf84d-4887-0ce6-20db-b2d000c517ac") },
                    { new Guid("41118502-da56-fce0-d613-50cacfee29d7"), "Rosca direta", 3, 12, 3, new Guid("86f11613-97e9-f034-4ba4-de8839d3c235") },
                    { new Guid("434be3b2-8517-ec02-1251-e2f7969c0e60"), "Elevação pélvica", 2, 12, 4, new Guid("d6f67462-35fc-0e21-cf5f-6fcb594191e1") },
                    { new Guid("43f888bb-2be1-d405-2478-7d284f2c522e"), "Remada baixa", 2, 12, 3, new Guid("6b34a99f-f8c6-6bc8-8503-cfe339f5a992") },
                    { new Guid("45283eec-b86f-177d-efa0-b2b331cf5cf4"), "Abdômen", 3, 15, 3, new Guid("95160482-322e-fd3b-292a-de783dcef433") },
                    { new Guid("469cf28f-0b75-c682-94ad-cf389b007a2d"), "Flexão de braço", 0, 12, 4, new Guid("124a9fea-78e9-4160-2bb7-83bc64b594e9") },
                    { new Guid("48aab5cc-8204-75df-a3bd-92037d508705"), "Cadeira abdutora", 2, 15, 3, new Guid("3faaaaa6-c761-1a74-1ff3-a83dab83d77e") },
                    { new Guid("49735ac9-8294-aae8-1ddb-b0fa6b8b1075"), "Pull-over", 3, 12, 3, new Guid("6b34a99f-f8c6-6bc8-8503-cfe339f5a992") },
                    { new Guid("4b023ce9-dd84-3d9c-4524-326c93767eba"), "Afundo", 1, 12, 3, new Guid("2f3df307-0d88-b533-dcbe-a26ba52c14b7") },
                    { new Guid("4eac2867-c357-e3d9-a69f-2f9842fd43ba"), "Flexão de braço", 1, 12, 3, new Guid("18f96935-1988-0b4e-e7c7-ae6c6cc1362a") },
                    { new Guid("4f130e84-3e48-9ad8-54c2-5061520fbd93"), "Supino reto", 1, 10, 4, new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf") },
                    { new Guid("59ee1467-be9d-b016-aaa6-f9e55faf16ad"), "Panturrilha", 2, 15, 4, new Guid("95160482-322e-fd3b-292a-de783dcef433") },
                    { new Guid("5c5aa428-3a08-cc92-2087-1ab76535ac65"), "Remada curvada", 1, 10, 4, new Guid("a3110bd2-25b9-3d89-5ce6-9fe1ad8576fd") },
                    { new Guid("5dd403c2-9b3d-3f9f-5c1b-9d0e2bdff235"), "Face pull", 3, 15, 3, new Guid("a3110bd2-25b9-3d89-5ce6-9fe1ad8576fd") },
                    { new Guid("5eb6e57b-381c-008c-e015-1e9a96606d25"), "Levantamento terra romeno", 0, 10, 4, new Guid("d6f67462-35fc-0e21-cf5f-6fcb594191e1") },
                    { new Guid("602bba91-f037-e079-347c-16dd23e1494e"), "Agachamento búlgaro", 0, 12, 3, new Guid("95160482-322e-fd3b-292a-de783dcef433") },
                    { new Guid("6121d8ca-8787-51eb-54d2-4eb6e9efa74a"), "Panturrilha", 3, 15, 4, new Guid("b98fa435-ea44-0e3a-ff03-62482ad07472") },
                    { new Guid("633a1913-3f2d-096e-bf75-d25feff95046"), "Panturrilha", 3, 15, 4, new Guid("fa959557-1fef-80a9-c4b8-07e2abf7056b") },
                    { new Guid("65bd8657-aa66-d42d-553f-6f866efbb97b"), "Leg press", 1, 12, 4, new Guid("926a35d3-2fbb-7ca8-3cea-1e9b12d17d41") },
                    { new Guid("66a29444-f9bd-ed45-a70c-49fb3a6d5285"), "Cadeira abdutora", 2, 15, 3, new Guid("d41ed357-08ef-c2a0-5ed4-6a499b862f6c") },
                    { new Guid("66c6122b-5857-bbc8-070b-9e8a29dd8f3a"), "Prancha", 3, 40, 3, new Guid("124a9fea-78e9-4160-2bb7-83bc64b594e9") },
                    { new Guid("67267b0f-6300-0ead-0326-f9d806aaa861"), "Supino inclinado", 1, 10, 4, new Guid("4aeaf84d-4887-0ce6-20db-b2d000c517ac") },
                    { new Guid("679779ff-d458-4402-7445-006a583ff534"), "Leg press", 1, 12, 4, new Guid("c6b31389-c4d9-2cee-83d4-e650c8ac5575") },
                    { new Guid("67d2d86e-fdda-d39a-63b3-0a54595275b7"), "Cadeira abdutora", 1, 15, 3, new Guid("95160482-322e-fd3b-292a-de783dcef433") },
                    { new Guid("68fe2651-b4bf-081c-b155-58d3ab307c69"), "Elevação lateral", 1, 15, 4, new Guid("4ac2fa0f-32c7-9bee-cf57-be0df59f572e") },
                    { new Guid("6abd7391-1bb9-bff3-7e0b-7b521071e2a2"), "Puxada frente", 1, 10, 4, new Guid("d5d9c914-6c52-5b49-e662-9d7dba2ff484") },
                    { new Guid("6d10711a-73fc-1324-3752-e336862477f1"), "Cadeira extensora", 2, 15, 4, new Guid("c6b31389-c4d9-2cee-83d4-e650c8ac5575") },
                    { new Guid("6dd5c07b-55dc-dc2a-46d1-6bb4fdf61871"), "Encolhimento", 3, 15, 3, new Guid("4ac2fa0f-32c7-9bee-cf57-be0df59f572e") },
                    { new Guid("7430a81b-a5c5-ee49-29e2-7a89bf4e2829"), "Cadeira extensora", 1, 12, 3, new Guid("fa959557-1fef-80a9-c4b8-07e2abf7056b") },
                    { new Guid("746358a0-49ca-53ba-5b90-053b8759fe71"), "Supino inclinado", 0, 10, 4, new Guid("70e75ed8-8f82-f7a9-e400-777cc92fc17a") },
                    { new Guid("7532af28-7eb8-6a0f-86fd-7f6993ff6879"), "Supino reto", 0, 10, 4, new Guid("d91d0e2e-6762-ad3e-96df-6ebdf72fc73e") },
                    { new Guid("772cc3fe-d10f-7d67-bb07-0c96532c6b71"), "Agachamento livre", 2, 10, 3, new Guid("8d7e89df-7c7d-5b0a-2547-6d1542ae1c93") },
                    { new Guid("773de9a5-143e-6404-03c9-19ba0b1d2c5d"), "Remada curvada", 1, 10, 4, new Guid("666cf4f3-3b2d-d497-3056-0e1c8f4ae470") },
                    { new Guid("77d0a6f9-5440-1ca4-726c-f0b4145377de"), "Remada curvada", 1, 10, 4, new Guid("6b34a99f-f8c6-6bc8-8503-cfe339f5a992") },
                    { new Guid("79426593-940e-6ada-c401-3959406a3db4"), "Puxada frente", 1, 10, 3, new Guid("8d7e89df-7c7d-5b0a-2547-6d1542ae1c93") },
                    { new Guid("7cb29348-7ec2-74ca-e8d1-e24f32f7db72"), "Crossover", 2, 15, 3, new Guid("2af2f60b-b84d-e4ec-b027-f29dccc5b80c") },
                    { new Guid("7cce6eda-f445-c59a-9320-43d5fcd6182a"), "Elevação frontal", 2, 12, 3, new Guid("4ac2fa0f-32c7-9bee-cf57-be0df59f572e") },
                    { new Guid("7d3481fa-b17a-54b6-74fc-aa8c18ce81d1"), "Tríceps corda", 5, 12, 3, new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf") },
                    { new Guid("88590895-3af6-5736-b8a2-09b7bccbd387"), "Flexão diamante", 1, 10, 3, new Guid("124a9fea-78e9-4160-2bb7-83bc64b594e9") },
                    { new Guid("8a2b7001-a187-b53e-717a-2b715bcc2cbd"), "Mesa flexora", 2, 12, 3, new Guid("3f56c712-8a78-7eee-b341-41c378cd33f3") },
                    { new Guid("8aa1991e-2271-4f4a-eeaf-6f9f3c755709"), "Desenvolvimento", 2, 12, 3, new Guid("86f11613-97e9-f034-4ba4-de8839d3c235") },
                    { new Guid("8afe53e9-0a58-776e-968a-e17e0b834ec2"), "Leg press", 1, 12, 4, new Guid("3faaaaa6-c761-1a74-1ff3-a83dab83d77e") },
                    { new Guid("8c11329f-0d3b-4ad9-7443-82e23cdd7b16"), "Elevação lateral", 1, 15, 4, new Guid("15511b0c-49d0-76c7-cd75-a92ef4c9256b") },
                    { new Guid("8c1bbe79-81bb-7882-3f2e-31cdf1b7ec59"), "Remada baixa", 2, 10, 4, new Guid("eaec17b1-f39a-c50c-4404-2c1118901a08") },
                    { new Guid("8d8c4c7e-86bc-81a8-6197-547b593b481f"), "Remada baixa", 0, 10, 4, new Guid("a68e2d16-a3ff-b19a-51a8-557b8f83eb88") },
                    { new Guid("8e3459e0-59e6-b93d-985d-58e2029d2994"), "Tríceps corda", 3, 15, 3, new Guid("2af2f60b-b84d-e4ec-b027-f29dccc5b80c") },
                    { new Guid("8e9fe1e5-a421-4875-3695-8f4ca4435d64"), "Desenvolvimento", 2, 12, 3, new Guid("a2d018e8-4b44-5610-a95c-1844a150c473") },
                    { new Guid("915a8c8c-95f1-71dc-e2f1-7f3d3c090634"), "Panturrilha", 3, 15, 4, new Guid("3f56c712-8a78-7eee-b341-41c378cd33f3") },
                    { new Guid("94f790bc-f1b1-0117-0372-948513098f96"), "Remada curvada", 1, 10, 4, new Guid("86f11613-97e9-f034-4ba4-de8839d3c235") },
                    { new Guid("97d4cbd5-84f9-fd84-b5ce-107857e62f23"), "Crucifixo", 2, 12, 3, new Guid("4aeaf84d-4887-0ce6-20db-b2d000c517ac") },
                    { new Guid("9826d6cd-0daf-97cb-b478-2a051e059ed9"), "Tríceps testa", 2, 12, 4, new Guid("2a5cc04a-c445-901b-10b8-b1d43db1a82b") },
                    { new Guid("9e130200-928d-a96b-ba49-b9648b8e68b1"), "Desenvolvimento", 2, 12, 3, new Guid("e7925f2b-65fc-dd8a-b9f1-9ef2a5b83cf1") },
                    { new Guid("9e569ad8-83e7-cf1c-4aa5-faf3838800be"), "Supino reto", 0, 10, 4, new Guid("2af2f60b-b84d-e4ec-b027-f29dccc5b80c") },
                    { new Guid("a23f92ea-f4c2-8fd6-fde7-a91123623887"), "Agachamento", 0, 10, 4, new Guid("926a35d3-2fbb-7ca8-3cea-1e9b12d17d41") },
                    { new Guid("a41c86b7-d37b-3aa6-dcea-455aa1893897"), "Puxada frente", 1, 12, 3, new Guid("a2d018e8-4b44-5610-a95c-1844a150c473") },
                    { new Guid("a4a185dd-30f3-efa2-9a0a-c01e05bd4cb0"), "Tríceps corda", 3, 15, 3, new Guid("2a5cc04a-c445-901b-10b8-b1d43db1a82b") },
                    { new Guid("ac77ae51-80c1-b7ab-c520-151cdfefddb8"), "Mesa flexora", 2, 12, 3, new Guid("fa959557-1fef-80a9-c4b8-07e2abf7056b") },
                    { new Guid("ac9a1326-5a4f-89b0-8e3f-09d7900520ac"), "Puxada frente", 0, 10, 4, new Guid("6b34a99f-f8c6-6bc8-8503-cfe339f5a992") },
                    { new Guid("ad14a775-42f7-a81c-e6fb-4fa1c6e3a13e"), "Rosca direta", 2, 12, 3, new Guid("666cf4f3-3b2d-d497-3056-0e1c8f4ae470") },
                    { new Guid("ad6b6819-5325-19a3-95b0-d4ed81e02002"), "Desenvolvimento", 1, 12, 3, new Guid("d91d0e2e-6762-ad3e-96df-6ebdf72fc73e") },
                    { new Guid("ada18861-297a-599b-8268-72dd5de29d3e"), "Remada curvada", 2, 10, 4, new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf") },
                    { new Guid("ae80d9f3-ce02-e79d-a45f-d53f8c9092d9"), "Prancha", 4, 40, 3, new Guid("8d7e89df-7c7d-5b0a-2547-6d1542ae1c93") },
                    { new Guid("aeab5e5c-d766-790e-3918-32673334469f"), "Desenvolvimento", 0, 10, 4, new Guid("15511b0c-49d0-76c7-cd75-a92ef4c9256b") },
                    { new Guid("b1b67cf2-00c4-cbc6-97c8-30e3acf4f6d5"), "Rosca direta", 4, 12, 3, new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf") },
                    { new Guid("b264f762-dc1c-03ad-c7a8-a4adc2272d6f"), "Agachamento", 0, 10, 4, new Guid("3f56c712-8a78-7eee-b341-41c378cd33f3") },
                    { new Guid("b48c75f5-fe7b-d326-6865-be5f388df8eb"), "Agachamento", 0, 10, 4, new Guid("c6b31389-c4d9-2cee-83d4-e650c8ac5575") },
                    { new Guid("b6c6fd25-7db1-999a-a2af-64616c608387"), "Tríceps testa", 3, 12, 3, new Guid("70e75ed8-8f82-f7a9-e400-777cc92fc17a") },
                    { new Guid("b729e984-68a8-ac8b-b5af-ad2944bd4b5f"), "Puxada frente", 0, 10, 4, new Guid("a3110bd2-25b9-3d89-5ce6-9fe1ad8576fd") },
                    { new Guid("be9ca879-3e5b-3573-9993-281ad887892f"), "Levantamento terra", 0, 8, 4, new Guid("83f858ad-2d72-be8a-991d-fb1c7b139db9") },
                    { new Guid("bf73a54b-ea79-9c87-39f4-6a1e091917da"), "Panturrilha", 3, 15, 4, new Guid("3faaaaa6-c761-1a74-1ff3-a83dab83d77e") },
                    { new Guid("c0100012-ac31-d535-86c5-b0d2ef50a5b9"), "Leg press", 0, 12, 4, new Guid("eaec17b1-f39a-c50c-4404-2c1118901a08") },
                    { new Guid("c04ba3ee-6de1-cbdd-ada7-29197fd212c9"), "Puxada frente", 0, 10, 4, new Guid("666cf4f3-3b2d-d497-3056-0e1c8f4ae470") },
                    { new Guid("c29724dd-4dd2-ae8d-7111-a8f11b56bc34"), "Elevação de panturrilha", 2, 20, 4, new Guid("2f3df307-0d88-b533-dcbe-a26ba52c14b7") },
                    { new Guid("c2fbc579-3525-c0ce-1d9e-b92b49d42ef5"), "Ponte de glúteo", 3, 15, 3, new Guid("2f3df307-0d88-b533-dcbe-a26ba52c14b7") },
                    { new Guid("c383937b-39b0-da6c-ea28-c173d7918659"), "Elevação lateral", 2, 15, 3, new Guid("d5d9c914-6c52-5b49-e662-9d7dba2ff484") },
                    { new Guid("c6087645-f1e9-0300-5b67-7d69a58da26b"), "Rosca martelo", 3, 12, 3, new Guid("d5d9c914-6c52-5b49-e662-9d7dba2ff484") },
                    { new Guid("c70fc173-3cd3-c7a7-dbca-d5c1bf02f3e4"), "Agachamento", 0, 10, 4, new Guid("5708dea8-5b27-0525-b0d4-09ea451077bf") },
                    { new Guid("c9008dad-9907-bd85-64df-0a7c32db8de1"), "Mesa flexora", 3, 15, 3, new Guid("926a35d3-2fbb-7ca8-3cea-1e9b12d17d41") },
                    { new Guid("c94d63ad-dae6-fe04-a964-6f0fcb28e61c"), "Supino inclinado", 0, 10, 4, new Guid("d5d9c914-6c52-5b49-e662-9d7dba2ff484") },
                    { new Guid("ca0ad601-f99c-ed80-146d-9a3abd65b329"), "Crossover", 3, 15, 3, new Guid("4aeaf84d-4887-0ce6-20db-b2d000c517ac") },
                    { new Guid("ccbd75f9-99c7-0181-3406-d368c89ea41d"), "Leg press", 2, 12, 3, new Guid("b98fa435-ea44-0e3a-ff03-62482ad07472") },
                    { new Guid("d54f2a5b-c592-58a4-eedb-2e7004fab1e7"), "Levantamento terra", 0, 8, 4, new Guid("d41ed357-08ef-c2a0-5ed4-6a499b862f6c") },
                    { new Guid("dabf32e8-57cf-05ce-5d7a-2b5dd96fcd0e"), "Rosca direta", 3, 12, 3, new Guid("e7925f2b-65fc-dd8a-b9f1-9ef2a5b83cf1") },
                    { new Guid("de7feade-a624-a223-c7c1-47fdfe5ac601"), "Panturrilha", 2, 15, 3, new Guid("d6f96a3a-3dd1-21ff-f8f8-019cfed16569") },
                    { new Guid("dee5383a-01f3-463d-5654-9e3dff80dd44"), "Tríceps corda", 4, 12, 3, new Guid("d5d9c914-6c52-5b49-e662-9d7dba2ff484") },
                    { new Guid("e0f1140e-7181-ad52-e230-7b2e15b76c16"), "Tríceps corda", 4, 12, 3, new Guid("e7925f2b-65fc-dd8a-b9f1-9ef2a5b83cf1") },
                    { new Guid("e1c2bb9a-d803-2462-75d0-168d4d52353f"), "Pull-over", 3, 12, 3, new Guid("a68e2d16-a3ff-b19a-51a8-557b8f83eb88") },
                    { new Guid("e5f8377d-5a94-644b-daea-3fc98383d1f6"), "Panturrilha", 3, 15, 4, new Guid("d6f67462-35fc-0e21-cf5f-6fcb594191e1") },
                    { new Guid("e5fea39b-9181-2fe7-252e-7a70f54e982c"), "Rosca martelo", 3, 12, 3, new Guid("666cf4f3-3b2d-d497-3056-0e1c8f4ae470") },
                    { new Guid("e7bb3958-fdeb-464c-0a0a-3ef69c154319"), "Supino", 0, 12, 3, new Guid("a2d018e8-4b44-5610-a95c-1844a150c473") },
                    { new Guid("ea54b507-f58b-a166-4792-3fc04a577fac"), "Leg press", 1, 12, 4, new Guid("3f56c712-8a78-7eee-b341-41c378cd33f3") },
                    { new Guid("ea559291-f6fe-3263-9a2f-035d963f7020"), "Prancha lateral", 3, 30, 3, new Guid("18f96935-1988-0b4e-e7c7-ae6c6cc1362a") },
                    { new Guid("ead6b3cb-9ecc-b2c8-e7bb-fa4209a1a3b3"), "Rosca scott", 2, 12, 3, new Guid("15511b0c-49d0-76c7-cd75-a92ef4c9256b") },
                    { new Guid("eef9fcdc-aeaa-7ddc-5665-d398ed4979e5"), "Elevação lateral", 1, 15, 3, new Guid("70e75ed8-8f82-f7a9-e400-777cc92fc17a") },
                    { new Guid("f1d8e3a4-7919-c133-968d-61d13f945f6d"), "Levantamento terra", 0, 8, 4, new Guid("3faaaaa6-c761-1a74-1ff3-a83dab83d77e") },
                    { new Guid("f4d3adfb-c062-b00b-823c-b80b4e15a26a"), "Rosca martelo", 2, 12, 3, new Guid("a68e2d16-a3ff-b19a-51a8-557b8f83eb88") },
                    { new Guid("f809e8ca-59c8-62b9-1a2f-a9478438aabc"), "Prancha", 4, 40, 3, new Guid("eaec17b1-f39a-c50c-4404-2c1118901a08") },
                    { new Guid("fb16c186-6bca-48cb-c9e6-d470c593aec8"), "Agachamento", 0, 12, 3, new Guid("d6f96a3a-3dd1-21ff-f8f8-019cfed16569") },
                    { new Guid("fbc6db40-efae-426c-8e54-9ec461ffce0f"), "Supino reto", 0, 10, 4, new Guid("e7925f2b-65fc-dd8a-b9f1-9ef2a5b83cf1") },
                    { new Guid("fc1fd451-8cc3-5bf8-bbf4-cb61cc2ff364"), "Supino inclinado", 1, 10, 3, new Guid("2af2f60b-b84d-e4ec-b027-f29dccc5b80c") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateDays_TemplateId",
                table: "WorkoutTemplateDays",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_TemplateDayId",
                table: "WorkoutTemplateExercises",
                column: "TemplateDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutTemplateExercises");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateDays");

            migrationBuilder.DropTable(
                name: "WorkoutTemplates");
        }
    }
}
