using Microsoft.EntityFrameworkCore;
using TicketFlow.BuildingBlocks.Domain.Models;

namespace TicketFlow.BuildingBlocks.Infrastructure.Persistence.PostgreSql;

public class PostgresRepository<TId, TEntity>(DbContext dbContext)
    where TId : struct
    where TEntity : Entity<TId>
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        var entityEntry = await _dbSet.AddAsync(entity, cancellationToken);
        
        return entityEntry.Entity;
    }

    public async Task<IEnumerable<TEntity>> CreateRangeAsync(
        TEntity[] entities,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (entities.Length == 0)
        {
            return [];
        }
        
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        
        return [..entities];
    }
    
    public TEntity Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        var entityEntry = _dbSet.Update(entity);
        
        return entityEntry.Entity;
    }

    // TODO: Bulk implementation
    public async Task UpdateRange(TEntity[] entities, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    

    public TEntity Delete(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        var entityEntry = _dbSet.Remove(entity);
        
        return entityEntry.Entity;
    }

    // TODO: Bulk implementation
    public async Task DeleteRangeAsync(TEntity[] entities, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    

    public IQueryable<TEntity> AsQuery()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<TEntity?> FindAsync(TId id, CancellationToken cancellationToken)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }
    
    
    
}