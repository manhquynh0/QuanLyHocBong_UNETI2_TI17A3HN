namespace QuanLyHocBong_UNETI2_TI17A3HN.Models;

public class HelloWord
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
