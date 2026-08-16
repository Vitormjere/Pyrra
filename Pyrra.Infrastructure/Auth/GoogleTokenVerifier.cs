using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pyrra.Application.Auth;

namespace Pyrra.Infrastructure.Auth {
    // fica na Infrastructure porque depende do SDK do Google — Application só conhece
    // IGoogleTokenVerifier, mesmo padrão do JwtTokenService e do HCaptchaVerificationService
    public class GoogleTokenVerifier : IGoogleTokenVerifier {
        private readonly GoogleAuthSettings _settings;
        private readonly ILogger<GoogleTokenVerifier> _logger;

        public GoogleTokenVerifier(IOptions<GoogleAuthSettings> settings, ILogger<GoogleTokenVerifier> logger) {
            _settings = settings.Value;
            _logger   = logger;
        }

        public async Task<GoogleUserInfo?> VerifyAsync(string idToken, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(idToken)) {
                return null;
            }

            try {
                // ValidateAsync confere a assinatura contra as chaves públicas do Google, o
                // emissor (accounts.google.com) e a expiração — Audience garante que o token foi
                // emitido especificamente pro client ID do Pyrra, não pra outro app qualquer que
                // também use Sign In With Google
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings {
                    Audience = new[] { _settings.ClientId }
                });

                return new GoogleUserInfo(payload.Subject, payload.Email, payload.EmailVerified, payload.Name ?? payload.Email);
            } catch (InvalidJwtException ex) {
                // token expirado, assinatura errada, audience de outro app etc. — não é bug nosso, é tentativa inválida
                _logger.LogWarning(ex, "Token do Google inválido ao tentar login.");
                return null;
            } catch (Exception ex) {
                _logger.LogError(ex, "Falha ao verificar token do Google.");
                return null;
            }
        }
    }
}
