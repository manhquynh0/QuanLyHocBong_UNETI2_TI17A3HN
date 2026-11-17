using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Middleware;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Auth
{
    public class RateLimitMiddlewareTests
    {
        private static DefaultHttpContext CreateContextWithPolicy(string policyName, bool isAjax = false)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            context.Response.Body = new MemoryStream();
            if (isAjax)
            {
                context.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            }

            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new EnableRateLimitingAttribute(policyName)),
                "TestEndpoint");
            context.SetEndpoint(endpoint);
            return context;
        }

        [Fact]
        public async Task InvokeAsync_ExceedsLoginLimit_Returns429On6thRequest()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            int nextCallCount = 0;
            var middleware = new RedisRateLimitMiddleware(
                _ => { nextCallCount++; return Task.CompletedTask; },
                cache,
                NullLogger<RedisRateLimitMiddleware>.Instance,
                redis: null);

            // 5 request đầu tiên phải được đi qua
            for (int i = 0; i < 5; i++)
            {
                var ctx = CreateContextWithPolicy("login_limit");
                await middleware.InvokeAsync(ctx);
                Assert.Equal(200, ctx.Response.StatusCode);
            }
            Assert.Equal(5, nextCallCount);

            // Request thứ 6 bị chặn với mã 429
            var blockedCtx = CreateContextWithPolicy("login_limit", isAjax: true);
            await middleware.InvokeAsync(blockedCtx);

            Assert.Equal(429, blockedCtx.Response.StatusCode);
            Assert.Equal(5, nextCallCount);

            blockedCtx.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(blockedCtx.Response.Body).ReadToEndAsync();
            Assert.Contains("success", body);
        }

        [Fact]
        public async Task InvokeAsync_WithoutRateLimitAttribute_AlwaysPassesThrough()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            int nextCallCount = 0;
            var middleware = new RedisRateLimitMiddleware(
                _ => { nextCallCount++; return Task.CompletedTask; },
                cache,
                NullLogger<RedisRateLimitMiddleware>.Instance,
                redis: null);

            for (int i = 0; i < 10; i++)
            {
                var ctx = new DefaultHttpContext();
                await middleware.InvokeAsync(ctx);
                Assert.Equal(200, ctx.Response.StatusCode);
            }

            Assert.Equal(10, nextCallCount);
        }
    }
}
