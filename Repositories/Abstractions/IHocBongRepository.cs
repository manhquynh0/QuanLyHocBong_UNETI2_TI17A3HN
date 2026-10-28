using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions
{
    public interface IHocBongRepository : IGenericRepository<HocBong>
    {
        IQueryable<HocBong> QueryActiveWithSponsor(bool asNoTracking = true);
        Task<HocBong?> GetByIdWithSponsorAsync(int maHocBong);
        Task<HocBong?> GetActiveByIdAsync(int maHocBong);
        Task<List<string>> GetDistinctBacDaoTaoAsync();
    }
}

