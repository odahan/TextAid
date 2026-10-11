namespace TextAid.AI;

/// <summary>Contains the server's reported memory allocation, active context, and retention deadline.</summary>
public sealed record OllamaModelState(long? SizeBytes, long? VramBytes, long? ContextSize, DateTimeOffset? ExpiresAt)
{
    /// <summary>Gets the share of reported model memory allocated on GPU, rather than GPU utilization.</summary>
    public double? GpuMemoryPercent => SizeBytes is > 0 && VramBytes is not null
        ? Math.Clamp(100d * VramBytes.Value / SizeBytes.Value, 0, 100)
        : null;
}
