using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Achievements;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Tests.Desafios {
    // não desbloqueia nada de verdade, só registra quem foi checado — a lógica de desbloqueio tem cobertura própria em AchievementCheckerServiceTests
    internal sealed class FakeAchievementCheckerService : IAchievementCheckerService {
        public readonly List<Guid> ChallengeCompletedChecks = new();

        public Task CheckStreakMilestonesAsync(Guid userId, IReadOnlyList<int> milestonesReached, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CheckChallengeCompletedAsync(Guid userId, CancellationToken cancellationToken = default) {
            ChallengeCompletedChecks.Add(userId);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeChallengeCategoryRepository : IChallengeCategoryRepository {
        public readonly List<ChallengeCategory> Categories = new();

        public Task<IReadOnlyList<ChallengeCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeCategory>>(Categories.OrderBy(c => c.Name).ToList());

        public Task<ChallengeCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));

        public Task AddAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChallengeCategory category, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(ChallengeCategory category, CancellationToken cancellationToken = default) {
            Categories.RemoveAll(c => c.Id == category.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeChallengeRepository : IChallengeRepository {
        public readonly List<Challenge> Challenges = new();

        public Task<IReadOnlyList<Challenge>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Challenge>>(Challenges.OrderBy(c => c.Title).ToList());

        public Task<IReadOnlyList<Challenge>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Challenge>>(Challenges.Where(c => c.CategoryId == categoryId).OrderBy(c => c.Title).ToList());

        public Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Challenges.FirstOrDefault(c => c.Id == id));

        public Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Challenges.Any(c => c.CategoryId == categoryId));

        public Task AddAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            Challenges.Add(challenge);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Challenge challenge, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Challenge challenge, CancellationToken cancellationToken = default) {
            Challenges.RemoveAll(c => c.Id == challenge.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTeamActiveCategoryRepository : ITeamActiveCategoryRepository {
        public readonly List<TeamActiveCategory> Activations = new();

        public Task<IReadOnlyList<TeamActiveCategory>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TeamActiveCategory>>(Activations.Where(a => a.TeamId == teamId).ToList());

        public Task<TeamActiveCategory?> GetAsync(Guid teamId, Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Activations.FirstOrDefault(a => a.TeamId == teamId && a.CategoryId == categoryId));

        public Task AddAsync(TeamActiveCategory activation, CancellationToken cancellationToken = default) {
            Activations.Add(activation);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(TeamActiveCategory activation, CancellationToken cancellationToken = default) {
            Activations.RemoveAll(a => a.Id == activation.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeChallengeSubmissionRepository : IChallengeSubmissionRepository {
        public readonly List<ChallengeSubmission> Submissions = new();

        public Task<ChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Submissions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<ChallengeSubmission>> GetForUserAndTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeSubmission>>(
                Submissions.Where(s => s.UserId == userId && s.TeamId == teamId).ToList());

        public Task<IReadOnlyList<ChallengeSubmission>> GetForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeSubmission>>(
                Submissions.Where(s => s.TeamId == teamId).ToList());

        public Task<ChallengeSubmission?> GetActiveForUserChallengeAsync(Guid userId, Guid challengeId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Submissions.FirstOrDefault(s =>
                s.UserId == userId && s.ChallengeId == challengeId && s.TeamId == teamId &&
                s.Status != ChallengeSubmissionStatus.Recusado));

        public Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeSubmission>>(
                Submissions.Where(s => s.TeamId == teamId && s.Status == ChallengeSubmissionStatus.Pendente).ToList());

        public Task<IReadOnlyList<ChallengeSubmission>> GetPendingForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeSubmission>>(
                Submissions.Where(s => s.TournamentId == tournamentId && s.Status == ChallengeSubmissionStatus.Pendente).ToList());

        public Task<IReadOnlyList<ChallengeSubmission>> GetApprovedForTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChallengeSubmission>>(
                Submissions.Where(s => s.TournamentId == tournamentId && s.Status == ChallengeSubmissionStatus.Aprovado).ToList());

        public Task<int> CountApprovedByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Submissions.Count(s => s.UserId == userId && s.Status == ChallengeSubmissionStatus.Aprovado));

        public Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default) {
            Submissions.Add(submission);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    internal sealed class FakeTeamMemberScoreRepository : ITeamMemberScoreRepository {
        public readonly List<TeamMemberScore> Scores = new();

        public Task<TeamMemberScore?> GetAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Scores.FirstOrDefault(s => s.TeamId == teamId && s.UserId == userId));

        public Task<IReadOnlyList<TeamMemberScore>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TeamMemberScore>>(Scores.Where(s => s.TeamId == teamId).ToList());

        public Task AddAsync(TeamMemberScore score, CancellationToken cancellationToken = default) {
            Scores.Add(score);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TeamMemberScore score, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    internal sealed class FakeTournamentChallengeRepository : ITournamentChallengeRepository {
        public readonly List<TournamentChallenge> Links = new();

        public Task<IReadOnlyList<TournamentChallenge>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentChallenge>>(Links.Where(l => l.TournamentId == tournamentId).ToList());

        public Task<TournamentChallenge?> GetAsync(Guid tournamentId, Guid challengeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Links.FirstOrDefault(l => l.TournamentId == tournamentId && l.ChallengeId == challengeId));

        public Task AddAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default) {
            Links.Add(tournamentChallenge);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAsync(TournamentChallenge tournamentChallenge, CancellationToken cancellationToken = default) {
            Links.RemoveAll(l => l.Id == tournamentChallenge.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTournamentOwnChallengeRepository : ITournamentOwnChallengeRepository {
        public readonly List<TournamentOwnChallenge> Challenges = new();

        public Task<IReadOnlyList<TournamentOwnChallenge>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TournamentOwnChallenge>>(
                Challenges.Where(c => c.TournamentId == tournamentId).OrderBy(c => c.Title).ToList());

        public Task<TournamentOwnChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Challenges.FirstOrDefault(c => c.Id == id));

        public Task AddAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default) {
            Challenges.Add(challenge);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(TournamentOwnChallenge challenge, CancellationToken cancellationToken = default) {
            Challenges.RemoveAll(c => c.Id == challenge.Id);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeChallengeSubmissionStorageService : IChallengeSubmissionStorageService {
        public int UploadCallCount { get; private set; }
        private readonly Dictionary<Guid, (byte[] Bytes, string ContentType)> _blobs = new();

        public Task<string> UploadAsync(Guid submissionId, Stream content, string contentType, CancellationToken cancellationToken = default) {
            UploadCallCount++;

            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            _blobs[submissionId] = (buffer.ToArray(), contentType);

            return Task.FromResult($"https://fake.blob.core.windows.net/challenge-submissions/{submissionId:N}");
        }

        public Task<(Stream Content, string ContentType)> DownloadAsync(Guid submissionId, CancellationToken cancellationToken = default) {
            if (!_blobs.TryGetValue(submissionId, out var blob)) {
                throw new NotFoundException("Foto da submissão não encontrada.");
            }

            return Task.FromResult<(Stream, string)>((new MemoryStream(blob.Bytes), blob.ContentType));
        }
    }
}
