using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace TrendClothing.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        // Sync methods (existing — kept for backward compat)
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
        void Remove(int id);
        void RemoveRange(IEnumerable<T> entities);
        T Get(int id);
        IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null,
            string IncludeProperties = null);
        T FirstOrDefault(Expression<Func<T, bool>> filter = null, string IncludeProperties = null);

        // ✅ NEW: Async versions — use these in controllers to avoid thread-pool blocking
        Task AddAsync(T entity);
        Task<T> GetAsync(int id);
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null,
            string IncludeProperties = null);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter = null, string IncludeProperties = null);
    }
}