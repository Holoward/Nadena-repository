using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Models;

namespace Persistence.Repository;

public class VolunteerRepository : NadenaRepositoryBase<Volunteer>, IVolunteerRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private new NadenaIdentityDbContext DbContext => (NadenaIdentityDbContext)base.DbContext;

    public VolunteerRepository(NadenaIdentityDbContext dbContext, UserManager<ApplicationUser> userManager) : base(dbContext)
    {
        _userManager = userManager;
    }

    public async Task<Volunteer> GetByIdAsync(int id)
    {
        return await DbContext.Volunteers.FindAsync(id);
    }

    public async Task<IEnumerable<Volunteer>> GetAllAsync()
    {
        return await DbContext.Volunteers.ToListAsync();
    }

    public async Task<Volunteer> GetByUserIdAsync(string userId)
    {
        return await DbContext.Volunteers.FirstOrDefaultAsync(v => v.UserId == userId);
    }

    public async Task AddAsync(Volunteer volunteer)
    {
        await DbContext.Volunteers.AddAsync(volunteer);
        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Volunteer volunteer)
    {
        DbContext.Volunteers.Update(volunteer);
        await DbContext.SaveChangesAsync();
    }

    public async Task<List<Volunteer>> GetVolunteersByIds(List<int> ids)
    {
        return await DbContext.Volunteers
            .Where(v => ids.Contains(v.Id))
            .ToListAsync();
    }

    public async Task<(string Email, string FullName)?> GetUserInfoByVolunteerIdAsync(int volunteerId)
    {
        var volunteer = await DbContext.Volunteers.FindAsync(volunteerId);
        if (volunteer == null) return null;

        var user = await _userManager.FindByIdAsync(volunteer.UserId);
        if (user == null) return null;

        return (user.Email ?? string.Empty, user.FullName);
    }

    public async Task<IEnumerable<Volunteer>> GetByIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Select(g => g.ToString()).ToList();
        return await DbContext.Volunteers.Where(v => ids.Contains(v.UserId)).ToListAsync();
    }
}
