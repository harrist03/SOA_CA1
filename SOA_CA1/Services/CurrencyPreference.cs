namespace SOA_CA1.Services;

public sealed class CurrencyPreference
{
    public string CurrentCurrency { get; private set; } = "USD";

    public void SetCurrency(string currency)
    {
        if (currency is not ("USD" or "EUR" or "GBP" or "JPY" or "CNY"))
        {
            throw new ArgumentException("An unsupported currency was provided.", nameof(currency));
        }

        CurrentCurrency = currency;
    }

    public static string GetSymbol(string currency) =>
        currency switch
        {
            "EUR" => "€",
            "GBP" => "£",
            "JPY" or "CNY" => "¥",
            _ => "$"
        };
}
