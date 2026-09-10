using System.Collections.Concurrent;

namespace LedgerCore.Domain.Money;

public static class CurrencyRegistry
{
    private static readonly ConcurrentDictionary<string, Currency> _currencies = new(StringComparer.OrdinalIgnoreCase);

    static CurrencyRegistry()
    {
        SeedDefaults();
    }

    public static Currency Get(string code)
    {
        if (_currencies.TryGetValue(code, out var currency))
            return currency;

        throw new KeyNotFoundException($"Currency '{code}' is not registered.");
    }

    public static bool TryGet(string code, out Currency? currency)
    {
        var found = _currencies.TryGetValue(code, out var result);
        currency = result;
        return found;
    }

    public static void Register(Currency currency)
    {
        if (!_currencies.TryAdd(currency.Code, currency))
            throw new InvalidOperationException(
                $"Currency '{currency.Code}' is already registered.");
    }

    public static IReadOnlyCollection<Currency> All()
        => _currencies.Values.ToList().AsReadOnly();

    public static void Reset()
    {
        _currencies.Clear();
        SeedDefaults();
    }

    private static void SeedDefaults()
    {
        _currencies.TryAdd(Currency.GHS.Code, Currency.GHS);
        _currencies.TryAdd(Currency.NGN.Code, Currency.NGN);
        _currencies.TryAdd(Currency.KES.Code, Currency.KES);
        _currencies.TryAdd(Currency.XOF.Code, Currency.XOF);
        _currencies.TryAdd(Currency.ZAR.Code, Currency.ZAR);
        _currencies.TryAdd(Currency.USD.Code, Currency.USD);
        _currencies.TryAdd(Currency.EUR.Code, Currency.EUR);
        _currencies.TryAdd(Currency.GBP.Code, Currency.GBP);
    }
}
