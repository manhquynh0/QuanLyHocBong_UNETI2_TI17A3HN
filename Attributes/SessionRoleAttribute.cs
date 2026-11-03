// using System.Linq;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Filters;
// using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
// using QuanLyHocBong_UNETI2_TI17A3HN.Services;

// namespace QuanLyHocBong_UNETI2_TI17A3HN.Infrastructure;

// [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// public sealed class SessionRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
// {
//     public void OnAuthorization(AuthorizationFilterContext context)
//     {
//         var jwtUser = context.HttpContext.Items["JwtUser"] as JwtPayload;
//         if (jwtUser is null)
//         {
//             context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", new { returnUrl = context.HttpContext.Request.Path });
//             return;
//         }
//         var role = jwtUser.Role;
//         if (roles.Length > 0 && !roles.Any(r => RoleAuthorizeAttribute.IsRoleMatch(r, role)))
//         {
//             context.Result = new RedirectToActionResult("AccessDenied", "TaiKhoan", null);
//         }
//     }
// }
