namespace LedgerCore.Api.Dtos;

public record PostEntryRequest(
    DateTimeOffset EntryDate,
    string Description,
    string Reference,
    List<EntryLineRequest> Lines,
    bool PostImmediately = false);

public record EntryLineRequest(
    Guid AccountId,
    long Amount,
    string CurrencyCode,
    string Type,
    decimal? FxRate = null);
