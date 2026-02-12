using BlazorAI.Queue;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Azure.AI.Inference;
using Azure;
using BlazorAI.Models;

namespace BlazorAI.Components.Pages
{
    public partial class MultiAgent
    {
        private List<ChatRequestMessage>? chatRequestMessages;
        private List<ChatMessage>? chatHistory;
        private ChatCompletionsClient? chatClient;

        [Inject]
        public required IConfiguration Configuration { get; set; }

        [Inject]
        private IBackgroundTaskQueue _backgroundTaskQueue { get; set; } = null!;


        protected void InitializeSemanticKernel()
        {
            chatRequestMessages = new List<ChatRequestMessage>();
            chatHistory = new List<ChatMessage>();

            // TODO: Implement multi-agent orchestration using Azure.AI.Projects Agents API
            // This is a placeholder implementation using basic chat completion
            var endpoint = new Uri(Configuration["AOI_ENDPOINT"]!);
            var credential = new AzureKeyCredential(Configuration["AOI_API_KEY"]!);
            chatClient = new ChatCompletionsClient(endpoint, credential);

            var systemMessage = new ChatRequestSystemMessage("You are a helpful AI assistant working in a multi-agent system.");
            chatRequestMessages.Add(systemMessage);
            chatHistory.Add(ChatMessage.FromChatRequestMessage(systemMessage));

            AddPlugins();

            CreateAgents();
        }

        private void CreateAgents()
        {
            // TODO: Implement agents using Azure.AI.Projects Agents API
            // This is a placeholder - actual multi-agent implementation will be in Phase 7
        }

        private void AddPlugins()
        {
            // TODO: Add plugins for multi-agent scenario
        }

        // Handle chat response
        private async ValueTask HandleChatResponseAsync(string response)
        {
            // Add assistant response to chat history
            var assistantMessage = new ChatRequestAssistantMessage(response);
            chatRequestMessages?.Add(assistantMessage);
            chatHistory?.Add(ChatMessage.FromChatRequestMessage(assistantMessage));

            // This is used to update the UI with the new message
            await InvokeAsync(StateHasChanged);
        }

        private async Task SendMessage()
        {
            if (chatClient is null)
            {
                throw new InvalidOperationException("The 'chatClient' field must be initialized before sending messages.");
            }

            // Copy the message from the user input - just like in Chat.razor.cs
            // This code grouping is used to handle the user input message and update the UI accordingly
            var userMessage = MessageInput;
            MessageInput = string.Empty;
            loading = true;
            
            var userRequestMessage = new ChatRequestUserMessage(userMessage);
            chatRequestMessages!.Add(userRequestMessage);
            chatHistory!.Add(ChatMessage.FromChatRequestMessage(userRequestMessage));
            StateHasChanged();

            // Use the injected _backgroundTaskQueue instance to queue the background chat orchestration task
            // This allows the UI to remain responsive while the orchestration runs in the background
            await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
            {
                try
                {
                    // TODO: Implement proper multi-agent orchestration
                    // For now, using basic chat completion as placeholder
                    var options = new ChatCompletionsOptions()
                    {
                        Messages = chatRequestMessages,
                        Model = Configuration["AOI_DEPLOYMODEL"]
                    };

                    var response = await chatClient.CompleteAsync(options);
                    var assistantMessage = response.Value.Content;
                    await HandleChatResponseAsync(assistantMessage ?? string.Empty);
                }
                catch (Exception ex)
                {
                    var errorMsg = new ChatMessage("assistant", $"Error: {ex.Message}");
                    chatHistory.Add(errorMsg);
                }
                finally
                {
                    // Ensure the UI is updated after the orchestration completes
                    loading = false;
                    await InvokeAsync(StateHasChanged);
                }
            });
        }

    }
}
