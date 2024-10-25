namespace AzureAIWebApp.Models
{
    public class ChatViewModel
    {
        public string UserInput { get; set; }
        public List<string> ChatHistory { get; set; } = new List<string>();
    }
}
