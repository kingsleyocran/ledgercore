namespace LedgerCore.Api.Dtos;

public record AccountResponse(
    Guid Id,
    string Name,
    string AccountNumber,
    string Type,
    string CurrencyCode,
    Guid? ParentId,
    bool IsActive,
    DateTimeOffset CreatedAt);
