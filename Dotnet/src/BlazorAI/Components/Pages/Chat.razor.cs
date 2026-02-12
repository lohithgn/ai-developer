using Microsoft.AspNetCore.Components;
using Azure.AI.Inference;
using Azure;
using BlazorAI.Models;

namespace BlazorAI.Components.Pages;

public partial class Chat
{
    private List<ChatRequestMessage>? chatRequestMessages;
    private List<ChatMessage>? chatHistory;
    private ChatCompletionsClient? chatClient;

    [Inject]
    public required IConfiguration Configuration { get; set; }
    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = null!;

    protected async Task InitializeSemanticKernel()
    {
        chatRequestMessages = new List<ChatRequestMessage>();
        chatHistory = new List<ChatMessage>();

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

        // Challenge 03 - Create ChatCompletionsOptions
        // This will be handled per request in SendMessage
    }


    private async Task AddPlugins()
    {
        // Challenge 03 - Add Time Plugin

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

            // Challenge 02 - Send a message to the chat completion service
            var options = new ChatCompletionsOptions()
            {
                Messages = chatRequestMessages,
                Model = Configuration["AOI_DEPLOYMODEL"]
            };

            var response = await chatClient.CompleteAsync(options);

            // Challenge 02 - Add Response to the Chat History object
            var assistantMessage = response.Value.Content;
            var assistantRequestMessage = new ChatRequestAssistantMessage(assistantMessage);
            chatRequestMessages.Add(assistantRequestMessage);
            chatHistory.Add(ChatMessage.FromChatRequestMessage(assistantRequestMessage));

            loading = false;
        }
    }
}
