using Azure.AI.Vision.ImageAnalysis;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using RectangleF = SixLabors.ImageSharp.RectangleF;
using ImageAnalyzer.Models;
using SixLabors.ImageSharp.PixelFormats;
using ImageAnalyzer.ViewModel;

public class ImageAnalysisController : Controller
{
    private readonly IConfiguration _configuration;

    public ImageAnalysisController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(ImageModel model)
    {
        if (model.ImageFile == null || model.ImageFile.Length == 0)
        {
            ModelState.AddModelError("", "Please upload an image file.");
            return View("Upload");
        }

        string aiSvcEndpoint = _configuration["AIServicesEndpoint"];
        string aiSvcKey = _configuration["AIServicesKey"];
        string outputFolder = _configuration["OutputFolder"];

        var client = new ImageAnalysisClient(new Uri(aiSvcEndpoint), new AzureKeyCredential(aiSvcKey));
        var imageFilePath = Path.Combine(outputFolder, model.ImageFile.FileName);

        using (var stream = new FileStream(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await model.ImageFile.CopyToAsync(stream);
        }

        var result = AnalyzeImage(imageFilePath, client);

        // Create the ViewModel
        var viewModel = new AnalysisResultViewModel
        {
            AnnotatedImagePath = Path.Combine(outputFolder, "annotated_" + Path.GetFileName(imageFilePath)),
            Caption = result.Caption?.Text,
            DenseCaptions = result.DenseCaptions?.Values.Select(dc => dc.Text).ToList(),
            Tags = result.Tags?.Values.Select(tag => tag.Name).ToList(),
            Objects = result.Objects?.Values.Select(obj => obj.Tags[0].Name).ToList(),
            People = result.People?.Values.Select(p => $"Bounding box {p.BoundingBox}, Confidence: {p.Confidence:F2}").ToList()
        };

        return View("AnalyzeResult", viewModel);
    }



    [HttpPost]
    public async Task<IActionResult> RemoveBackground(ImageModel model)
    {
        if (model.ImageFile == null || model.ImageFile.Length == 0)
        {
            ModelState.AddModelError("", "Please upload an image file.");
            return View("Upload");
        }

        string aiSvcEndpoint = _configuration["AIServicesEndpoint"];
        string aiSvcKey = _configuration["AIServicesKey"];
        string outputFolder = _configuration["OutputFolder"];

        var imageFilePath = Path.Combine(outputFolder, model.ImageFile.FileName);

        using (var stream = new FileStream(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await model.ImageFile.CopyToAsync(stream);
        }

        var resultFilePath = await BackgroundForeground(imageFilePath, aiSvcEndpoint, aiSvcKey, outputFolder);

        // Pass the result file path to the view
        ViewData["BackgroundRemovedImagePath"] = resultFilePath;

        return View("RemoveBackgroundResult");
    }


    private ImageAnalysisResult AnalyzeImage(string imageFile, ImageAnalysisClient client)
    {
        using var stream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = client.Analyze(
            BinaryData.FromStream(stream),
            VisualFeatures.Caption |
            VisualFeatures.DenseCaptions |
            VisualFeatures.Objects |
            VisualFeatures.Tags |
            VisualFeatures.People);

        // Annotate image and save the result
        AnnotateImage(imageFile, result);
        return result;
    }

    private void AnnotateImage(string imageFile, ImageAnalysisResult result)
    {
        using var stream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);

        var penObject = Pens.Solid(Color.Cyan, 3);
        var penPerson = Pens.Solid(Color.Red, 3);
        var font = SystemFonts.CreateFont("Arial", 16);
        var brush = Brushes.Solid(Color.WhiteSmoke);

        foreach (var detectedObject in result.Objects.Values)
        {
            var rect = new RectangleF(detectedObject.BoundingBox.X, detectedObject.BoundingBox.Y, detectedObject.BoundingBox.Width, detectedObject.BoundingBox.Height);
            image.Mutate(ctx => ctx.Draw(penObject, rect));
            image.Mutate(ctx => ctx.DrawText(detectedObject.Tags[0].Name, font, brush, new PointF(rect.X, rect.Y)));
        }

        foreach (var detectedPerson in result.People.Values)
        {
            var rect = new RectangleF(detectedPerson.BoundingBox.X, detectedPerson.BoundingBox.Y, detectedPerson.BoundingBox.Width, detectedPerson.BoundingBox.Height);
            image.Mutate(ctx => ctx.Draw(penPerson, rect));
            image.Mutate(ctx => ctx.DrawText($"Person (Confidence: {detectedPerson.Confidence:F2})", font, brush, new PointF(rect.X, rect.Y)));
        }

        var outputFolder = _configuration["OutputFolder"];
        var outputFilePath = Path.Combine(outputFolder, "annotated_" + Path.GetFileName(imageFile));
        image.Save(outputFilePath);
    }


    private async Task<string> BackgroundForeground(string imageFile, string endpoint, string key, string outputFolder)
    {
        string apiVersion = "2023-02-01-preview";
        string mode = "backgroundRemoval";
        string url = $"{endpoint}/computervision/imageanalysis:segment?api-version={apiVersion}&mode={mode}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

        var imageBytes = await System.IO.File.ReadAllBytesAsync(imageFile);
        using var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadAsByteArrayAsync();
        var outputFile = Path.Combine(outputFolder, "background_removed_" + Path.GetFileName(imageFile));
        await System.IO.File.WriteAllBytesAsync(outputFile, responseData);

        return outputFile;
    }

}
