using ERPSystem.Application.DTOs.Leaves;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for leave request operations (الإجازات).
/// </summary>
public interface ILeaveService
{
    /// <summary>
    /// Returns all leave requests (with employee names).
    /// </summary>
    Task<List<LeaveDto>> GetAllAsync();

    /// <summary>
    /// Gets a single leave request by id.
    /// </summary>
    Task<LeaveDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new leave request. Validates employee existence and date order.
    /// </summary>
    Task<LeaveDto> CreateAsync(CreateLeaveDto dto);

    /// <summary>
    /// Approves or rejects a leave request (معلّقة/موافق عليها/مرفوضة).
    /// </summary>
    Task<LeaveDto> SetStatusAsync(Guid id, LeaveStatus status);

    /// <summary>
    /// Soft-deletes a leave request.
    /// </summary>
    Task DeleteAsync(Guid id);
}
