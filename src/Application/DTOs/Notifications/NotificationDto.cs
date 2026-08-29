namespace ERPSystem.Application.DTOs.Notifications;

/// <summary>كائن عرض لإشعار موظف (يُعرض في جرس الإشعارات).</summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Category { get; set; }
}