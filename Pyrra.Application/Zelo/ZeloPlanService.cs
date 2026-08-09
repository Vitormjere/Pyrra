using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Nutricao;
using Pyrra.Domain.Treinos;
using Pyrra.Domain.Users;
using Pyrra.Domain.Zelo;

namespace Pyrra.Application.Zelo {
    public class ZeloPlanService : IZeloPlanService {
        // limite diário do Zelo conversacional, separado do DailyLimit do Zelo geral (ZeloService)
        private const int DailyLimit = 20;

        private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);

        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IZeloPlanSessionRepository     _sessionRepository;
        private readonly IZeloPlanAnswerRepository      _answerRepository;
        private readonly IZeloPlanMessageRepository     _messageRepository;
        private readonly IZeloPlanQueryLogRepository    _queryLogRepository;
        private readonly IZeloContextBuilder            _contextBuilder;
        private readonly IZeloPlanAssistant             _assistant;
        private readonly IWorkoutPlanDayRepository      _workoutPlanDayRepository;
        private readonly IWorkoutPlanExerciseRepository _workoutPlanExerciseRepository;
        private readonly INutritionPlanItemRepository   _nutritionPlanItemRepository;
        private readonly IUserRepository                _userRepository;
        private readonly IClockService                  _clock;

        public ZeloPlanService(
            IZeloPlanSessionRepository     sessionRepository,
            IZeloPlanAnswerRepository      answerRepository,
            IZeloPlanMessageRepository     messageRepository,
            IZeloPlanQueryLogRepository    queryLogRepository,
            IZeloContextBuilder            contextBuilder,
            IZeloPlanAssistant             assistant,
            IWorkoutPlanDayRepository      workoutPlanDayRepository,
            IWorkoutPlanExerciseRepository workoutPlanExerciseRepository,
            INutritionPlanItemRepository   nutritionPlanItemRepository,
            IUserRepository                userRepository,
            IClockService                  clock) {
            _sessionRepository             = sessionRepository;
            _answerRepository              = answerRepository;
            _messageRepository             = messageRepository;
            _queryLogRepository            = queryLogRepository;
            _contextBuilder                = contextBuilder;
            _assistant                     = assistant;
            _workoutPlanDayRepository      = workoutPlanDayRepository;
            _workoutPlanExerciseRepository = workoutPlanExerciseRepository;
            _nutritionPlanItemRepository   = nutritionPlanItemRepository;
            _userRepository                = userRepository;
            _clock                         = clock;
        }

        public async Task<ZeloPlanSessionState> StartOrResumeAsync(Guid userId, CancellationToken cancellationToken = default) {
            var existing = await _sessionRepository.GetActiveForUserAsync(userId, _clock.UtcNow, cancellationToken);
            if (existing is not null) {
                return await BuildStateAsync(existing, cancellationToken);
            }

            var session = new ZeloPlanSession {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Status    = ZeloPlanSessionStatus.Coletando,
                CreatedAt = _clock.UtcNow,
                ExpiresAt = _clock.UtcNow.Add(SessionLifetime)
            };
            await _sessionRepository.AddAsync(session, cancellationToken);

            var firstQuestion = ZeloPlanQuestionFlow.GetNextQuestion(Array.Empty<ZeloPlanAnswer>());
            return new ZeloPlanSessionState(session.Id, session.Status, firstQuestion, 0);
        }

        public async Task<ZeloPlanSessionState> AnswerAsync(Guid userId, Guid sessionId, string answer, CancellationToken cancellationToken = default) {
            var session = await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            if (session.Status != ZeloPlanSessionStatus.Coletando) {
                throw new InvalidZeloPlanException("Este formulário já foi concluído.");
            }

            var answered = await _answerRepository.GetBySessionIdAsync(sessionId, cancellationToken);
            var currentQuestion = ZeloPlanQuestionFlow.GetNextQuestion(answered);
            if (currentQuestion is null) {
                throw new InvalidZeloPlanException("Este formulário já foi concluído.");
            }

            var normalizedAnswer = answer?.Trim();
            if (string.IsNullOrEmpty(normalizedAnswer)) {
                throw new InvalidZeloPlanException("Informe uma resposta.");
            }
            if (currentQuestion.Options is not null && !currentQuestion.Options.Contains(normalizedAnswer)) {
                throw new InvalidZeloPlanException("Resposta inválida para esta pergunta.");
            }

            await _answerRepository.AddAsync(new ZeloPlanAnswer {
                Id       = Guid.NewGuid(),
                SessionId = sessionId,
                Key      = currentQuestion.Key,
                Question = currentQuestion.Text,
                Answer   = normalizedAnswer,
                Order    = answered.Count
            }, cancellationToken);

            var updatedAnswered = await _answerRepository.GetBySessionIdAsync(sessionId, cancellationToken);

            var nextQuestion = ZeloPlanQuestionFlow.GetNextQuestion(updatedAnswered);
            if (nextQuestion is not null) {
                return new ZeloPlanSessionState(session.Id, session.Status, nextQuestion, updatedAnswered.Count);
            }

            return await GenerateAsync(session, updatedAnswered, cancellationToken);
        }

