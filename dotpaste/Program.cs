using System.Timers;
using dotpaste.Endpoints;
using dotpaste.Utils;

var builder = WebApplication.CreateBuilder(args);
var Props = new Constants(args);
builder.Services.AddSingleton(Props);
var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});
app.UseStaticFiles();

var fileID = new ThreadSafeCounter();

var resetTimer = new System.Timers.Timer(Props.TIMER_INTERVAL);
resetTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
{
    app.Logger.LogInformation("Started clean operation on '{UPLOADS_PATH}'", Props.UPLOADS_PATH);
    var directory = new DirectoryInfo(Props.UPLOADS_PATH);

    foreach (var file in directory.EnumerateFiles())
    {
        app.Logger.LogDebug("Deleting file '{}'", file.FullName);
        file.Delete();
        app.Logger.LogDebug("Deleted file '{}'", file.FullName);
    }

    fileID = new ThreadSafeCounter();
    app.Logger.LogInformation("Completed clean operation on '{UPLOADS_PATH}'", Props.UPLOADS_PATH);
};
resetTimer.Start();

app.MapIndex();
app.MapPastes(fileID);

app.Run();

public partial class Program;