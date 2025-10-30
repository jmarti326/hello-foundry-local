using Microsoft.AI.Foundry.Local;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using HelloFoundry.Web.Configuration;

namespace HelloFoundry.Web.Services;

public interface IAiChatService
{
    Task<string> ChatAsync(string message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ChatStreamAsync(string message, CancellationToken cancellationToken = default);
}

public class AiChatService : IAiChatService, IAsyncDisposable
{
    private readonly AiModelOptions _options;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    
    private FoundryLocalManager? _manager;
    private ChatClient? _chatClient;
    private bool _initialized = false;
    private bool _disposed = false;

    public AiChatService()
    {
        // For now, use default options - will be improved with proper DI later
        _options = new AiModelOptions();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.InitializationTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        await _initSemaphore.WaitAsync(linkedCts.Token);
        try
        {
            if (_initialized) return;

            Console.WriteLine($"Initializing AI model: {_options.ModelAlias}");
            
            _manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: _options.ModelAlias);
            
            var model = await _manager.GetModelInfoAsync(aliasOrModelId: _options.ModelAlias);
                
            var key = new ApiKeyCredential(_manager.ApiKey);
            var client = new OpenAIClient(key, new OpenAIClientOptions
            {
                Endpoint = _manager.Endpoint
            });

            _chatClient = client.GetChatClient(model?.ModelId);
            _initialized = true;
            
            Console.WriteLine("AI model initialized successfully");
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"AI model initialization timed out after {_options.InitializationTimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize AI model: {ex.Message}");
            throw new InvalidOperationException("Failed to initialize AI model", ex);
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));
        
        try
        {
            await EnsureInitializedAsync(cancellationToken);
            
            Console.WriteLine($"Processing chat request: {message[..Math.Min(50, message.Length)]}...");
            
            var completion = await _chatClient!.CompleteChatAsync([ChatMessage.CreateUserMessage(message)], cancellationToken: cancellationToken);
            var response = completion.Value.Content[0].Text;
            
            Console.WriteLine($"Chat response generated (length: {response.Length})");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ChatAsync: {ex.Message}");
            throw;
        }
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string message, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));
        
        await EnsureInitializedAsync(cancellationToken);
        
        Console.WriteLine($"Processing streaming chat request: {message[..Math.Min(50, message.Length)]}...");
        
        var completionUpdates = _chatClient!.CompleteChatStreaming([ChatMessage.CreateUserMessage(message)], cancellationToken: cancellationToken);
        
        foreach (var completionUpdate in completionUpdates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                var text = completionUpdate.ContentUpdate[0].Text;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                    
                    // Add configurable delay for better streaming experience
                    if (_options.EnableStreamingDelay && _options.StreamingDelayMs > 0)
                    {
                        await Task.Delay(_options.StreamingDelayMs, cancellationToken);
                    }
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _initSemaphore.WaitAsync();
            try
            {
                _manager?.Dispose();
                _initSemaphore?.Dispose();
                _disposed = true;
                Console.WriteLine("AiChatService disposed");
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}