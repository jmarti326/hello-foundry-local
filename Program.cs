using Microsoft.AI.Foundry.Local;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics.Metrics;

var alias = "qwen2.5-0.5b";

var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);

var model = await manager.GetModelInfoAsync(aliasOrModelId: alias);
ApiKeyCredential key = new ApiKeyCredential(manager.ApiKey);
OpenAIClient client = new OpenAIClient(key, new OpenAIClientOptions
{
    Endpoint = manager.Endpoint
});

var chatClient = client.GetChatClient(model?.ModelId);

var completionUpdates = chatClient.CompleteChatStreaming("Why is the sky blue'");

Console.Write($"[ASSISTANT]: ");
foreach (var completionUpdate in completionUpdates)
{
    if (completionUpdate.ContentUpdate.Count > 0)
    {
        Console.Write(completionUpdate.ContentUpdate[0].Text);
    }
}