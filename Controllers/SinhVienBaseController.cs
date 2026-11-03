// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - lớp cơ sở kiểm tra quyền Sinh viên (tại Controller) và lấy sinh viên đang đăng nhập.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public abstract class SinhVienBaseController : Controller
    {
        protected readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        protected SinhVienBaseController(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        // Lấy MaTaiKhoan từ JWT Payload trong HttpContext.Items
        protected int? MaTaiKhoanHienTai => HttpContext.Items["MaTaiKhoan"] as int?;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var maTaiKhoan = context.HttpContext.Items["MaTaiKhoan"] as int?;
            var vaiTro = context.HttpContext.Items["VaiTro"] as string;

            if (maTaiKhoan == null)
            {
                context.Result = RedirectToAction("DangNhap", "TaiKhoan");
                return;
            }

            bool laSinhVien = !string.IsNullOrWhiteSpace(vaiTro) &&
                (vaiTro.Trim().Equals(VaiTro.SinhVien, StringComparison.OrdinalIgnoreCase)
                 || vaiTro.Trim().Equals("SinhVien", StringComparison.OrdinalIgnoreCase)
                 || vaiTro.Trim().StartsWith("Sinh", StringComparison.OrdinalIgnoreCase));

            if (!laSinhVien)
            {
                context.Result = RedirectToAction("AccessDenied", "TaiKhoan");
                return;
            }

            base.OnActionExecuting(context);
        }

        protected async Task<SinhVien?> LaySinhVienHienTaiAsync(bool theoDoi = false)
        {
            var maTaiKhoan = MaTaiKhoanHienTai;
            if (maTaiKhoan == null) return null;

            IQueryable<SinhVien> q = _context.SinhVien;
            if (!theoDoi) q = q.AsNoTracking();
            return await q.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
        }
    }
}
