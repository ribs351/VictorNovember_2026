namespace VictorNovember.Interfaces;

public interface IPronunciationAnnotator
{
    Task<PronunciationResult> AnnotateAsync(string text, CancellationToken cancellationToken = default);
}
