namespace Bento.Api.Constants;

public static class OrderStatuses
{
    public const string Pending = "待確認";
    public const string Preparing = "製作中";
    public const string Completed = "已完成";
    public const string Cancelled = "已取消";

    public static readonly string[] All =
    [
        Pending,
        Preparing,
        Completed,
        Cancelled
    ];

    public static bool IsAllowed(string status) => All.Contains(status, StringComparer.Ordinal);
}
