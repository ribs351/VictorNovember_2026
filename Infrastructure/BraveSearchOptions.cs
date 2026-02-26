namespace VictorNovember.Infrastructure;

public sealed class BraveSearchOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.search.brave.com/res/v1/";
    public int MonthlyLimit { get; set; } = 1000;
    public string SafeSearch { get; set; } = "strict";
}
