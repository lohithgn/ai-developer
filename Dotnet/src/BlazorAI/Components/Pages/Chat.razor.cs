using Microsoft.AspNetCore.Components;
using Azure.AI.Inference;
using Azure;
using BlazorAI.Models;
using BlazorAI.Plugins;
using System.Text.Json;

namespace BlazorAI.Components.Pages;

public partial class Chat
{
    private List<ChatRequestMessage>? chatRequestMessages;
    private List<ChatMessage>? chatHistory;
    private ChatCompletionsClient? chatClient;
    private List<ChatCompletionsToolDefinition>? tools;
    private GeocodingPlugin? geocodingPlugin;

    [Inject]
    public required IConfiguration Configuration { get; set; }
    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;
    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = null!;

    protected async Task InitializeSemanticKernel()
    {
        chatRequestMessages = new List<ChatRequestMessage>();
        chatHistory = new List<ChatMessage>();
        tools = new List<ChatCompletionsToolDefinition>();

        // Challenge 02 - Configure Azure AI Inference Client
        var endpoint = new Uri(Configuration["AOI_ENDPOINT"]!);
        var credential = new AzureKeyCredential(Configuration["AOI_API_KEY"]!);
        chatClient = new ChatCompletionsClient(endpoint, credential);

        // Add system message to set the context
        var systemMessage = new ChatRequestSystemMessage("You are a helpful AI assistant.");
        chatRequestMessages.Add(systemMessage);
        chatHistory.Add(ChatMessage.FromChatRequestMessage(systemMessage));

        // Challenge 03 and 04 - Services Required
        // No additional setup needed for Azure AI Inference

        // Challenge 05 - Register Azure AI Foundry Text Embeddings Generation


        // Challenge 05 - Register Search Index


        // Challenge 07 - Add Azure AI Foundry Text To Image


        // Challenge 03, 04, 05, & 07 - Add Plugins
        await AddPlugins();
    }


    private async Task AddPlugins()
    {
        // Challenge 03 - Add Geocoding Plugin Function Tool Definition
        geocodingPlugin = new GeocodingPlugin(HttpClientFactory, Configuration);
        
        var geocodingFunction = new FunctionDefinition("geocode_address")
        {
            Description = "Takes an address search query, and returns a collection of latitude and longitude coordinates that are most likely to match the query. The more specific the query, the better the results. IE: use 27301, USA to get the address of a postal code in the US. Or '5027 Bartley Way, McLeansville NC' will get better results - than just something like '27301' or 'Springfield'.",
            Parameters = BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new
                {
                    Address = new
                    {
                        Type = "string",
                        Description = "The address to geocode"
                    }
                },
                Required = new[] { "address" }
            }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };
        
        tools?.Add(new ChatCompletionsToolDefinition(geocodingFunction));

        // Challenge 04 - Import OpenAPI Spec

        // Challenge 05 - Add Search Plugin

        // Challenge 07 - Text To Image Plugin

    }

    private async Task SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(newMessage) && chatRequestMessages != null && chatHistory != null && chatClient != null)
        {
            // This tells Blazor the UI is going to be updated.
            StateHasChanged();
            loading = true;
            // Copy the user message to a local variable and clear the newMessage field in the UI
            var userMessage = newMessage;
            newMessage = string.Empty;
            StateHasChanged();

            // Challenge 02 - Update Chat History
            var userRequestMessage = new ChatRequestUserMessage(userMessage);
            chatRequestMessages.Add(userRequestMessage);
            chatHistory.Add(ChatMessage.FromChatRequestMessage(userRequestMessage));

            // Challenge 03 - Implement function calling loop
            bool continueLoop = true;
            int maxIterations = 5; // Prevent infinite loops
            int iteration = 0;

            while (continueLoop && iteration < maxIterations)
            {
                iteration++;

                // Challenge 02 - Send a message to the chat completion service
                var options = new ChatCompletionsOptions()
                {
                    Messages = chatRequestMessages,
                    Model = Configuration["AOI_DEPLOYMODEL"]
                };

                // Challenge 03 - Add tools to the request if available
                if (tools != null && tools.Count > 0)
                {
                    foreach (var tool in tools)
                    {
                        options.Tools.Add(tool);
                    }
                }

                var response = await chatClient.CompleteAsync(options);

                // Challenge 03 - Check if we have tool calls
                if (response.Value.ToolCalls != null && response.Value.ToolCalls.Count > 0)
                {
                    // Add the assistant message with tool calls to history
                    chatRequestMessages.Add(new ChatRequestAssistantMessage(response.Value));

                    // Display the assistant's reasoning if there's content
                    if (!string.IsNullOrEmpty(response.Value.Content))
                    {
                        chatHistory.Add(new ChatMessage("assistant", response.Value.Content));
                        StateHasChanged();
                    }

                    // Process each tool call
                    foreach (var toolCall in response.Value.ToolCalls)
                    {
                        var functionName = toolCall.Name;
                        var functionArgs = toolCall.Arguments;

                        // Display tool call message
                        chatHistory.Add(new ChatMessage("tool", $"Calling function: {functionName} with args: {functionArgs}"));
                        StateHasChanged();

                        // Dispatch to the appropriate function
                        string toolResult = await ExecuteFunction(functionName, functionArgs);

                        // Add the tool response to the conversation
                        chatRequestMessages.Add(new ChatRequestToolMessage(
                            toolCallId: toolCall.Id,
                            content: toolResult));

                        // Display tool result
                        chatHistory.Add(new ChatMessage("tool", $"Function result: {toolResult}"));
                        StateHasChanged();
                    }
                }
                else
                {
                    // No tool calls, we're done
                    // Challenge 02 - Add Response to the Chat History object
                    var assistantMessage = response.Value.Content;
                    var assistantRequestMessage = new ChatRequestAssistantMessage(assistantMessage);
                    chatRequestMessages.Add(assistantRequestMessage);
                    chatHistory.Add(ChatMessage.FromChatRequestMessage(assistantRequestMessage));
                    continueLoop = false;
                }
            }

            loading = false;
        }
    }

    private async Task<string> ExecuteFunction(string functionName, string functionArgs)
    {
        try
        {
            // Challenge 03 - Dispatch function calls
            switch (functionName)
            {
                case "geocode_address":
                    var geocodeArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(functionArgs);
                    var address = geocodeArgs?["address"].GetString() ?? string.Empty;
                    return await geocodingPlugin!.GeocodeAddressAsync(address);

                default:
                    return $"Unknown function: {functionName}";
            }
        }
        catch (Exception ex)
        {
            return $"Error executing function {functionName}: {ex.Message}";
        }
    }
}
