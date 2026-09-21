namespace LedgerCore.Api.Dtos;

public record BalanceResponse(long Amount, string CurrencyCode, string Formatted);
