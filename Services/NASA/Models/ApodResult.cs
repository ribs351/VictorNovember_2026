namespace VictorNovember.Services.NASA.Models;

public sealed record ApodResult(
    string Title,
    string TrimmedExplanation,
    string ImageUrl,
    string Date,
    string? Copyright
);