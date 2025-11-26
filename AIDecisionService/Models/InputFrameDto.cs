namespace AIDecisionService.Models;

/// <summary>
/// Data Transfer Object for input frame
/// </summary>
public class InputFrameDto
{
    public int Action { get; set; }  // Actions enum as int for serialization
    public int Frames { get; set; }
}
