namespace VictorNovember.Interfaces;

public interface ITtsService
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}
