// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// MODULE 1: XÁC MINH JWT ACCESS TOKEN & PHÂN QUYỀN TRÊN CONTROLLER
// ============================================================================

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Lấy thông tin người dùng đã được xác minh qua JWT AccessToken (hoặc làm mới bằng RefreshToken)
            var jwtUser = context.HttpContext.Items["JwtUser"] as JwtPayloadData;
            int? maTaiKhoan = jwtUser?.MaTaiKhoan ?? context.HttpContext.Session.GetInt32("MaTaiKhoan");
            string? vaiTro = jwtUser?.VaiTro ?? context.HttpContext.Session.GetString("VaiTro");

            // 1. Kiểm tra đăng nhập (AccessToken hợp lệ)
            if (!maTaiKhoan.HasValue || string.IsNullOrEmpty(vaiTro))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", new { returnUrl });
                return;
            }

            // 2. Kiểm tra vai trò được phép
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                var hasPermission = _allowedRoles.Any(r => IsRoleMatch(r, vaiTro));
                if (!hasPermission)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "TaiKhoan", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// So khớp linh hoạt các vai trò chuẩn trong hệ thống:
        /// Hỗ trợ cả "Cán bộ học bổng" và "Cán bộ xét duyệt", "Admin", "Sinh viên"
        /// </summary>
        public static bool IsRoleMatch(string allowedRole, string? userRole)
        {
            if (string.IsNullOrWhiteSpace(allowedRole) || string.IsNullOrWhiteSpace(userRole))
                return false;

            allowedRole = allowedRole.Trim();
            userRole = userRole.Trim();

            if (allowedRole.Equals(userRole, StringComparison.OrdinalIgnoreCase))
                return true;

            bool isCanBoAllowed = allowedRole.Contains("Cán bộ", StringComparison.OrdinalIgnoreCase)
                               || allowedRole.Contains("Cán", StringComparison.OrdinalIgnoreCase)
                               || allowedRole.Equals("CanBo", StringComparison.OrdinalIgnoreCase);

            bool isCanBoUser = userRole.Contains("Cán bộ", StringComparison.OrdinalIgnoreCase)
                            || userRole.Contains("Cán", StringComparison.OrdinalIgnoreCase)
                            || userRole.Contains("CĂ", StringComparison.OrdinalIgnoreCase)
                            || userRole.Equals("CanBo", StringComparison.OrdinalIgnoreCase)
                            || userRole.Equals("canbo", StringComparison.OrdinalIgnoreCase);

            if (isCanBoAllowed && isCanBoUser)
                return true;

            bool isAdminAllowed = allowedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                               || allowedRole.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
            bool isAdminUser = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                            || userRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase)
                            || userRole.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);

            if (isAdminAllowed && isAdminUser)
                return true;

            bool isStudentAllowed = allowedRole.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase)
                                 || allowedRole.Equals("Sinh vien", StringComparison.OrdinalIgnoreCase);
            bool isStudentUser = userRole.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase)
                              || userRole.Equals("Sinh vien", StringComparison.OrdinalIgnoreCase)
                              || userRole.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase);

            if (isStudentAllowed && isStudentUser)
                return true;

            return false;
        }
    }
}
