using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Auth
{
    public class JwtServiceTests
    {
        private static JwtService CreateJwtService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT_SECRET_KEY"] = "UnitTest_Access_SecretKey_32_Bytes_Long_123456!",
                    ["JWT_REFRESH_SECRET_KEY"] = "UnitTest_Refresh_SecretKey_32_Bytes_Long_123456!"
                })
                .Build();

            return new JwtService(config);
        }

        [Fact]
        public void GenerateAndValidateAccessToken_ValidAccount_ReturnsExpectedClaims()
        {
            var jwtService = CreateJwtService();
            var account = new TaiKhoan
            {
                MaTaiKhoan = 10,
                TenDangNhap = "sv001",
                HoTen = "Nguyen Van A",
                VaiTro = "Sinh viên",
                AnhDaiDien = "https://cdn.example.com/avt.png"
            };

            var token = jwtService.GenerateAccessToken(account);
            var payload = jwtService.ValidateAccessToken(token);

            Assert.NotNull(payload);
            Assert.Equal(10, payload!.AccountId);
            Assert.Equal("sv001", payload.Username);
            Assert.Equal("Nguyen Van A", payload.FullName);
            Assert.Equal("Sinh viên", payload.Role);
            Assert.Equal("https://cdn.example.com/avt.png", payload.Avatar);
        }

        [Fact]
        public void GenerateRefreshToken_CalledTwice_ProducesUniqueCompactTokens()
        {
            var jwtService = CreateJwtService();
            var account = new TaiKhoan { MaTaiKhoan = 10, TenDangNhap = "sv001" };

            var rt1 = jwtService.GenerateRefreshToken(account);
            var rt2 = jwtService.GenerateRefreshToken(account);

            Assert.NotEqual(rt1, rt2);
            Assert.True(rt1.Length < 250);

            var payload = jwtService.ValidateRefreshToken(rt1);
            Assert.NotNull(payload);
            Assert.Equal(10, payload!.AccountId);
        }

        [Fact]
        public void ValidateAccessToken_TamperedOrEmptyToken_ReturnsNull()
        {
            var jwtService = CreateJwtService();
            var account = new TaiKhoan { MaTaiKhoan = 1, TenDangNhap = "admin", VaiTro = "Admin" };
            var token = jwtService.GenerateAccessToken(account);

            var tampered = token[..^3] + "xyz";

            Assert.Null(jwtService.ValidateAccessToken(tampered));
            Assert.Null(jwtService.ValidateAccessToken(""));
            Assert.Null(jwtService.ValidateAccessToken(null));
        }

        [Fact]
        public void ValidateAccessToken_WithRefreshToken_ReturnsNullBecauseDifferentSecrets()
        {
            var jwtService = CreateJwtService();
            var account = new TaiKhoan { MaTaiKhoan = 5, TenDangNhap = "sv005" };

            var refreshToken = jwtService.GenerateRefreshToken(account);

            Assert.Null(jwtService.ValidateAccessToken(refreshToken));
        }
    }
}
