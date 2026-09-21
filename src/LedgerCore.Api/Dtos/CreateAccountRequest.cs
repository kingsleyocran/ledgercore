namespace LedgerCore.Api.Dtos;

public record CreateAccountRequest(
    string Name,
    string AccountNumber,
    string Type,
    string CurrencyCode,
    Guid? ParentId = null);
