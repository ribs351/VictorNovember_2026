namespace VictorNovember.Services.Fun.Models;

public sealed record ApodResult(
    string Title,
    string TrimmedExplanation,
    string ImageUrl,
    string Date,
    string? Copyright
);