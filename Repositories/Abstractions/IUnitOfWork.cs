using System.Threading;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions
{
    public interface IUnitOfWork
    {
        IHocBongRepository HocBongs { get; }
        IHoSoHocBongRepository HoSoHocBongs { get; }
        IGenericRepository<DonViTaiTro> DonViTaiTros { get; }
        IGenericRepository<SinhVien> SinhViens { get; }
        IGenericRepository<TaiKhoan> TaiKhoans { get; }
        IGenericRepository<TaiLieuHoSo> TaiLieuHoSos { get; }
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}

