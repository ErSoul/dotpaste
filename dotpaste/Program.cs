using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Timers;
using dotpaste;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Startup(out string UPLOADS_PATH, args);

var fileID = new ThreadSafeCounter();

var resetTimer = new System.Timers.Timer(Properties.TIMER_INTERVAL);
resetTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
{
    app.Logger.LogInformation("Started clean operation on '{}'", UPLOADS_PATH);
    var directory = new DirectoryInfo(UPLOADS_PATH);

    foreach (var file in directory.EnumerateFiles())
    {
        app.Logger.LogDebug("Deleting file '{}'", file.FullName);
        file.Delete();
        app.Logger.LogDebug("Deleted file '{}'", file.FullName);
    }

    fileID = new ThreadSafeCounter();
    app.Logger.LogInformation("Completed clean operation on '{}'", UPLOADS_PATH);
};
resetTimer.Start();

app.MapGet("/", (HttpContext context) =>
{
    var userAgent = context.Request.Headers.UserAgent.ToString();

    if (Properties.UserAgents.Any(check => userAgent.Contains(check, StringComparison.InvariantCultureIgnoreCase)))
        return Results.File("index.html", "text/html");

    return Results.File("index.txt", "text/plain");
}).WithName("Index");

app.MapPost("/", (HttpRequest request) =>
{
    fileID.Increment();
    var currentFileURL = WebEncoders.Base64UrlEncode(BitConverter.GetBytes(fileID.Value));
    var currentURL = $"{request.Scheme}://{request.Host.Value}/content/";

    if (request.HasFormContentType)
    {
        if (request.Form.TryGetValue("content", out var content))
        {
            File.WriteAllText(UPLOADS_PATH + currentFileURL, request.Form["content"]);
            app.HandleFileDeletion(UPLOADS_PATH + currentFileURL);
            return Properties.UserAgents.Any(check => request.Headers.UserAgent.ToString().Contains(check, StringComparison.InvariantCultureIgnoreCase)) ?
                    Results.Redirect(currentURL + currentFileURL) :
                    Results.Text(currentURL + currentFileURL);
        }

        if (request.Form.Files.Any() && request.Form.Files[0].Name == "content" && Properties.AcceptedContentTypes.Any(ct => request.Form.Files[0].ContentType == ct))
        {
            FileStream fileStream = new(UPLOADS_PATH + currentFileURL, FileMode.CreateNew);
            request.Form.Files[0].OpenReadStream().CopyTo(fileStream);
            fileStream.Dispose();
            app.HandleFileDeletion(UPLOADS_PATH + currentFileURL);
            return Properties.UserAgents.Any(check => request.Headers.UserAgent.ToString().Contains(check, StringComparison.InvariantCultureIgnoreCase)) ?
                    Results.Redirect(currentURL + currentFileURL) :
                    Results.Text(currentURL + currentFileURL);
        }
    }

    if (Properties.AcceptedContentTypes.Any(ct => request.ContentType == ct))
    {
        FileStream fileStream = new(UPLOADS_PATH + currentFileURL, FileMode.CreateNew);
        request.BodyReader.AsStream().CopyTo(fileStream);
        fileStream.Dispose();
        app.HandleFileDeletion(UPLOADS_PATH + currentFileURL);
        return Properties.UserAgents.Any(check => request.Headers.UserAgent.ToString().Contains(check, StringComparison.InvariantCultureIgnoreCase)) ?
                Results.Redirect(currentURL + currentFileURL) :
                Results.Text(currentURL + currentFileURL);
    }

    return Results.BadRequest("Unsupported content type.");
}).DisableAntiforgery().WithName("Post");

app.MapGet("/content/{file}", (string file, [FromQuery(Name = "lang")] string? lang) =>
{
    if (!File.Exists(UPLOADS_PATH + file))
        return Results.NotFound();

    if (lang == null)
        return Results.File(UPLOADS_PATH + file, "text/plain");

    return Results.Text(View.TemplateHTML(File.ReadAllText(UPLOADS_PATH + file)), "text/html");
}).WithName("GetFile");

app.Run();

public partial class Program;