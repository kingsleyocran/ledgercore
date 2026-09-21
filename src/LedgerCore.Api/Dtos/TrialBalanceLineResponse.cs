namespace LedgerCore.Api.Dtos;

public record TrialBalanceLineResponse(
    Guid AccountId,
    string AccountName,
    string AccountNumber,
    string AccountType,
    long DebitBalance,
    long CreditBalance,
    string CurrencyCode);
