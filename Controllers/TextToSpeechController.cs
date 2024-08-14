using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using System.Text;

namespace AzureAIWebApp.Controllers
{
    public class TextToSpeechController : Controller
    {
        private readonly IConfiguration _configuration;

        public TextToSpeechController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConvertTextToSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest("Text cannot be empty.");
            }
            string aiSpeechSvcRegion = _configuration["AISpeechServicesRegion"];
            string aiSpeechSvcKey = _configuration["AISpeechServicesKey"];

            var speechConfig = SpeechConfig.FromSubscription(aiSpeechSvcKey, aiSpeechSvcRegion);
            speechConfig.SpeechSynthesisVoiceName = "en-US-AriaNeural";

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
            var result = await speechSynthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                using var audioDataStream = AudioDataStream.FromResult(result);
                var memoryStream = new MemoryStream();
                byte[] buffer = new byte[4096];
                uint bytesRead;

                // Read the audio data into memoryStream
                while ((bytesRead = audioDataStream.ReadData(buffer)) > 0)
                {
                    memoryStream.Write(buffer, 0, (int)bytesRead);
                }

                // Convert memory stream to byte array
                byte[] audioBytes = memoryStream.ToArray();
                // Convert byte array to base64 string
                string base64Audio = Convert.ToBase64String(audioBytes);

                // Pass the base64 audio string to the view
                ViewBag.AudioBase64 = base64Audio;
                return View("Index");
            }
            else
            {
                return BadRequest($"Speech synthesis failed. Reason: {result.Reason}");
            }
        }


    }
}
