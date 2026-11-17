using System;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static QuanLyHocBong_UNETI2_TI17A3HNContext Create()
        {
            var options = new DbContextOptionsBuilder<QuanLyHocBong_UNETI2_TI17A3HNContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString("N"))
                .Options;

            return new QuanLyHocBong_UNETI2_TI17A3HNContext(options);
        }
    }
}
