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

        // Module 1 lưu Session với 3 khóa: "MaTaiKhoan" (int), "HoTen", "VaiTro".
        protected int? MaTaiKhoanHienTai => HttpContext.Session.GetInt32("MaTaiKhoan");

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var maTaiKhoan = context.HttpContext.Session.GetInt32("MaTaiKhoan");
            var vaiTro = context.HttpContext.Session.GetString("VaiTro");

            if (maTaiKhoan == null)
            {
                // Chưa đăng nhập -> về trang đăng nhập của Module 1 (đổi tên action/controller nếu khác).
                context.Result = RedirectToAction("DangNhap", "TaiKhoan");
                return;
            }

            if (vaiTro != VaiTro.SinhVien)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
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
