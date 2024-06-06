using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using dotpaste;
using System.Timers;

const int TIMER_INTERVAL = 86400000; //-- 24h

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseStaticFiles();

var options = new Queue<string>(args);

string? ARGS_UPLOAD_PATH = null;

while (options.TryDequeue(out string? option))
{
    if (option.StartsWith("--uploads-path") || option.StartsWith("-u"))
        if (option.Contains('='))
            ARGS_UPLOAD_PATH = option.Split('=')[1];
        else
            ARGS_UPLOAD_PATH = options.Dequeue();
}

string DEFAULT_UPLOADS_PATH = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar + "uploads" + Path.DirectorySeparatorChar;
string UPLOADS_PATH = ARGS_UPLOAD_PATH ?? Environment.GetEnvironmentVariable("DOTPASTE_UPLOADS_PATH") ?? DEFAULT_UPLOADS_PATH;

try
{
    if (!Directory.Exists(UPLOADS_PATH))
        Directory.CreateDirectory(UPLOADS_PATH);
}
catch (Exception)
{
    app.Logger.LogError("Couldn't create uploads directory.");
    Environment.Exit(1);
}

string[] userAgents = [
    "Gecko",
    "Mozilla",
    "Presto",
    "Firefox",
    "EdgeHTML",
    "Safari",
    "Chromium"
];

string[] acceptedContentTypes = [
    "text/plain",
    "text/html",
    "application/json",
    "application/javascript",
    "application/xml"
];

string[] markupLanguages = [
    "markup",
    "html",
    "xml",
    "svg",
    "rss"
];

var fileID = new ThreadSafeCounter();

var resetTimer = new System.Timers.Timer(TIMER_INTERVAL);
resetTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
{
    app.Logger.LogInformation("Started clean operation on '{}'", UPLOADS_PATH);
    var directory = new DirectoryInfo(UPLOADS_PATH);

    foreach(var file in directory.EnumerateFiles())
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

    if (userAgents.Any(check => userAgent.Contains(check, StringComparison.InvariantCultureIgnoreCase)))
        return Results.File("index.html", "text/html");

    return Results.File("index.txt", "text/plain");
}).WithName("Index");

app.MapPost("/", (HttpRequest request) => {
    fileID.Increment();
    var currentFileURL = WebEncoders.Base64UrlEncode(BitConverter.GetBytes(fileID.Value));
    var currentURL = $"{request.Scheme}://{request.Host.Value}/content/";

    if (request.HasFormContentType)
    {
        if(request.Form.TryGetValue("content", out var content))
        {
            File.WriteAllText(UPLOADS_PATH + currentFileURL, request.Form["content"]);
            return Results.Text(currentURL + currentFileURL);
        }

        if(request.Form.Files.Any() && request.Form.Files[0].Name == "content" && acceptedContentTypes.Any(ct => request.Form.Files[0].ContentType == ct))
        {
            FileStream fileStream = new(UPLOADS_PATH + currentFileURL, FileMode.CreateNew);
            request.Form.Files[0].OpenReadStream().CopyTo(fileStream);
            fileStream.Dispose();
            return Results.Text(currentURL + currentFileURL);
        }
    }

    if(acceptedContentTypes.Any(ct => request.ContentType == ct))
    {
        FileStream fileStream = new(UPLOADS_PATH + currentFileURL, FileMode.CreateNew);
        request.BodyReader.AsStream().CopyTo(fileStream);
        fileStream.Dispose();
        return Results.Text(currentURL + currentFileURL);
    }

    return Results.BadRequest("Unsupported content type.");
}).DisableAntiforgery().WithName("Post");

app.MapGet("/content/{file}", (string file, [FromQuery(Name = "lang")] string? lang) => {
    try
    {
        if(lang == null)
            return Results.File(UPLOADS_PATH + file, "text/plain");

        if (markupLanguages.Any(ml => lang == ml))
            return Results.Text(View.TemplateHTML(File.ReadAllText(UPLOADS_PATH + file)), "text/html");

        return Results.Text(View.Template(File.ReadAllText(UPLOADS_PATH + file)), "text/html");
    } catch (FileNotFoundException) {
        return Results.NotFound();
    }
}).WithName("GetFile");

app.Run();