using System.Timers;

namespace dotpaste
{
    public static class Properties
    {
        public static readonly string[] UserAgents = [
            "Gecko",
            "Mozilla",
            "Presto",
            "Firefox",
            "EdgeHTML",
            "Safari",
            "Chromium"
        ];

        public static readonly string[] AcceptedContentTypes = [
            "text/plain",
            "text/html",
            "application/json",
            "application/javascript",
            "application/xml"
        ];

        public static readonly double TIMER_INTERVAL = (DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds;
        private const short DELETE_FILE_INTERVAL = 1;

        private static readonly string DEFAULT_UPLOADS_PATH = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar + "uploads";
        public static WebApplication Startup(this WebApplication app, out string UPLOADS_PATH, params string[] args)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });
            app.UseStaticFiles();

            var options = new Queue<string>(args);

            string? ARGS_UPLOAD_PATH = null;

            while (options.TryDequeue(out string? option))
            {
                if (option.StartsWith("--uploads-path") || option.StartsWith("-u"))
                    if (option.Contains('='))
                        ARGS_UPLOAD_PATH = option.Split('=')[1];
                    else
                        options.TryDequeue(out ARGS_UPLOAD_PATH);
            }

            UPLOADS_PATH = ARGS_UPLOAD_PATH ?? Environment.GetEnvironmentVariable("DOTPASTE_UPLOADS_PATH") ?? DEFAULT_UPLOADS_PATH;

            try
            {
                if (!Directory.Exists(UPLOADS_PATH))
                    Directory.CreateDirectory(UPLOADS_PATH);

                if (!Path.EndsInDirectorySeparator(UPLOADS_PATH))
                    UPLOADS_PATH += Path.DirectorySeparatorChar;
            }
            catch (Exception)
            {
                app.Logger.LogError("Couldn't create uploads directory.");
                Environment.Exit(1);
            }

            return app;
        }

        public static void HandleFileDeletion(this WebApplication app, string filePath)
        {
            var deleteFileTimer = new System.Timers.Timer((DateTime.Now.AddHours(DELETE_FILE_INTERVAL) - DateTime.Now).TotalMilliseconds);
            deleteFileTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
            {
                if (File.Exists(filePath))   //-- Check if file exists 'cause the main timer might have clean it up.
                {
                    app.Logger.LogDebug("Deleting file '{}'", filePath);
                    File.Delete(filePath);
                    app.Logger.LogDebug("Deleted file '{}'", filePath);
                }
            };
            deleteFileTimer.Start();
        }
    }
}
