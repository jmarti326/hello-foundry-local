using System.ComponentModel.DataAnnotations;

namespace HelloFoundry.Web.Models;

public class ChatRequest
{
    [Required(ErrorMessage = "Message is required")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 4000 characters")]
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Response { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool Success { get; init; } = true;
}

public class ErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string? Details { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool Success { get; init; } = false;
}