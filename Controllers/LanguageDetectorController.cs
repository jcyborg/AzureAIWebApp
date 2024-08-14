using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace AzureAIWebApp.Controllers
{
    public class LanguageDetectorController : Controller
    {
        private readonly string _aiSvcEndpoint;
        private readonly string _aiSvcKey;
        public LanguageDetectorController(IConfiguration configuration)
        {
            _aiSvcEndpoint = configuration["AIServicesEndpoint"];
            _aiSvcKey = configuration["AIServicesKey"];
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DetectLanguage(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                ViewBag.Error = "Please enter some text.";
                return View("Index");
            }

            var result = await GetLanguage(userText);
            ViewBag.LanguageResult = result;
            return View("Index");
        }

        private async Task<string> GetLanguage(string text)
        {
            try
            {
                JObject jsonBody = new JObject(
                    new JProperty("documents",
                    new JArray(
                        new JObject(
                            new JProperty("id", 1),
                            new JProperty("text", text)
                    ))));

                UTF8Encoding utf8 = new UTF8Encoding(true, true);
                byte[] encodedBytes = utf8.GetBytes(jsonBody.ToString());

                var client = new HttpClient();
                var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _aiSvcKey);
                var uri = _aiSvcEndpoint + "text/analytics/v3.1/languages?" + queryString;

                HttpResponseMessage response;
                using (var content = new ByteArrayContent(encodedBytes))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JObject results = JObject.Parse(responseContent);

                    StringBuilder resultBuilder = new StringBuilder();
                    foreach (JObject document in results["documents"])
                    {
                        resultBuilder.AppendLine("Language: " + (string)document["detectedLanguage"]["name"]);
                    }
                    return resultBuilder.ToString();
                }
                else
                {
                    return response.ToString();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
