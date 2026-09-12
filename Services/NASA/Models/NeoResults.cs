namespace VictorNovember.Services.NASA.Models;
public sealed record NeoResult(
    string Name,
    string NasaJplUrl,
    bool IsPotentiallyHazardous,
    double DiameterMinMeters,
    double DiameterMaxMeters,
    double MissDistanceKm,
    double MissDistanceLunar,
    double VelocityKmPerSecond,
    string CloseApproachDate,
    string OrbitingBody,
    int TotalObjectCountToday
);