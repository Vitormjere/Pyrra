using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Tests.Comunidade;
using Pyrra.Application.Usuario;
using Pyrra.Domain.Users;
using Xunit;

namespace Pyrra.Application.Tests.Usuario {
    public class UserAccountServiceTests {
        private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Bob   = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // hasher real, não fake — é o mesmo PasswordHasher usado em produção, então o roundtrip hash/verify aqui é de verdade
        private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

        private User MakeUser(Guid id, string email, string password) {
            var user = new User { Id = id, Name = "Alice", Email = email };
            user.PasswordHash = Hasher.HashPassword(user, password);
            return user;
        }

        private static (UserAccountService service, FakeUserRepository users, FakeClock clock, FakeUserProfilePictureStorageService pictures) Build(params User[] users) {
            var repo = new FakeUserRepository(users);
            var clock = new FakeClock();
            var pictures = new FakeUserProfilePictureStorageService();
            var service = new UserAccountService(repo, Hasher, pictures, clock);
            return (service, repo, clock, pictures);
        }

        // ---- nome ----

        [Fact]
        public async Task UpdateNameAsync_AtualizaNome() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, clock, _) = Build(alice);

            var updated = await service.UpdateNameAsync(Alice, "  Alice Silva  ");

            Assert.Equal("Alice Silva", updated.Name);
            Assert.Equal(clock.UtcNow, updated.UpdatedAt);
        }

        [Fact]
        public async Task UpdateNameAsync_Vazio_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            await Assert.ThrowsAsync<InvalidAccountException>(() => service.UpdateNameAsync(Alice, "   "));
        }

        // ---- trocar e-mail ----

        [Fact]
        public async Task ChangeEmailAsync_ComSenhaCorreta_Atualiza() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            var updated = await service.ChangeEmailAsync(Alice, "novo@x.com", "SenhaForte123");

            Assert.Equal("novo@x.com", updated.Email);
        }

        [Fact]
        public async Task ChangeEmailAsync_NormalizaParaMinusculas() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            var updated = await service.ChangeEmailAsync(Alice, "  NOVO@X.com  ", "SenhaForte123");

            Assert.Equal("novo@x.com", updated.Email);
        }

        [Fact]
        public async Task ChangeEmailAsync_SenhaAtualErrada_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _) = Build(alice);

            await Assert.ThrowsAsync<IncorrectPasswordException>(
                () => service.ChangeEmailAsync(Alice, "novo@x.com", "SenhaErrada"));

            // nada mudou
            Assert.Equal("alice@x.com", users.Users.Single(u => u.Id == Alice).Email);
        }

        [Fact]
        public async Task ChangeEmailAsync_JaEmUsoPorOutroUsuario_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var bob   = MakeUser(Bob, "bob@x.com", "OutraSenha123");
            var (service, _, _, _) = Build(alice, bob);

            await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
                () => service.ChangeEmailAsync(Alice, "bob@x.com", "SenhaForte123"));
        }

        [Fact]
        public async Task ChangeEmailAsync_MesmoEmailAtual_EhNoOp() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            // mesmo e-mail (só a caixa muda, já normalizado) não deve checar unicidade contra si mesmo nem lançar
            var updated = await service.ChangeEmailAsync(Alice, "ALICE@X.COM", "SenhaForte123");

            Assert.Equal("alice@x.com", updated.Email);
        }

        // ---- trocar senha ----

        [Fact]
        public async Task ChangePasswordAsync_ComSenhaAtualCorreta_TrocaEValidaComANova() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _) = Build(alice);

            await service.ChangePasswordAsync(Alice, "SenhaForte123", "NovaSenha456");

            var stored = users.Users.Single(u => u.Id == Alice);
            var result = Hasher.VerifyHashedPassword(stored, stored.PasswordHash, "NovaSenha456");
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public async Task ChangePasswordAsync_SenhaAtualErrada_Lanca_ENaoTrocaANova() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _) = Build(alice);

            await Assert.ThrowsAsync<IncorrectPasswordException>(
                () => service.ChangePasswordAsync(Alice, "SenhaErrada", "NovaSenha456"));

            var stored = users.Users.Single(u => u.Id == Alice);
            // a senha antiga continua válida, a troca não foi aplicada
            var result = Hasher.VerifyHashedPassword(stored, stored.PasswordHash, "SenhaForte123");
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public async Task ChangePasswordAsync_NovaSenhaFraca_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            await Assert.ThrowsAsync<WeakPasswordException>(
                () => service.ChangePasswordAsync(Alice, "SenhaForte123", "curta"));
        }

        // ---- fuso horário ----

        [Fact]
        public async Task UpdateTimezoneAsync_ValidoIana_Atualiza() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            var updated = await service.UpdateTimezoneAsync(Alice, "America/New_York");

            Assert.Equal("America/New_York", updated.Timezone);
        }

        [Fact]
        public async Task UpdateTimezoneAsync_Invalido_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);

            await Assert.ThrowsAsync<InvalidAccountException>(
                () => service.UpdateTimezoneAsync(Alice, "Nao/Existe"));
        }

        // ---- privacidade do perfil ----

        [Fact]
        public async Task UpdateProfileVisibilityAsync_Atualiza() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock, _) = Build(alice);

            var updated = await service.UpdateProfileVisibilityAsync(Alice, ProfileVisibility.SomenteAmigos);

            Assert.Equal(ProfileVisibility.SomenteAmigos, updated.ProfileVisibility);
            Assert.Equal(clock.UtcNow, users.Users.Single(u => u.Id == Alice).UpdatedAt);
        }

        // ---- cor de destaque ----

        [Fact]
        public async Task UpdateAccentColorAsync_Atualiza() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock, _) = Build(alice);

            var updated = await service.UpdateAccentColorAsync(Alice, AccentColor.Roxo);

            Assert.Equal(AccentColor.Roxo, updated.AccentColor);
            Assert.Equal(clock.UtcNow, users.Users.Single(u => u.Id == Alice).UpdatedAt);
        }

        // ---- exclusão de conta ----

        [Fact]
        public async Task DeleteAccountAsync_ComSenhaCorreta_MarcaDeletedAt() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock, _) = Build(alice);

            await service.DeleteAccountAsync(Alice, "SenhaForte123");

            var stored = users.Users.Single(u => u.Id == Alice);
            Assert.Equal(clock.UtcNow, stored.DeletedAt);
        }

        [Fact]
        public async Task DeleteAccountAsync_ContaExcluidaSomeDeTodaConsulta() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _) = Build(alice);

            await service.DeleteAccountAsync(Alice, "SenhaForte123");

            // o próprio repositório já não encontra mais o usuário — é isso que faz uma sessão existente perder efeito na próxima chamada
            Assert.Null(await users.GetByIdAsync(Alice));
            Assert.Null(await users.GetByEmailAsync("alice@x.com"));
        }

        [Fact]
        public async Task DeleteAccountAsync_SenhaErrada_Lanca_ENaoMarcaDeletedAt() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, _, _) = Build(alice);

            await Assert.ThrowsAsync<IncorrectPasswordException>(
                () => service.DeleteAccountAsync(Alice, "SenhaErrada"));

            Assert.Null(users.Users.Single(u => u.Id == Alice).DeletedAt);
        }

        [Fact]
        public async Task DeleteAccountAsync_Novamente_LancaNotFound() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, _) = Build(alice);
            await service.DeleteAccountAsync(Alice, "SenhaForte123");

            // segunda tentativa não encontra a conta (mesmo critério de sumir de toda consulta), fica indistinguível de usuário inexistente
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAccountAsync(Alice, "SenhaForte123"));
        }

        // ---- foto de perfil ----

        [Fact]
        public async Task SetProfilePictureAsync_TipoValido_ArmazenaEAtualizaUrl() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, users, clock, pictures) = Build(alice);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var updated = await service.SetProfilePictureAsync(Alice, stream, "image/png", stream.Length);

            Assert.Equal($"https://fake.blob.core.windows.net/profile-pictures/{Alice:N}", updated.ProfilePictureUrl);
            Assert.Equal(1, pictures.UploadCallCount);
            Assert.Equal(clock.UtcNow, users.Users.Single(u => u.Id == Alice).UpdatedAt);
        }

        [Fact]
        public async Task SetProfilePictureAsync_TipoInvalido_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, pictures) = Build(alice);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            await Assert.ThrowsAsync<InvalidAccountException>(
                () => service.SetProfilePictureAsync(Alice, stream, "application/pdf", stream.Length));

            Assert.Equal(0, pictures.UploadCallCount);
        }

        [Fact]
        public async Task SetProfilePictureAsync_MaiorQue3MB_Lanca() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, pictures) = Build(alice);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            await Assert.ThrowsAsync<InvalidAccountException>(
                () => service.SetProfilePictureAsync(Alice, stream, "image/png", 3 * 1024 * 1024 + 1));

            Assert.Equal(0, pictures.UploadCallCount);
        }

        [Fact]
        public async Task RemoveProfilePictureAsync_ComFoto_RemoveEZeraUrl() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            alice.ProfilePictureUrl = "https://fake.blob.core.windows.net/profile-pictures/existente";
            var (service, users, _, pictures) = Build(alice);

            var updated = await service.RemoveProfilePictureAsync(Alice);

            Assert.Null(updated.ProfilePictureUrl);
            Assert.Equal(1, pictures.DeleteCallCount);
            Assert.Null(users.Users.Single(u => u.Id == Alice).ProfilePictureUrl);
        }

        [Fact]
        public async Task RemoveProfilePictureAsync_SemFoto_NaoChamaStorage() {
            var alice = MakeUser(Alice, "alice@x.com", "SenhaForte123");
            var (service, _, _, pictures) = Build(alice);

            await service.RemoveProfilePictureAsync(Alice);

            Assert.Equal(0, pictures.DeleteCallCount);
        }
    }
}
