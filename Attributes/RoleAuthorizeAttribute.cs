using System;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Attributes
{
    /// <summary>
    /// Attribute đánh dấu yêu cầu đăng nhập và phân quyền theo vai trò.
    /// Logic kiểm tra được thực thi sớm tại tầng Middleware (RoleAuthMiddleware).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RoleAuthorizeAttribute : Attribute
    {
        public string[] AllowedRoles { get; }

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            AllowedRoles = allowedRoles;
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

            bool isCanBoAllowed = allowedRole.Contains("Cán bộ", StringComparison.OrdinalIgnoreCase);
            bool isCanBoUser = userRole.Contains("Cán bộ", StringComparison.OrdinalIgnoreCase);
            if (isCanBoAllowed && isCanBoUser)
                return true;

            bool isAdminAllowed = allowedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                              
            bool isAdminUser = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                           
            if (isAdminAllowed && isAdminUser)
                return true;

            bool isStudentAllowed = allowedRole.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase);
            bool isStudentUser = userRole.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase);
            if (isStudentAllowed && isStudentUser)
                return true;

            return false;
        }
    }
}
