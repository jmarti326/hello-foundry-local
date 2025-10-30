using Microsoft.AI.Foundry.Local;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace HelloFoundry.Web.Services;

public interface IAiChatService
{
    Task<string> ChatAsync(string message);
    IAsyncEnumerable<string> ChatStreamAsync(string message);
}

public class AiChatService : IAiChatService, IDisposable
{
    private FoundryLocalManager? _manager;
    private ChatClient? _chatClient;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private bool _initialized = false;
    private bool _disposed = false;

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_initialized) return;

            var alias = "qwen2.5-0.5b";
            
            _manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);
            
            var model = await _manager.GetModelInfoAsync(aliasOrModelId: alias);
            var key = new ApiKeyCredential(_manager.ApiKey);
            var client = new OpenAIClient(key, new OpenAIClientOptions
            {
                Endpoint = _manager.Endpoint
            });

            _chatClient = client.GetChatClient(model?.ModelId);
            _initialized = true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<string> ChatAsync(string message)
    {
        await EnsureInitializedAsync();
        
        var completion = await _chatClient!.CompleteChatAsync(message);
        return completion.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string message)
    {
        await EnsureInitializedAsync();
        
        var completionUpdates = _chatClient!.CompleteChatStreaming(message);
        
        foreach (var completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                var text = completionUpdate.ContentUpdate[0].Text;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _manager?.Dispose();
            _initSemaphore?.Dispose();
            _disposed = true;
        }
    }
}