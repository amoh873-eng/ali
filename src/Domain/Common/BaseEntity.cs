namespace ERPSystem.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides common properties like Id and audit fields.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key for the entity.
    /// Uses Guid to avoid sequential ID guessing and support distributed systems.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the entity was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Username or identifier of who created this entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp of the last modification. Null if never modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Username or identifier of who last modified this entity.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag. When true, the entity is considered deleted
    /// but remains in the database for audit trails.
    /// </summary>
    public bool IsDeleted { get; set; }
}
