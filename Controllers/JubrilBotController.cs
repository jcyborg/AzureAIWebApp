using Azure.AI.OpenAI;
using AzureAIWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using OpenAIChatBot.Extensions;
using System.ClientModel;

namespace AzureAIWebApp.Controllers
{
    public class JubrilBotController : Controller
    {
        private readonly AzureOpenAIClient _aiClient;
        private readonly string _deploymentName = "your deployment name here";
        private readonly IConfiguration _configuration;
        public JubrilBotController(IConfiguration configuration)
        {
            _configuration = configuration;
            string apiKey = _configuration["OpenAIAPIKey"];
            string endpoint = _configuration["OpenAIEndPoint"];

            _aiClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        }

        //Load the chat window (GET request)
        public IActionResult Index()
        {
            var chatHistory = HttpContext.Session.Get<List<string>>("ChatHistory") ?? new List<string>();

            if (chatHistory.Count == 0)
            {
                chatHistory.Add("JubrilBot: Hello, my name is JubrilBot, how may I help you today?");
                HttpContext.Session.Set("ChatHistory", chatHistory);
            }

            var model = new ChatViewModel
            {
                ChatHistory = chatHistory
            };
            return View(model);
        }


        // Handle user input and return AI response (POST request)
        [HttpPost]
        public IActionResult Index(ChatViewModel model)
        {
            var chatHistory = HttpContext.Session.Get<List<string>>("ChatHistory") ?? new List<string>();

            if (!string.IsNullOrEmpty(model.UserInput))
            {
                ChatClient chatClient = _aiClient.GetChatClient(_deploymentName);

                ChatCompletion completion = chatClient.CompleteChat(new[]
                {
                    new UserChatMessage(model.UserInput),
                });

                chatHistory.Add($"You: {model.UserInput}");
                chatHistory.Add($"JubrilBot: {completion.Content[0].Text}");

                HttpContext.Session.Set("ChatHistory", chatHistory);
                model.UserInput = string.Empty;
            }
            model.ChatHistory = chatHistory;

            return View(model);
        }
    }
}
