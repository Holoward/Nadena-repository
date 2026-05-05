using Domain.Entities;

namespace Application.Interfaces;

public interface IDataPoolRepository
{
    Task<DataPool?> GetByIdAsync(int id);
    Task<IEnumerable<DataPool>> GetAllAsync();
    Task<IEnumerable<DataPool>> GetAllActiveAsync();
    Task AddAsync(DataPool pool);
    Task UpdateAsync(DataPool pool);

    /// <summary>
    /// Recounts the rows in <paramref name="sourceTable"/> and persists the
    /// live count back to every DataPool whose SourceTable matches.
    /// Call this after any bulk delete (e.g. revocation) to keep
    /// ApproximateRecordCount accurate.
    /// </summary>
    Task RecalculateApproximateCountAsync(string sourceTable, CancellationToken ct = default);
}