        public async Task<ZeloPlanSessionState> RetryGenerationAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) {
            var session = await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            if (session.Status != ZeloPlanSessionStatus.Coletando) {
                throw new InvalidZeloPlanException("Este formulário já foi concluído.");
            }

            var answered = await _answerRepository.GetBySessionIdAsync(sessionId, cancellationToken);
            if (ZeloPlanQuestionFlow.GetNextQuestion(answered) is not null) {
                throw new InvalidZeloPlanException("Ainda há perguntas pendentes.");
            }

            return await GenerateAsync(session, answered, cancellationToken);
        }

        public async Task<ZeloPlanPreview> GetPreviewAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) {
            var session = await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            if (session.Status != ZeloPlanSessionStatus.PlanoGerado || session.GeneratedPlanJson is null) {
                throw new InvalidZeloPlanException("O plano ainda não foi gerado.");
            }

            var plan = JsonSerializer.Deserialize<GeneratedPlan>(session.GeneratedPlanJson, JsonOptions)
                       ?? throw new InvalidZeloPlanException("O plano ainda não foi gerado.");
            return new ZeloPlanPreview(session.Id, plan, session.Status);
        }

        public async Task ApplyAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) {
            var session = await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            if (session.Status != ZeloPlanSessionStatus.PlanoGerado || session.GeneratedPlanJson is null) {
                throw new InvalidZeloPlanException("Não há plano gerado pra aplicar.");
            }

            var plan = JsonSerializer.Deserialize<GeneratedPlan>(session.GeneratedPlanJson, JsonOptions)
                       ?? throw new InvalidZeloPlanException("Não há plano gerado pra aplicar.");

            // Treino: mesma cópia que WorkoutTemplateService.ApplyAsync faz ao aplicar um template
            var days = plan.WorkoutDays
                .Select(d => new WorkoutPlanDay { DayOfWeek = d.DayOfWeek, Label = d.Label })
                .ToList();
            await _workoutPlanDayRepository.UpsertManyAsync(userId, days, cancellationToken);

            var exercises = plan.WorkoutDays
                .SelectMany(day => day.Exercises.Select(e => new WorkoutPlanExercise {
                    Id           = Guid.NewGuid(),
                    UserId       = userId,
                    DayOfWeek    = day.DayOfWeek,
                    Type         = e.Type,
                    ExerciseName = e.ExerciseName,
                    Sets         = e.Sets,
                    Reps         = e.Reps,
                    Order        = e.Order
                }))
                .ToList();
            await _workoutPlanExerciseRepository.ReplaceAllForUserAsync(userId, exercises, cancellationToken);

            // Nutrição: mesmo padrão, agora com ReplaceAllForUserAsync (novo, criado pra este fluxo)
            var items = plan.NutritionDays
                .SelectMany(day => day.Items.Select(i => new NutritionPlanItem {
                    Id        = Guid.NewGuid(),
                    UserId    = userId,
                    DayOfWeek = day.DayOfWeek,
                    MealType  = i.MealType,
                    ItemName  = i.ItemName,
                    Quantity  = i.Quantity
                }))
                .ToList();
            await _nutritionPlanItemRepository.ReplaceAllForUserAsync(userId, items, cancellationToken);

            session.Status    = ZeloPlanSessionStatus.Aplicada;
            session.AppliedAt = _clock.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        public async Task DiscardAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) {
            var session = await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            if (session.Status != ZeloPlanSessionStatus.PlanoGerado) {
                throw new InvalidZeloPlanException("Não há plano gerado pra descartar.");
            }

            session.Status = ZeloPlanSessionStatus.Descartada;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        private async Task<ZeloPlanSessionState> GenerateAsync(ZeloPlanSession session, IReadOnlyList<ZeloPlanAnswer> answers, CancellationToken cancellationToken) {
            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken)
                       ?? throw new NotFoundException("Usuário não encontrado.");
            var today = _clock.TodayIn(user.Timezone);

            var log = await _queryLogRepository.GetByUserAndDateAsync(user.Id, today, cancellationToken);
            if (log is not null && log.Count >= DailyLimit) {
                throw new ZeloPlanRateLimitExceededException();
            }

            var context = await _contextBuilder.BuildAsync(user.Id, cancellationToken);
            var result  = await _assistant.GeneratePlanAsync(context, answers, cancellationToken);

            if (!result.Success || result.Plan is null) {
                // não consome cota em falha, mesma regra do Zelo geral; sessão permanece Coletando
                // sem próxima pergunta, o front oferece "tentar de novo" via RetryGenerationAsync
                return new ZeloPlanSessionState(session.Id, session.Status, null, answers.Count, result.Message);
            }

            session.GeneratedPlanJson = JsonSerializer.Serialize(result.Plan, JsonOptions);
            session.Status            = ZeloPlanSessionStatus.PlanoGerado;
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            await IncrementQuotaAsync(user.Id, today, log, cancellationToken);

            return new ZeloPlanSessionState(session.Id, session.Status, null, answers.Count);
        }

        public async Task<IReadOnlyList<ZeloPlanChatMessage>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) {
            await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            var messages = await _messageRepository.GetBySessionIdAsync(sessionId, cancellationToken);
            return messages.Select(m => new ZeloPlanChatMessage(m.Id, m.Role, m.Content, m.CreatedAt)).ToList();
        }

        public async Task<ZeloPlanChatResult> SendMessageAsync(Guid userId, Guid sessionId, string message, CancellationToken cancellationToken = default) {
            var session = await GetOwnedActiveSessionAsync(userId, sessionId, cancellationToken);
            var planReady = session.Status is ZeloPlanSessionStatus.PlanoGerado or ZeloPlanSessionStatus.Aplicada;
            if (!planReady || session.GeneratedPlanJson is null) {
                throw new InvalidZeloPlanException("O chat livre só fica disponível depois que o plano é gerado.");
            }

            var normalizedMessage = message?.Trim();
            if (string.IsNullOrEmpty(normalizedMessage)) {
                throw new InvalidZeloPlanException("Informe uma mensagem.");
            }
            if (normalizedMessage.Length > 300) {
                throw new InvalidZeloPlanException("A mensagem deve ter no máximo 300 caracteres.");
            }

            var user  = await _userRepository.GetByIdAsync(userId, cancellationToken)
                        ?? throw new NotFoundException("Usuário não encontrado.");
            var today = _clock.TodayIn(user.Timezone);

            var log = await _queryLogRepository.GetByUserAndDateAsync(userId, today, cancellationToken);
            if (log is not null && log.Count >= DailyLimit) {
                throw new ZeloPlanRateLimitExceededException();
            }

            var plan = JsonSerializer.Deserialize<GeneratedPlan>(session.GeneratedPlanJson, JsonOptions)
                       ?? throw new InvalidZeloPlanException("O chat livre só fica disponível depois que o plano é gerado.");
            var history = await _messageRepository.GetBySessionIdAsync(sessionId, cancellationToken);

            // salva a pergunta do usuário mesmo que o Zelo não consiga responder — é entrada real dele, não deveria se perder
            await _messageRepository.AddAsync(new ZeloPlanMessage {
                Id = Guid.NewGuid(), SessionId = sessionId, Role = ZeloPlanMessageRole.Usuario,
                Content = normalizedMessage, CreatedAt = _clock.UtcNow
            }, cancellationToken);

            var context = await _contextBuilder.BuildAsync(userId, cancellationToken);
            var result  = await _assistant.ContinueChatAsync(context, plan, history, normalizedMessage, cancellationToken);

            if (!result.Success) {
                // não consome cota em falha, mesma regra da geração de plano
                return new ZeloPlanChatResult(null, result.Message);
            }

            var reply = new ZeloPlanMessage {
                Id = Guid.NewGuid(), SessionId = sessionId, Role = ZeloPlanMessageRole.Zelo,
                Content = result.Message, CreatedAt = _clock.UtcNow
            };
            await _messageRepository.AddAsync(reply, cancellationToken);
            await IncrementQuotaAsync(userId, today, log, cancellationToken);

            return new ZeloPlanChatResult(new ZeloPlanChatMessage(reply.Id, reply.Role, reply.Content, reply.CreatedAt), null);
        }

        private async Task IncrementQuotaAsync(Guid userId, DateOnly today, ZeloPlanQueryLog? existing, CancellationToken cancellationToken) {
            if (existing is null) {
                await _queryLogRepository.UpsertAsync(new ZeloPlanQueryLog {
                    Id = Guid.NewGuid(), UserId = userId, Date = today, Count = 1
                }, cancellationToken);
                return;
            }

            existing.Count++;
            await _queryLogRepository.UpsertAsync(existing, cancellationToken);
        }

        private async Task<ZeloPlanSessionState> BuildStateAsync(ZeloPlanSession session, CancellationToken cancellationToken) {
            var answered = await _answerRepository.GetBySessionIdAsync(session.Id, cancellationToken);

            if (session.Status == ZeloPlanSessionStatus.PlanoGerado) {
                return new ZeloPlanSessionState(session.Id, session.Status, null, answered.Count);
            }

            var next = ZeloPlanQuestionFlow.GetNextQuestion(answered);
            return new ZeloPlanSessionState(session.Id, session.Status, next, answered.Count);
        }

        private async Task<ZeloPlanSession> GetOwnedActiveSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken) {
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session is null || session.UserId != userId) {
                throw new NotFoundException($"Sessão '{sessionId}' não encontrada.");
            }
            if (session.ExpiresAt <= _clock.UtcNow) {
                throw new NotFoundException($"Sessão '{sessionId}' expirou.");
            }
            return session;
        }
    }
}
