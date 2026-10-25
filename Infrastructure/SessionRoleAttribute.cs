using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32("MaTaiKhoan") is null)
        {
            context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }
        var role = session.GetString("VaiTro");
        if (roles.Length > 0 && !roles.Any(r => RoleAuthorizeAttribute.IsRoleMatch(r, role)))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "TaiKhoan", null);
        }
    }
}
