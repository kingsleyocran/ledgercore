namespace LedgerCore.Domain.Money;

public sealed record Currency
{
    public string Code { get; }
    public string Name { get; }
    public int DecimalPlaces { get; }
    public string Symbol { get; }

    public Currency(string code, string name, int decimalPlaces, string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        Code = code.ToUpperInvariant();
        Name = name;
        DecimalPlaces = decimalPlaces;
        Symbol = symbol;
    }

    public static readonly Currency GHS = new("GHS", "Ghana Cedi", 2, "₵");
    public static readonly Currency NGN = new("NGN", "Nigerian Naira", 2, "₦");
    public static readonly Currency KES = new("KES", "Kenyan Shilling", 2, "KSh");
    public static readonly Currency XOF = new("XOF", "West African CFA", 0, "CFA");
    public static readonly Currency ZAR = new("ZAR", "South African Rand", 2, "R");
    public static readonly Currency USD = new("USD", "US Dollar", 2, "$");
    public static readonly Currency EUR = new("EUR", "Euro", 2, "€");
    public static readonly Currency GBP = new("GBP", "British Pound", 2, "£");
}
