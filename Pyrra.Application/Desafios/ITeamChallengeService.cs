using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pyrra.Domain.Desafios;

namespace Pyrra.Application.Desafios {
    public interface ITeamChallengeService {
        // Todas as categorias do catálogo com o flag de ativação do time — só o dono. Lança
        // NotFoundException se o time não existir ou quem chama não for o dono (não revela gestão
        // de time alheio, mesmo critério do TeamService).
        Task<IReadOnlyList<TeamCategoryStatus>> GetCategoriesForTeamAsync(Guid ownerId, Guid teamId, CancellationToken cancellationToken = default);

        // Idempotente: ativar uma categoria já ativa não é erro. Lança NotFoundException se a
        // categoria não existir, o time não existir, ou quem chama não for o dono.
        Task ActivateCategoryAsync(Guid ownerId, Guid teamId, Guid categoryId, CancellationToken cancellationToken = default);

        // Idempotente: desativar uma categoria que já não está ativa não é erro.
        Task DeactivateCategoryAsync(Guid ownerId, Guid teamId, Guid categoryId, CancellationToken cancellationToken = default);

        // Desafios das categorias ativas do time, com deadline expirado já filtrado — qualquer
        // membro (dono ou não). Lança NotFoundException se o time não existir ou quem chama não
        // for dono nem membro.
        Task<IReadOnlyList<AvailableChallenge>> GetAvailableChallengesAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

        // Envia uma foto como prova de conclusão — precisa ser membro do time, a categoria do
        // desafio estar ativa nesse time, o desafio não ter prazo expirado, e não ter uma
        // submissão ativa (Pendente ou Aprovado) em aberto pra esse desafio nesse time. Valida
        // tipo (jpg/png/webp) e tamanho (3MB) antes de subir pro Blob Storage. Lança
        // InvalidChallengeException pra qualquer uma dessas condições; NotFoundException se o
        // time/desafio não existir ou quem chama não for dono nem membro.
        Task<ChallengeSubmission> SubmitChallengeProofAsync(
            Guid userId, Guid teamId, Guid challengeId, Stream content, string contentType, long contentLength,
            CancellationToken cancellationToken = default);

        // Submissões pendentes do time, mais antiga primeiro. Quem pode ver: o dono do TIME, salvo
        // se o time estiver Aprovado num torneio no momento da chamada — nesse caso, é o dono do
        // TORNEIO quem vê (resolvido a cada chamada, não fixado em nenhum momento anterior).
        Task<IReadOnlyList<PendingSubmission>> GetPendingSubmissionsAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken = default);

        // Aprova: soma os pontos do desafio ao TotalPoints do time SEMPRE, ao placar INDIVIDUAL de
        // quem enviou dentro desse time (TeamMemberScore — criado na primeira aprovação dessa
        // pessoa nesse time) SEMPRE, e também ao Score da entrada do time num torneio
        // (TournamentTeam) SE o time estiver Aprovado num no momento da aprovação. Quem pode
        // aprovar: mesmo critério de GetPendingSubmissionsAsync (dono do time, ou dono do torneio
        // se o time estiver num). Só se a submissão ainda estiver Pendente (InvalidChallengeException
        // caso já avaliada). Quem enviou a prova não pode aprovar a própria submissão
        // (InvalidChallengeException), mesmo sendo o único aprovador possível — sem exceção
        // automática.
        Task ApproveSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default);

        // Recusa: não soma pontos em lugar nenhum. Mesmas guardas de existência/estado/quem-pode
        // de ApproveSubmissionAsync, mas SEM a trava de auto-avaliação — quem aprova pode recusar
        // a própria submissão (não há ganho nenhum em bloquear isso).
        Task RejectSubmissionAsync(Guid callerId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default);

        // Bytes e Content-Type da foto de uma submissão — container privado, servido só por aqui.
        // Qualquer membro do time (dono ou não) pode ver, mesmo critério de GetAvailableChallengesAsync.
        // Lança NotFoundException se o time/submissão não existir, a submissão não pertencer a
        // esse time, ou quem chama não for dono nem membro.
        Task<(Stream Content, string ContentType)> GetSubmissionPhotoAsync(Guid userId, Guid teamId, Guid submissionId, CancellationToken cancellationToken = default);

        // Ranking de TODOS os membros do time (dono + membros) por placar INDIVIDUAL — não é o
        // TotalPoints coletivo do time. Ordenado por pontos desc, nome como desempate; quem nunca
        // teve submissão aprovada aparece com 0. Qualquer membro do time (dono ou não) pode ver,
        // mesmo critério de GetAvailableChallengesAsync. Lança NotFoundException se o time não
        // existir ou quem chama não for dono nem membro.
        Task<IReadOnlyList<TeamMemberRanking>> GetTeamRankingAsync(Guid callerId, Guid teamId, CancellationToken cancellationToken = default);
    }
}
