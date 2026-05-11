using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using dotnet_user.Data;
using Microsoft.EntityFrameworkCore;

namespace dotnet_user.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T>
        where T : class
    {
        private readonly DataContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(DataContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public IQueryable<T> Query(bool asNoTracking = true)
        {
            return asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
        }

        public async Task<T> GetByIdAsync(params object[] keyValues)
        {
            return await _dbSet.FindAsync(keyValues);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }
    }
}
