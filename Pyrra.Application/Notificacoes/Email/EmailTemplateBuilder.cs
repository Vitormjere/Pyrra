using System;
using System.Net;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Notificacoes.Email {
    // Monta o HTML dos e-mails do Pyrra — um template só, reaproveitado pelos 4 tipos de e-mail
    // (confirmação, redefinição de senha, convite aceito, conquista desbloqueada).
    //
    // Fundo externo claro (não o preto do app): e-mail totalmente escuro tende a ser sinalizado
    // por filtro de spam com mais frequência, e vários clientes (Outlook em especial) ignoram
    // fundo escuro definido por CSS e forçam texto escuro sobre fundo escuro — ilegível. O card
    // interno escuro com o acento em destaque já carrega a identidade visual sem esse risco.
    // Tabelas + estilo inline em vez de classes: é o que realmente funciona de forma consistente
    // entre clientes de e-mail (Gmail, Outlook, Apple Mail todos têm suporte parcial e
    // inconsistente pra <style> e CSS moderno).
    public static class EmailTemplateBuilder {
        // mesma paleta de utils/accentColors.ts no frontend — sem uma fonte compartilhada entre
        // C# e TypeScript, os dois precisam ser mantidos em sincronia manualmente se a paleta mudar
        public static string AccentHex(AccentColor color) => color switch {
            AccentColor.Verde    => "#02F5A1",
            AccentColor.Azul     => "#3B9EFF",
            AccentColor.Rosa     => "#FF2E9F",
            AccentColor.Roxo     => "#B14EFF",
            AccentColor.Vermelho => "#FF4B4B",
            AccentColor.Laranja  => "#FF8A1E",
            AccentColor.Amarelo  => "#FFD400",
            _                    => "#02F5A1"
        };

        public static string Build(string accentHex, string heading, string bodyHtml, string? ctaText = null, string? ctaUrl = null) {
            var ctaHtml = ctaText is not null && ctaUrl is not null
                ? $"""
                   <table role="presentation" cellpadding="0" cellspacing="0" style="margin: 28px auto 4px;">
                     <tr>
                       <td style="border-radius: 999px; background-color: {accentHex};">
                         <a href="{Encode(ctaUrl)}" style="display: inline-block; padding: 14px 32px; font-family: Arial, Helvetica, sans-serif; font-size: 15px; font-weight: 700; color: #05090A; text-decoration: none;">
                           {Encode(ctaText)}
                         </a>
                       </td>
                     </tr>
                   </table>
                   """
                : "";

            return $"""
                <!doctype html>
                <html lang="pt-BR">
                  <head>
                    <meta charset="utf-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                    <title>{Encode(heading)}</title>
                  </head>
                  <body style="margin: 0; padding: 32px 16px; background-color: #F2F2F2; font-family: Arial, Helvetica, sans-serif;">
                    <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width: 480px; margin: 0 auto;">
                      <tr>
                        <td style="text-align: center; padding-bottom: 20px;">
                          <span style="font-size: 22px; font-weight: 700; letter-spacing: -0.02em; color: {accentHex};">Pyrra</span>
                        </td>
                      </tr>
                      <tr>
                        <td style="background-color: #05090A; border-radius: 16px; padding: 32px 28px;">
                          <h1 style="margin: 0 0 16px; font-size: 20px; font-weight: 700; color: #F4FFFB;">{Encode(heading)}</h1>
                          <div style="font-size: 15px; line-height: 1.6; color: #B8C4C8;">
                            {bodyHtml}
                          </div>
                          {ctaHtml}
                        </td>
                      </tr>
                      <tr>
                        <td style="text-align: center; padding-top: 20px; font-size: 12px; color: #8A9599;">
                          Pyrra — foco, treino, finanças e hábitos num só lugar.
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>
                """;
        }

        // pública porque quem monta o bodyHtml de cada tipo de e-mail (EmailNotificationService)
        // também precisa encodar os textos que vêm de dados do usuário (nome, nome do time etc.)
        // antes de interpolar no HTML
        public static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
