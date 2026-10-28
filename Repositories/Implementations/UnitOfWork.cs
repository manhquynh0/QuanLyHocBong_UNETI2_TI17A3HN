using System.Threading;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations
{
    public class UnitOfWork(
        QuanLyHocBong_UNETI2_TI17A3HNContext context,
        IHocBongRepository hocBongRepository,
        IHoSoHocBongRepository hoSoHocBongRepository) : IUnitOfWork
    {
        private IGenericRepository<DonViTaiTro>? _donViTaiTros;
        private IGenericRepository<SinhVien>? _sinhViens;
        private IGenericRepository<TaiKhoan>? _taiKhoans;
        private IGenericRepository<TaiLieuHoSo>? _taiLieuHoSos;

        public IHocBongRepository HocBongs => hocBongRepository;
        public IHoSoHocBongRepository HoSoHocBongs => hoSoHocBongRepository;

        public IGenericRepository<DonViTaiTro> DonViTaiTros =>
            _donViTaiTros ??= new GenericRepository<DonViTaiTro>(context);

        public IGenericRepository<SinhVien> SinhViens =>
            _sinhViens ??= new GenericRepository<SinhVien>(context);

        public IGenericRepository<TaiKhoan> TaiKhoans =>
            _taiKhoans ??= new GenericRepository<TaiKhoan>(context);

        public IGenericRepository<TaiLieuHoSo> TaiLieuHoSos =>
            _taiLieuHoSos ??= new GenericRepository<TaiLieuHoSo>(context);

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            context.SaveChangesAsync(ct);
    }
}

