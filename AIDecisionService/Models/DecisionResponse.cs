namespace AIDecisionService.Models;

/// <summary>
/// Response containing the sequence of input frames
/// </summary>
public class DecisionResponse
{
    public InputFrameDto[] Sequence { get; set; } = Array.Empty<InputFrameDto>();
}
