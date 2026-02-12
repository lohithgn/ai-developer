# Migration Guide: Semantic Kernel to Azure AI Foundry SDK

This guide documents the migration of the AI Developer workshop from Microsoft Semantic Kernel (v1.57.0) to Azure AI Foundry SDK for .NET (`Azure.AI.Inference` / `Azure.AI.Projects`).

## Overview

The migration replaces the unified Semantic Kernel abstraction with direct Azure AI SDK client classes, requiring manual implementation of features that were previously built into SK.

## Package Changes

### Removed Packages
- `Microsoft.SemanticKernel` (v1.57.0)
- `Microsoft.SemanticKernel.Agents.Core` (v1.57.0)
- `Microsoft.SemanticKernel.Agents.Magentic` (v1.57.0-preview)
- `Microsoft.SemanticKernel.Agents.Orchestration` (v1.57.0-preview)
- `Microsoft.SemanticKernel.Agents.Runtime.InProcess` (v1.57.0-preview)
- `Microsoft.SemanticKernel.Connectors.AzureAISearch` (v1.57.0-preview)
- `Microsoft.SemanticKernel.Connectors.OpenAI` (v1.57.0)
- `Microsoft.SemanticKernel.Plugins.OpenApi` (v1.57.0)

### Added Packages
- `Azure.AI.Inference` (v1.0.0-beta.5)
- `Azure.AI.Projects` (v1.1.0)
- `Azure.Search.Documents` (v11.7.0)

## Key Code Changes

### 1. Chat Completion (Challenge 02)

**Before (Semantic Kernel):**
```csharp
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
var kernel = kernelBuilder.Build();
var chatService = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory();
chatHistory.AddUserMessage(userMessage);
var response = await chatService.GetChatMessageContentAsync(chatHistory);
chatHistory.AddAssistantMessage(response.Content);
```

**After (Azure AI Foundry SDK):**
```csharp
var endpoint = new Uri(Configuration["AOI_ENDPOINT"]!);
var credential = new AzureKeyCredential(Configuration["AOI_API_KEY"]!);
var chatClient = new ChatCompletionsClient(endpoint, credential);

var chatRequestMessages = new List<ChatRequestMessage>();
chatRequestMessages.Add(new ChatRequestUserMessage(userMessage));

var options = new ChatCompletionsOptions()
{
    Messages = chatRequestMessages,
    Model = Configuration["AOI_DEPLOYMODEL"]
};

var response = await chatClient.CompleteAsync(options);
var assistantMessage = response.Value.Content;
chatRequestMessages.Add(new ChatRequestAssistantMessage(assistantMessage));
```

### 2. Function Calling / Plugins (Challenge 03)

**Before (Semantic Kernel):**
```csharp
[KernelFunction("geocode_address")]
[Description("Geocodes an address...")]
public async Task<string> GeocodeAddressAsync(string address)
{
    // Implementation
}

// Registration
kernel.ImportPluginFromObject(new GeocodingPlugin(...));

// Automatic function calling via OpenAIPromptExecutionSettings
var settings = new OpenAIPromptExecutionSettings 
{ 
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions 
};
```

**After (Azure AI Foundry SDK):**
```csharp
// Plugin class without attributes
public class GeocodingPlugin
{
    [Description("Geocodes an address...")]
    public async Task<string> GeocodeAddressAsync(string address)
    {
        // Implementation
    }
}

// Manual tool definition
var geocodingFunction = new FunctionDefinition("geocode_address")
{
    Description = "Takes an address search query...",
    Parameters = BinaryData.FromObjectAsJson(new
    {
        Type = "object",
        Properties = new
        {
            Address = new { Type = "string", Description = "..." }
        },
        Required = new[] { "address" }
    }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
};

var tools = new List<ChatCompletionsToolDefinition>
{
    new ChatCompletionsToolDefinition(geocodingFunction)
};

// Manual function calling loop
var options = new ChatCompletionsOptions()
{
    Messages = chatRequestMessages,
    Model = modelName
};

foreach (var tool in tools)
{
    options.Tools.Add(tool);
}

var response = await chatClient.CompleteAsync(options);

if (response.Value.ToolCalls != null && response.Value.ToolCalls.Count > 0)
{
    chatRequestMessages.Add(new ChatRequestAssistantMessage(response.Value));
    
    foreach (var toolCall in response.Value.ToolCalls)
    {
        // Dispatch to function
        string result = await ExecuteFunction(toolCall.Name, toolCall.Arguments);
        
        // Add result back to conversation
        chatRequestMessages.Add(new ChatRequestToolMessage(
            toolCallId: toolCall.Id,
            content: result));
    }
    
    // Continue conversation with another API call
}
```

