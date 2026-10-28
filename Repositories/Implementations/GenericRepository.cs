using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations
{
    public class GenericRepository<T>(QuanLyHocBong_UNETI2_TI17A3HNContext context) : IGenericRepository<T>
        where T : class
    {
        protected readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context = context;
        protected readonly DbSet<T> _dbSet = context.Set<T>();

        public IQueryable<T> Query(bool asNoTracking = true) =>
            asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

        public Task<List<T>> GetAllAsync(CancellationToken ct = default) =>
            _dbSet.AsNoTracking().ToListAsync(ct);

        public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default) =>
            await _dbSet.FindAsync(new[] { id }, ct);

        public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            _dbSet.AnyAsync(predicate, ct);

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
            predicate == null ? _dbSet.CountAsync(ct) : _dbSet.CountAsync(predicate, ct);

        public async Task AddAsync(T entity, CancellationToken ct = default) =>
            await _dbSet.AddAsync(entity, ct);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);
    }
}

