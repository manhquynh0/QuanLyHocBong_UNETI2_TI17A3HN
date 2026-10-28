using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions
{
    public interface IHoSoHocBongRepository : IGenericRepository<HoSoHocBong>
    {
        IQueryable<HoSoHocBong> QueryWithDetails(bool asNoTracking = true);
        Task<HoSoHocBong?> GetByIdWithDetailsAsync(int maHoSo);
        Task<int> CountByHocBongAsync(int maHocBong);
        Task<int> CountApprovedByHocBongAsync(int maHocBong);
        Task<List<TaiLieuItem>> GetTaiLieuItemsAsync(int maHoSo);
    }
}

