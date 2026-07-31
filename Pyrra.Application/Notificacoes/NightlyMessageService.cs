using System;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Application.Focos;
using Pyrra.Domain.Focos;
using Pyrra.Domain.Users;

namespace Pyrra.Application.Notificacoes {
    public class NightlyMessageService : INightlyMessageService {
        private readonly IFocusCheckInService _checkInService;
        private readonly IUserRepository      _userRepository;

        public NightlyMessageService(IFocusCheckInService checkInService, IUserRepository userRepository) {
            _checkInService = checkInService;
            _userRepository = userRepository;
        }

        public async Task<ClosingMessage> GenerateClosingMessageAsync(Guid userId, CancellationToken cancellationToken = default) {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                throw new NotFoundException("Usuário não encontrado.");
            }

            // usa o DailyScore atual para gerar o preview da mensagem
            var result = await _checkInService.GetDailyScoreAsync(userId, null, cancellationToken);
            var score  = result.Score;

            var situation = Classify(score);
            var percent   = ToPercent(score.Percentage);
            var text      = ClosingMessageCatalog.Compose(user.CommunicationTone, situation, percent);

            return new ClosingMessage(text, user.CommunicationTone.ToString(), situation.ToString(), percent);
        }

        // ordem das condições garante a classificação correta da situação
        private static ClosingSituation Classify(DailyScore score) {
            if (score.PointsPossible == 0) {
                return ClosingSituation.SemFocos;
            }

            if (score.GoalMet) {
                return ClosingSituation.MetaBatida;
            }

            if (score.PointsEarned == 0) {
                return ClosingSituation.Nada;
            }

            return score.Percentage < 0.50m
                ? ClosingSituation.Longe
                : ClosingSituation.Perto;
        }

        // converte a fração em percentual inteiro para exibir na mensagem
        private static int ToPercent(decimal percentage) =>
            (int)Math.Round(percentage * 100, MidpointRounding.AwayFromZero);
    }
}
