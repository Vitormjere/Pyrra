using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pyrra.Application.Desafios;

namespace Pyrra.Api.HostedServices {
    // roda periodicamente sorteando os 3 desafios do dia pros times que ainda não têm — ver
    // DailyChallengeGeneratorService pra lógica de sorteio em si. Intervalo de 15min: os horários
    // de revelação já são sorteados no dia inteiro (00h-23h59), então o intervalo só afeta o
    // quanto demora até o sorteio do dia novo rodar após a meia-noite, ou até um time que acabou
    // de ativar a primeira categoria ver seus desafios — 15min é imperceptível pros dois casos.
    public class DailyChallengeGeneratorHostedService : BackgroundService {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyChallengeGeneratorHostedService> _logger;

        public DailyChallengeGeneratorHostedService(
            IServiceScopeFactory scopeFactory, ILogger<DailyChallengeGeneratorHostedService> logger) {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // roda uma vez já na subida, sem esperar o primeiro tick — senão um restart do
            // servidor logo após a virada do dia deixaria times sem desafios até 15min depois
            await RunOnceAsync(stoppingToken);

            using var timer = new PeriodicTimer(PollInterval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken)) {
                await RunOnceAsync(stoppingToken);
            }
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken) {
            try {
                using var scope = _scopeFactory.CreateScope();
                var generator = scope.ServiceProvider.GetRequiredService<IDailyChallengeGeneratorService>();
                var processed = await generator.GenerateMissingForTodayAsync(cancellationToken);
                if (processed > 0) {
                    _logger.LogInformation("Sorteio diário de desafios: {Count} time(s) processado(s).", processed);
                }
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogError(ex, "Falha ao sortear os desafios do dia.");
            }
        }
    }
}
