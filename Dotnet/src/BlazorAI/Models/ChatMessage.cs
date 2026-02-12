using Azure.AI.Inference;

namespace BlazorAI.Models
{
    /// <summary>
    /// Wrapper class to provide a unified interface for displaying chat messages
    /// Maps Azure.AI.Inference ChatRequestMessage types to a simple display model
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;

        public ChatMessage(string role, string content, string? authorName = null)
        {
            Role = role;
            Content = content;
            AuthorName = authorName ?? role;
        }

        /// <summary>
        /// Convert from ChatRequestMessage to ChatMessage for display
        /// </summary>
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
                ChatRequestSystemMessage systemMsg => systemMsg.Content ?? string.Empty,
                ChatRequestToolMessage toolMsg => toolMsg.Content ?? string.Empty,
                _ => string.Empty
            };

            return new ChatMessage(role, content, authorName);
        }

        /// <summary>
        /// Convert a ChatMessage back to ChatRequestMessage
        /// </summary>
        public ChatRequestMessage ToChatRequestMessage()
        {
            return Role.ToLower() switch
            {
                "user" => new ChatRequestUserMessage(Content),
                "assistant" => new ChatRequestAssistantMessage(Content),
                "system" => new ChatRequestSystemMessage(Content),
                _ => new ChatRequestUserMessage(Content)
            };
        }
    }
}
