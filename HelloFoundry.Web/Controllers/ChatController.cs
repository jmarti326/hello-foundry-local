using Microsoft.AspNetCore.Mvc;
using HelloFoundry.Web.Services;

namespace HelloFoundry.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;

    public ChatController(IAiChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var response = await _aiChatService.ChatAsync(request.Message);
            
            return Ok(new { response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}