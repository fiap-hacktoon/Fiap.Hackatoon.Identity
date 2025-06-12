using Fiap.Hackatoon.Identity.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> GetAll(FindOptions? findOptions = null);
        Task<TEntity> FindOne(Expression<Func<TEntity, bool>> predicate, FindOptions? findOptions = null);
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate, FindOptions? findOptions = null);
        Task Add(TEntity entity);
        Task AddMany(IEnumerable<TEntity> entities);
        Task Update(TEntity entity);
        Task Delete(TEntity entity);
        Task DeleteMany(Expression<Func<TEntity, bool>> predicate);
    }
}
