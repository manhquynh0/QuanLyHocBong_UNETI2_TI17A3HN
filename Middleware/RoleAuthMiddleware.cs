using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Middleware
{
    /// <summary>
    /// Middleware kiểm tra đăng nhập và phân quyền theo [RoleAuthorize] gắn trên Controller/Action.
    /// Chạy ngay sau JwtAuthMiddleware để chặn sớm các request trái phép trước khi vào MVC.
    /// </summary>
    public class RoleAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var roleAttrs = context.GetEndpoint()?.Metadata.GetOrderedMetadata<RoleAuthorizeAttribute>();
            if (roleAttrs == null || roleAttrs.Count == 0)
            {
                await _next(context);
                return;
            }

            // 1. Kiểm tra đã đăng nhập qua JWT chưa
            var jwtUser = context.Items["JwtUser"] as JwtPayload;
            if (jwtUser == null || string.IsNullOrEmpty(jwtUser.Role))
            {
                if (IsAjaxRequest(context))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync("{\"success\": false, \"message\": \"Vui lòng đăng nhập để tiếp tục.\"}");
                    return;
                }

                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/TaiKhoan/DangNhap?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }

            // 2. Kiểm tra từng yêu cầu phân quyền (cấp Controller và cấp Action)
            foreach (var attr in roleAttrs)
            {
                if (attr.AllowedRoles == null || attr.AllowedRoles.Length == 0)
                    continue;

                bool hasPermission = attr.AllowedRoles.Any(r => RoleAuthorizeAttribute.IsRoleMatch(r, jwtUser.Role));
                if (!hasPermission)
                {
                    if (IsAjaxRequest(context))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"success\": false, \"message\": \"Bạn không có quyền thực hiện chức năng này.\"}");
                        return;
                    }

                    context.Response.Redirect("/TaiKhoan/AccessDenied");
                    return;
                }
            }

            await _next(context);
        }

        private static bool IsAjaxRequest(HttpContext context)
        {
            return string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
