using dotpaste.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace dotpaste.Endpoints
{
    public static class Paste
    {
        public static void MapPastes(this WebApplication app, ThreadSafeCounter fileID)
        {
            app.MapPost("/", (HttpRequest request, [FromServices] Constants Props) =>
            {
                fileID.Increment();
                var currentFileURL = WebEncoders.Base64UrlEncode(BitConverter.GetBytes(fileID.Value));
                var currentURL = $"{request.Scheme}://{request.Host.Value}/content/";

                if (request.HasFormContentType)
                {
                    if (request.Form.TryGetValue("content", out var content))
                    {
                        File.WriteAllText(Props.UPLOADS_PATH + currentFileURL, request.Form["content"]);
                        app.HandleFileDeletion(Props.UPLOADS_PATH + currentFileURL);
                        return Array.Exists(Props.UserAgents, check => request.Headers.UserAgent.ToString().Contains(check, StringComparison.InvariantCultureIgnoreCase)) ?
                                Results.Redirect(currentURL + currentFileURL) :
                                Results.Text(currentURL + currentFileURL + '\n');
                    }

                    if (request.Form.Files.Any() && request.Form.Files[0].Name == "content" && Array.Exists(Props.AcceptedContentTypes, ct => request.Form.Files[0].ContentType == ct))
                    {
                        FileStream fileStream = new(Props.UPLOADS_PATH + currentFileURL, FileMode.CreateNew);
                        request.Form.Files[0].OpenReadStream().CopyTo(fileStream);
                        fileStream.Dispose();
                        app.HandleFileDeletion(Props.UPLOADS_PATH + currentFileURL);
                        return Array.Exists(Props.UserAgents, check => request.Headers.UserAgent.ToString().Contains(check, StringComparison.InvariantCultureIgnoreCase)) ?
                                Results.Redirect(currentURL + currentFileURL) :
                                Results.Text(currentURL + currentFileURL + '\n');
                    }
                }

                if (Array.Exists(Props.AcceptedContentTypes, ct => request.ContentType == ct))
                {
                    FileStream fileStream = new(Props.UPLOADS_PATH + currentFileURL, FileMode.CreateNew);
                    request.BodyReader.AsStream().CopyTo(fileStream);
                    fileStream.Dispose();
                    app.HandleFileDeletion(Props.UPLOADS_PATH + currentFileURL);
                    return Array.Exists(Props.UserAgents, check => request.Headers.UserAgent.ToString().Contains(check, StringComparison.InvariantCultureIgnoreCase)) ?
                            Results.Redirect(currentURL + currentFileURL) :
                            Results.Text(currentURL + currentFileURL + '\n');
                }

                return Results.BadRequest("Unsupported content type.");
            }).DisableAntiforgery().WithName("Post");

            app.MapGet("/content/{file}", (string file, [FromQuery(Name = "lang")] string? lang, [FromServices] Constants Props) =>
            {
                if (!File.Exists(Props.UPLOADS_PATH + file))
                    return Results.NotFound();

                if (lang == null)
                    return Results.File(Props.UPLOADS_PATH + file, "text/plain");

                return Results.Text(View.TemplateHTML(File.ReadAllText(Props.UPLOADS_PATH + file)), "text/html");
            }).WithName("GetFile");
        }
    }
}
