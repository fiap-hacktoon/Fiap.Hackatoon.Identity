using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Fiap.Hackatoon.Identity.Infrastructure.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly IdentityContext _identityContext;
        public Repository(IdentityContext identityContext)
        {
            _identityContext = identityContext;
        }
        public async Task Add(TEntity entity)
        {
            _identityContext.Set<TEntity>().Add(entity);
            await _identityContext.SaveChangesAsync();
        }
        public async Task AddMany(IEnumerable<TEntity> entities)
        {
            _identityContext.Set<TEntity>().AddRange(entities);
           await _identityContext.SaveChangesAsync();
        }
        public async Task Delete(TEntity entity)
        {
            _identityContext.Set<TEntity>().Remove(entity);
            await _identityContext.SaveChangesAsync();
        }
        public async Task DeleteMany(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = Find(predicate);
            _identityContext.Set<TEntity>().RemoveRange(entities);
            await _identityContext.SaveChangesAsync();
        }
        public async Task<TEntity> FindOne(Expression<Func<TEntity, bool>> predicate, FindOptions? findOptions = null)
        {
            return await Get(findOptions).FirstOrDefaultAsync(predicate)!;
        }
        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate, FindOptions? findOptions = null)
        {
            return Get(findOptions).Where(predicate);
        }
        public IQueryable<TEntity> GetAll(FindOptions? findOptions = null)
        {
            return Get(findOptions);
        }
        public async Task Update(TEntity entity)
        {
            _identityContext.Set<TEntity>().Update(entity);
            await _identityContext.SaveChangesAsync();
        }      
        private DbSet<TEntity> Get(FindOptions? findOptions = null)
        {
            findOptions ??= new FindOptions();
            var entity = _identityContext.Set<TEntity>();
            if (findOptions.IsAsNoTracking && findOptions.IsIgnoreAutoIncludes)            
                entity.IgnoreAutoIncludes().AsNoTracking();            
            else if (findOptions.IsIgnoreAutoIncludes)            
                entity.IgnoreAutoIncludes();            
            else if (findOptions.IsAsNoTracking)                
                entity.AsNoTracking();
            
            return entity;
        }
    }
}
