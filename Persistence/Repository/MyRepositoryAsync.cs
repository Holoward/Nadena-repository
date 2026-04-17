using Application.Interfaces;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public class MyRepositoryAsync<T> : RepositoryBase<T>, IRepositoryAsync<T> where T : class
{
    public readonly ApplicationDbContext DbContext;
    public MyRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
    {
        DbContext = dbContext;
    }
}