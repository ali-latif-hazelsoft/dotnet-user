using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace dotnet_user.Repositories
{
    public interface IGenericRepository<T>
        where T : class
    {
        IQueryable<T> Query(bool asNoTracking = true);
        Task<T> GetByIdAsync(params object[] keyValues);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
