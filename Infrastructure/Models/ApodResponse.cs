namespace VictorNovember.Infrastructure.Models;

public sealed class ApodResponse
{
    public string Date { get; set; } = "";
    public string Title { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string PostUrl { get; set; } = "";
    public string? HdUrl { get; set; }
    public string MediaType { get; set; } = "";
    public string? Copyright { get; set; }
}