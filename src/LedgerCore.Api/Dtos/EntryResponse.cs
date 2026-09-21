namespace LedgerCore.Api.Dtos;

public record EntryResponse(
    Guid Id,
    DateTimeOffset EntryDate,
    string Description,
    string Reference,
    string Status,
    List<EntryLineResponse> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PostedAt,
    DateTimeOffset? VoidedAt,
    string? VoidReason);

public record EntryLineResponse(
    Guid AccountId,
    long Amount,
    string CurrencyCode,
    string Type,
    decimal? FxRate);
