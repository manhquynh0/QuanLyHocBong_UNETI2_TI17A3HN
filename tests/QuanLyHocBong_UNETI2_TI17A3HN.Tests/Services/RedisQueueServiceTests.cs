using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Services
{
    public class RedisQueueServiceTests
    {
        [Fact]
        public async Task EnqueueAndDequeueEmail_InMemoryFallback_PreservesFifoOrder()
        {
            var queue = new RedisQueueService(NullLogger<RedisQueueService>.Instance, redis: null);

            await queue.EnqueueEmailAsync(new EmailJob { ToEmail = "a@uneti.edu.vn", Subject = "1" });
            await queue.EnqueueEmailAsync(new EmailJob { ToEmail = "b@uneti.edu.vn", Subject = "2" });

            var first = await queue.DequeueEmailAsync();
            var second = await queue.DequeueEmailAsync();
            var empty = await queue.DequeueEmailAsync();

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Null(empty);
            Assert.Equal("a@uneti.edu.vn", first!.ToEmail);
            Assert.Equal("b@uneti.edu.vn", second!.ToEmail);
        }

        [Fact]
        public async Task EnqueueAndDequeueUpload_InMemoryFallback_PreservesFifoOrder()
        {
            var queue = new RedisQueueService(NullLogger<RedisQueueService>.Instance, redis: null);

            await queue.EnqueueUploadAsync(new UploadFileJob { LocalFilePath = "c:/1.pdf", EntityId = 1 });
            await queue.EnqueueUploadAsync(new UploadFileJob { LocalFilePath = "c:/2.pdf", EntityId = 2 });

            var first = await queue.DequeueUploadAsync();
            var second = await queue.DequeueUploadAsync();

            Assert.Equal(1, first!.EntityId);
            Assert.Equal(2, second!.EntityId);
        }
    }
}
