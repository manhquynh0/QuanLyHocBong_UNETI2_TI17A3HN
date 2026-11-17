using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Middleware;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Auth
{
    public class RoleAuthMiddlewareTests
    {
        private static DefaultHttpContext CreateContext(JwtPayload? user, params object[] metadata)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/Admin/Secret";
            context.Response.Body = new MemoryStream();
            if (user != null)
            {
                context.Items["JwtUser"] = user;
            }
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "Test"));
            return context;
        }

        [Fact]
        public async Task InvokeAsync_Unauthenticated_RedirectsToLogin()
        {
            bool nextCalled = false;
            var middleware = new RoleAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var ctx = CreateContext(null, new RoleAuthorizeAttribute());

            await middleware.InvokeAsync(ctx);

            Assert.False(nextCalled);
            Assert.Equal(302, ctx.Response.StatusCode);
            Assert.Contains("/TaiKhoan/DangNhap", ctx.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task InvokeAsync_UnauthenticatedAjax_Returns401()
        {
            bool nextCalled = false;
            var middleware = new RoleAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var ctx = CreateContext(null, new RoleAuthorizeAttribute());
            ctx.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

            await middleware.InvokeAsync(ctx);

            Assert.False(nextCalled);
            Assert.Equal(401, ctx.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_StudentAccessingAdminEndpoint_RedirectsToAccessDenied()
        {
            bool nextCalled = false;
            var middleware = new RoleAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var student = new JwtPayload { AccountId = 2, Role = "Sinh viên" };
            var ctx = CreateContext(student, new RoleAuthorizeAttribute("Admin"));

            await middleware.InvokeAsync(ctx);

            Assert.False(nextCalled);
            Assert.Equal(302, ctx.Response.StatusCode);
            Assert.Contains("/TaiKhoan/AccessDenied", ctx.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task InvokeAsync_OfficerAccessingCanBoHocBongEndpoint_AllowsThrough()
        {
            bool nextCalled = false;
            var middleware = new RoleAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
            var officer = new JwtPayload { AccountId = 3, Role = "Cán bộ xét duyệt" };
            var ctx = CreateContext(officer, new RoleAuthorizeAttribute(), new RoleAuthorizeAttribute("Admin", "Cán bộ học bổng"));

            await middleware.InvokeAsync(ctx);

            Assert.True(nextCalled);
        }
    }
}
