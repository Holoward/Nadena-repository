using Application.Interfaces;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repository;

public abstract class NadenaRepositoryBase<T> : RepositoryBase<T>, IRepositoryAsync<T> where T : class
{
    public readonly DbContext DbContext;
    public NadenaRepositoryBase(DbContext dbContext) : base(dbContext)
    {
        DbContext = dbContext;
    }
}

public class MyRepositoryAsync<T> : NadenaRepositoryBase<T> where T : class
{
    public MyRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}