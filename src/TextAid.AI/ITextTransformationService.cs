using TextAid.Core;

namespace TextAid.AI;

/// <summary>Transforms one captured text through the configured AI middleware path.</summary>
public interface ITextTransformationService
{
    /// <summary>Returns one complete transformed result.</summary>
    Task<string> TransformAsync(TextTransformationRequest request, CancellationToken cancellationToken);
}
