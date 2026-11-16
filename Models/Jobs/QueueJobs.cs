using System;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs
{
    public class EmailJob
    {
        public string JobId { get; set; } = Guid.NewGuid().ToString("N");
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UploadFileJob
    {
        public string JobId { get; set; } = Guid.NewGuid().ToString("N");
        public string LocalFilePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string Folder { get; set; } = "minhchung";
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
