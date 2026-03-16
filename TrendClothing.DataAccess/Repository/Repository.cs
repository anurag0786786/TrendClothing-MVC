using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;

namespace TrendClothing.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            dbSet = _context.Set<T>();
        }

        // ─────────────────── SYNC (kept for backward compat) ───────────────────

        public void Add(T entity) => dbSet.Add(entity);

        public T Get(int id) => dbSet.Find(id);

        public IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null,
            string IncludeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            if (IncludeProperties != null)
                foreach (var prop in IncludeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            if (orderby != null) return orderby(query).ToList();
            return query.ToList();
        }

        public T FirstOrDefault(Expression<Func<T, bool>> filter = null, string IncludeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            if (IncludeProperties != null)
                foreach (var prop in IncludeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            return query.FirstOrDefault();
        }

        public void Remove(T entity) => dbSet.Remove(entity);

        public void Remove(int id) => dbSet.Remove(Get(id));

        public void RemoveRange(IEnumerable<T> entities) => dbSet.RemoveRange(entities);

        public void Update(T entity)
        {
            // ✅ FIX: Removed _context.ChangeTracker.Clear() — it was cancelling
            // other pending changes in the same request (concurrency bug).
            // EF Core handles update tracking automatically — just call Update().
            dbSet.Update(entity);
        }

        // ─────────────────── ASYNC (new) ───────────────────

        public async Task AddAsync(T entity) => await dbSet.AddAsync(entity);

        public async Task<T> GetAsync(int id) => await dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null,
            string IncludeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            if (IncludeProperties != null)
                foreach (var prop in IncludeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            if (orderby != null) return await orderby(query).ToListAsync();
            return await query.ToListAsync();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter = null, string IncludeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            if (IncludeProperties != null)
                foreach (var prop in IncludeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            return await query.FirstOrDefaultAsync();
        }
    }
}