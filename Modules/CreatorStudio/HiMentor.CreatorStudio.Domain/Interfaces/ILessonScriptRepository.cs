using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;

namespace HiMentor.CreatorStudio.Domain.Interfaces;

public interface ILessonScriptRepository : IRepository<LessonScript>
{
    /// <summary>Tela "Meus Roteiros" — todos os roteiros do criador, mais recentes primeiro.</summary>
    Task<IEnumerable<LessonScript>> ListByCreatorAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>Progresso do Clone do Professor — quantos roteiros já foram marcados como gravados.</summary>
    Task<int> CountRecordedByCreatorAsync(Guid creatorId, CancellationToken ct = default);
}
