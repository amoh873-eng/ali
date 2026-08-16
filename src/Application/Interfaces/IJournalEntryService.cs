using ERPSystem.Application.DTOs.Journal;
using ERPSystem.Domain.Entities;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Interfaces;

/// <summary>
/// Contract for journal entry (القيود المحاسبية) operations.
/// The heart of double-entry accounting — every business document posts balanced entries here.
/// </summary>
public interface IJournalEntryService
{
    /// <summary>
    /// Returns all journal entries (newest first) including their lines.
    /// </summary>
    Task<List<JournalEntryDto>> GetEntriesAsync();

    /// <summary>
    /// Gets a single journal entry including its lines.
    /// </summary>
    Task<JournalEntryDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Returns a customer statement-style ledger for a given account (كشف حساب حساب/أستاذ).
    /// </summary>
    Task<List<JournalEntryDto>> GetByAccountAsync(Guid accountId);

    /// <summary>
    /// Prepares a balanced journal entry: validates that Σ(debits) == Σ(credits),
    /// builds the entry + lines, updates each account's CurrentBalance, and STAGES it
    /// in the shared DbContext (does NOT call SaveChanges — the caller posts it atomically
    /// together with its document). Returns the prepared entry.
    /// </summary>
    Task<JournalEntry> PrepareEntryAsync(
        JournalEntryType entryType,
        DateTime entryDate,
        string? reference,
        string? description,
        IReadOnlyCollection<JournalEntryLegInput> legs);

    /// <summary>
    /// Creates AND saves a manual journal entry (قيد يدوي) after validating balance.
    /// Unlike PrepareEntryAsync, this immediately persists the entry because a manual
    /// entry has no parent document that would save it on the caller's behalf.
    /// </summary>
    Task<JournalEntryDto> CreateManualEntryAsync(
        DateTime entryDate,
        string? reference,
        string? description,
        IReadOnlyCollection<JournalEntryLegInput> legs);
}
