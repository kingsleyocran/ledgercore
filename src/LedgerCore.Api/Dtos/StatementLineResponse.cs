namespace LedgerCore.Api.Dtos;

public record StatementLineResponse(
    DateTimeOffset EntryDate,
    string Description,
    string Reference,
    long? DebitAmount,
    long? CreditAmount,
    string CurrencyCode,
    long RunningBalance);
