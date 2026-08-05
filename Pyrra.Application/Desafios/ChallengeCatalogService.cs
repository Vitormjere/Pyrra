using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Application.Common.Exceptions;
using Pyrra.Application.Common.Interfaces;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    public class ChallengeCatalogService : IChallengeCatalogService {
        private readonly IChallengeCategoryRepository _categoryRepository;
        private readonly IChallengeRepository         _challengeRepository;
        private readonly IAdminAuthorizationService   _adminAuth;
        private readonly IClockService                _clock;

        public ChallengeCatalogService(
            IChallengeCategoryRepository categoryRepository,
            IChallengeRepository         challengeRepository,
            IAdminAuthorizationService   adminAuth,
            IClockService                clock) {
            _categoryRepository  = categoryRepository;
            _challengeRepository = challengeRepository;
            _adminAuth           = adminAuth;
            _clock               = clock;
        }

        public async Task<IReadOnlyList<ChallengeCategory>> GetCategoriesAsync(Guid adminUserId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);
            return await _categoryRepository.GetAllAsync(cancellationToken);
        }

        public async Task<ChallengeCategory> CreateCategoryAsync(
            Guid adminUserId, string name, string? description, string icon, ChallengeCategoryColor color,
            CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var (normalizedName, normalizedDescription, normalizedIcon) = ValidateCategoryInput(name, description, icon, color);

            var now = _clock.UtcNow;
            var category = new ChallengeCategory {
                Id          = Guid.NewGuid(),
                Name        = normalizedName,
                Description = normalizedDescription,
                Icon        = normalizedIcon,
                Color       = color,
                CreatedAt   = now,
                UpdatedAt   = now
            };

            await _categoryRepository.AddAsync(category, cancellationToken);
            return category;
        }

        public async Task<ChallengeCategory> UpdateCategoryAsync(
            Guid adminUserId, Guid categoryId, string name, string? description, string icon, ChallengeCategoryColor color,
            CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            var (normalizedName, normalizedDescription, normalizedIcon) = ValidateCategoryInput(name, description, icon, color);

            category.Name        = normalizedName;
            category.Description = normalizedDescription;
            category.Icon        = normalizedIcon;
            category.Color       = color;
            category.UpdatedAt   = _clock.UtcNow;

            await _categoryRepository.UpdateAsync(category, cancellationToken);
            return category;
        }

        public async Task DeleteCategoryAsync(Guid adminUserId, Guid categoryId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            // impede apagar com desafios vinculados para evitar referências quebradas
            if (await _challengeRepository.AnyByCategoryAsync(categoryId, cancellationToken)) {
                throw new ChallengeCategoryInUseException();
            }

            await _categoryRepository.DeleteAsync(category, cancellationToken);
        }

        public async Task<IReadOnlyList<Challenge>> GetChallengesAsync(Guid adminUserId, Guid? categoryId = null, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);
            return categoryId.HasValue
                ? await _challengeRepository.GetByCategoryAsync(categoryId.Value, cancellationToken)
                : await _challengeRepository.GetAllAsync(cancellationToken);
        }

        public async Task<Challenge> CreateChallengeAsync(
            Guid adminUserId, Guid categoryId, string title, string? description, int points, DateTime? deadline,
            CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            var normalizedTitle = ValidateChallengeInput(title, points);
            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            var now = _clock.UtcNow;
            var challenge = new Challenge {
                Id          = Guid.NewGuid(),
                CategoryId  = categoryId,
                Title       = normalizedTitle,
                Description = normalizedDescription,
                Points      = points,
                Deadline    = deadline,
                CreatedAt   = now,
                UpdatedAt   = now
            };

            await _challengeRepository.AddAsync(challenge, cancellationToken);
            return challenge;
        }

        public async Task<Challenge> UpdateChallengeAsync(
            Guid adminUserId, Guid challengeId, Guid categoryId, string title, string? description, int points, DateTime? deadline,
            CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null) {
                throw new NotFoundException($"Categoria '{categoryId}' não encontrada.");
            }

            var normalizedTitle = ValidateChallengeInput(title, points);
            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            challenge.CategoryId  = categoryId;
            challenge.Title       = normalizedTitle;
            challenge.Description = normalizedDescription;
            challenge.Points      = points;
            challenge.Deadline    = deadline;
            challenge.UpdatedAt   = _clock.UtcNow;

            await _challengeRepository.UpdateAsync(challenge, cancellationToken);
            return challenge;
        }

        public async Task DeleteChallengeAsync(Guid adminUserId, Guid challengeId, CancellationToken cancellationToken = default) {
            await _adminAuth.EnsureAdminAsync(adminUserId, cancellationToken);

            var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
            if (challenge is null) {
                throw new NotFoundException($"Desafio '{challengeId}' não encontrado.");
            }

            await _challengeRepository.DeleteAsync(challenge, cancellationToken);
        }

        private static (string Name, string? Description, string Icon) ValidateCategoryInput(string name, string? description, string icon, ChallengeCategoryColor color) {
            var normalizedName = name?.Trim();
            if (string.IsNullOrEmpty(normalizedName)) {
                throw new InvalidChallengeException("O nome da categoria é obrigatório.");
            }

            var normalizedIcon = icon?.Trim();
            if (string.IsNullOrEmpty(normalizedIcon)) {
                throw new InvalidChallengeException("O ícone da categoria é obrigatório.");
            }

            if (!Enum.IsDefined(color)) {
                throw new InvalidChallengeException($"Cor '{color}' não é válida.");
            }

            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            return (normalizedName, normalizedDescription, normalizedIcon);
        }

        private static string ValidateChallengeInput(string title, int points) {
            var normalizedTitle = title?.Trim();
            if (string.IsNullOrEmpty(normalizedTitle)) {
                throw new InvalidChallengeException("O título do desafio é obrigatório.");
            }

            if (points <= 0) {
                throw new InvalidChallengeException("A pontuação do desafio precisa ser maior que zero.");
            }

            return normalizedTitle;
        }
    }
}
