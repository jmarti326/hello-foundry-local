using Microsoft.AspNetCore.Mvc;
using HelloFoundry.Web.Services;
using System.Text.Json;

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

    [HttpGet("stream/{message}")]
    public async Task StreamChat(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Response.StatusCode = 400;
                return;
            }

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            await foreach (var chunk in _aiChatService.ChatStreamAsync(Uri.UnescapeDataString(message)))
            {
                var data = JsonSerializer.Serialize(new { chunk = chunk });
                await Response.WriteAsync($"data: {data}\n\n");
                await Response.Body.FlushAsync();
                
                // Small delay to ensure proper streaming
                await Task.Delay(10);
            }

            // Send completion signal
            await Response.WriteAsync("data: {\"done\": true}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            var errorData = JsonSerializer.Serialize(new { error = "An error occurred while processing your request" });
            await Response.WriteAsync($"data: {errorData}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}