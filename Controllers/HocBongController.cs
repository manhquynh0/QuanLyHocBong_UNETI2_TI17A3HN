// Họ và tên: Nguyễn Văn Tình
// Mã sinh viên: 23103100142
// Nội dung thực hiện: Module 2 - Quản lý học bổng, tìm kiếm, lọc, sắp xếp và phân trang.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    [RoleAuthorize("Admin", "Cán bộ học bổng")]
    public class HocBongController : Controller
    {
        private readonly IHocBongService _hocBongService;

        public HocBongController(IHocBongService hocBongService)
        {
            _hocBongService = hocBongService;
        }

        public async Task<IActionResult> Index(HocBongIndexViewModel filters)
        {
            return View(await _hocBongService.LayDanhSachAsync(filters));
        }

        public async Task<IActionResult> Search([FromQuery] HocBongIndexViewModel filters)
        {
            return View("Index", await _hocBongService.LayDanhSachAsync(filters));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var hocBong = await _hocBongService.LayTheoMaAsync(id.Value);
            if (hocBong is null)
            {
                return NotFound();
            }

            ViewData["SoHoSo"] = await _hocBongService.DemSoHoSoAsync(hocBong.MaHocBong);
            return View(hocBong);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDonViTaiTroAsync();
            return View(new HocBongFormViewModel
            {
                NgayBatDauNhanHoSo = DateTime.Today,
                HanNopHoSo = DateTime.Today.AddDays(30),
                TrangThai = TrangThaiHocBong.DangNhanHoSo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HocBongFormViewModel model)
        {
            ValidateSchedule(model);
            if (ModelState.IsValid)
            {
                var (thanhCong, loi) = await _hocBongService.TaoAsync(ToEntity(model));
                if (thanhCong)
                {
                    TempData["HocBongMessage"] = "Tạo học bổng thành công. Danh sách đã được cập nhật.";
                    TempData["HocBongMessageType"] = "success";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, loi ?? "Không thể tạo học bổng.");
            }

            await PopulateDonViTaiTroAsync(model.MaDonViTaiTro);
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var hocBong = await _hocBongService.LayTheoMaAsync(id.Value);
            if (hocBong is null)
            {
                return NotFound();
            }

            await PopulateDonViTaiTroAsync(hocBong.MaDonViTaiTro);
            return View(ToFormViewModel(hocBong));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HocBongFormViewModel model)
        {
            if (id != model.MaHocBong)
            {
                return NotFound();
            }

            ValidateSchedule(model);
            if (ModelState.IsValid)
            {
                var (thanhCong, loi) = await _hocBongService.CapNhatAsync(id, ToEntity(model));
                if (thanhCong)
                {
                    TempData["HocBongMessage"] = "Cập nhật học bổng thành công. Danh sách đã được cập nhật.";
                    TempData["HocBongMessageType"] = "success";
                    return RedirectToAction(nameof(Index));
                }

                if (loi == "Không tìm thấy học bổng cần cập nhật.")
                {
                    return NotFound();
                }

                ModelState.AddModelError(string.Empty, loi ?? "Không thể cập nhật học bổng.");
            }

            await PopulateDonViTaiTroAsync(model.MaDonViTaiTro);
            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var hocBong = await _hocBongService.LayTheoMaAsync(id.Value);
            if (hocBong is null)
            {
                return NotFound();
            }

            return View(hocBong);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (thanhCong, loi) = await _hocBongService.XoaAsync(id);
            TempData["HocBongMessage"] = thanhCong
                ? "Xóa học bổng thành công."
                : loi ?? "Không thể xóa học bổng.";
            TempData["HocBongMessageType"] = thanhCong ? "success" : "error";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDonViTaiTroAsync(int? maDonViTaiTro = null)
        {
            var donViTaiTros = await _hocBongService.LayDanhSachDonViTaiTroAsync();
            ViewData["MaDonViTaiTro"] = new SelectList(
                donViTaiTros,
                "MaDonViTaiTro",
                "TenDonViTaiTro",
                maDonViTaiTro);
        }

        private void ValidateSchedule(HocBongFormViewModel model)
        {
            if (model.HanNopHoSo.HasValue
                && model.NgayBatDauNhanHoSo.HasValue
                && model.HanNopHoSo.Value.Date < model.NgayBatDauNhanHoSo.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(HocBongFormViewModel.HanNopHoSo),
                    "Hạn nộp hồ sơ không thể trước ngày bắt đầu nhận hồ sơ.");
            }
        }

        private static HocBong ToEntity(HocBongFormViewModel model)
        {
            return new HocBong
            {
                MaHocBong = model.MaHocBong,
                TenHocBong = model.TenHocBong,
                MaDonViTaiTro = model.MaDonViTaiTro,
                SoSuat = model.SoSuat,
                BacDaoTao = model.BacDaoTao,
                DiemTrungBinhToiThieu = model.DiemTrungBinhToiThieu,
                NgayBatDauNhanHoSo = model.NgayBatDauNhanHoSo!.Value,
                HanNopHoSo = model.HanNopHoSo!.Value,
                MoTaHocBong = model.MoTaHocBong ?? string.Empty,
                DieuKienXetHocBong = model.DieuKienXetHocBong ?? string.Empty,
                TrangThai = TrangThaiHocBong.TinhTrang(
                    model.NgayBatDauNhanHoSo!.Value,
                    model.HanNopHoSo!.Value,
                    DateTime.Today,
                    model.TrangThai)
            };
        }

        private static HocBongFormViewModel ToFormViewModel(HocBong hocBong)
        {
            return new HocBongFormViewModel
            {
                MaHocBong = hocBong.MaHocBong,
                TenHocBong = hocBong.TenHocBong,
                MaDonViTaiTro = hocBong.MaDonViTaiTro,
                SoSuat = hocBong.SoSuat,
                BacDaoTao = hocBong.BacDaoTao,
                DiemTrungBinhToiThieu = hocBong.DiemTrungBinhToiThieu,
                NgayBatDauNhanHoSo = hocBong.NgayBatDauNhanHoSo,
                HanNopHoSo = hocBong.HanNopHoSo,
                MoTaHocBong = hocBong.MoTaHocBong,
                DieuKienXetHocBong = hocBong.DieuKienXetHocBong,
                TrangThai = hocBong.TrangThai
            };
        }

    }
}
