using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using dotpaste;

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

int fileID = 0; //-- TODO: make thread-safe

app.MapGet("/", (HttpContext context) =>
{
    var userAgent = context.Request.Headers.UserAgent.ToString();

    if (userAgents.Any(check => userAgent.Contains(check, StringComparison.InvariantCultureIgnoreCase)))
        return Results.File("index.html", "text/html");

    return Results.File("index.txt", "text/plain");
}).WithName("Index");

app.MapPost("/", (HttpRequest request) => {
    var currentFileURL = WebEncoders.Base64UrlEncode(BitConverter.GetBytes(++fileID));
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
            FileStream fileStream = new(UPLOADS_PATH + currentFileURL, FileMode.Create);
            request.Form.Files[0].OpenReadStream().CopyTo(fileStream);
            fileStream.Dispose();
            return Results.Text(currentURL + currentFileURL);
        }
    }

    if(acceptedContentTypes.Any(ct => request.ContentType == ct))
    {
        FileStream fileStream = new(UPLOADS_PATH + currentFileURL, FileMode.Create);
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

        return Results.Text(View.Template(File.ReadAllText(UPLOADS_PATH + file)), "text/html");
    } catch (FileNotFoundException) {
        return Results.NotFound();
    }
}).WithName("GetFile");

app.Run();