### 3. Chat History Display

To maintain compatibility with the Razor UI, a wrapper model was created:

```csharp
public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public string AuthorName { get; set; }
    
    public static ChatMessage FromChatRequestMessage(ChatRequestMessage message, string? authorName = null)
    {
        string role = message switch
        {
            ChatRequestUserMessage => "user",
            ChatRequestAssistantMessage => "assistant",
            ChatRequestSystemMessage => "system",
            ChatRequestToolMessage => "tool",
            _ => "unknown"
        };
        
        string content = message switch
        {
            ChatRequestUserMessage userMsg => userMsg.Content ?? string.Empty,
            ChatRequestAssistantMessage assistantMsg => assistantMsg.Content ?? string.Empty,
            // ... etc
        };
        
        return new ChatMessage(role, content, authorName);
    }
}
```

## Features Still To Implement

The following challenges are left as exercises (with placeholder comments in code):

### Challenge 04: OpenAPI Plugin Import
- **SK Approach**: `ImportPluginFromOpenApiAsync`
- **Foundry Approach**: Parse OpenAPI spec manually, create `FunctionDefinition` for each operation

### Challenge 05: RAG / Embeddings
- **SK Approach**: `AddAzureOpenAITextEmbeddingGeneration`, `AzureAISearchVectorStore`
- **Foundry Approach**: Use `EmbeddingsClient` from `Azure.AI.Inference` and `SearchClient` from `Azure.Search.Documents`

### Challenge 07: Image Generation
- **SK Approach**: `AddAzureOpenAITextToImage`
- **Foundry Approach**: Use `ImageGenerationsClient` (to be added to SDK)

### Challenge 08: Multi-Agent
- **SK Approach**: `Agent`, `MagenticOrchestration`
- **Foundry Approach**: Use `Azure.AI.Projects` Agents API or build custom orchestration

## Configuration Changes

The configuration remains largely the same, using the same keys:
- `AOI_DEPLOYMODEL`: Model deployment name
- `AOI_ENDPOINT`: Azure OpenAI endpoint
- `AOI_API_KEY`: API key

## Breaking Changes

1. **No unified Kernel abstraction**: Direct client instantiation required
2. **Manual function calling**: No automatic tool invocation
3. **ChatHistory type change**: From `SK.ChatHistory` to `List<ChatRequestMessage>`
4. **No built-in plugin system**: Manual tool registration required
5. **Response structure**: Access via `response.Value.Content` instead of complex hierarchy

## Benefits of Migration

1. **Direct SDK access**: Less abstraction, more control
2. **Smaller dependency footprint**: Removed multiple preview packages
3. **Closer to Azure API**: Better alignment with official Azure patterns
4. **More explicit**: Function calling flow is visible and debuggable

## Testing

Build the project:
```bash
cd Dotnet/src/BlazorAI
dotnet restore
dotnet build
```

Run the application (requires valid Azure OpenAI configuration):
```bash
dotnet run
```

## Notes

- The migration maintains the workshop structure with challenge comments
- All warnings about nullable fields are pre-existing and not related to migration
- Multi-agent functionality is a placeholder pending full Azure.AI.Projects integration
