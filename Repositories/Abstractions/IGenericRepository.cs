using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Query(bool asNoTracking = true);
        Task<List<T>> GetAllAsync(CancellationToken ct = default);
        Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity);
        void Remove(T entity);
    }
}

