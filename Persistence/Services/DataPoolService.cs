using Microsoft.EntityFrameworkCore;
using Persistence.Context; // adjust if needed
using Domain.Entities;

public class DataPoolService
{
    private readonly ApplicationDbContext _context;

    public DataPoolService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<long> GetApproximateRecordCountAsync(int dataPoolId)
    {
        var dataPool = await _context.DataPools.FindAsync(dataPoolId);

        if (dataPool == null)
            throw new Exception("DataPool not found");

        return dataPool.SourceTable switch
        {
            "YoutubeComments" => await _context.YoutubeComments.LongCountAsync(),
            "SpotifyListeningRecords" => await _context.SpotifyListeningRecords.LongCountAsync(),
            "NetflixViewingRecords" => await _context.NetflixViewingRecords.LongCountAsync(),
            _ => 0
        };
    }
}
