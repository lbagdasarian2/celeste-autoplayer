using AIDecisionService.Models;
using AIDecisionService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIDecisionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecisionController : ControllerBase
{
    private readonly AIDecisionService.Services.AIDecisionService _aiService;

    public DecisionController(AIDecisionService.Services.AIDecisionService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Get decision based on game state
    /// </summary>
    /// <param name="state">Current game state</param>
    /// <returns>Sequence of input frames to execute</returns>
    [HttpPost("decide")]
    public ActionResult<DecisionResponse> Decide([FromBody] GameStateDto state)
    {
        if (state == null)
        {
            return BadRequest("Game state is required");
        }

        try
        {
            var sequence = _aiService.DecideNextAction(state);
            return Ok(new DecisionResponse { Sequence = sequence });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new { status = "healthy" });
    }
}
