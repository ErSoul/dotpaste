namespace dotpaste.Utils
{
    public class Constants
    {
        public readonly string[] UserAgents = [
            "Gecko",
            "Mozilla",
            "Presto",
            "Firefox",
            "EdgeHTML",
            "Safari",
            "Chromium"
        ];

        public readonly string[] AcceptedContentTypes = [
            "text/plain",
            "text/html",
            "application/json",
            "application/javascript",
            "application/xml"
        ];

        public readonly double TIMER_INTERVAL = (DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds;
        public const short DELETE_FILE_INTERVAL = 1;

        private readonly string DEFAULT_UPLOADS_PATH = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar + "uploads";
        public readonly string UPLOADS_PATH;

        public Constants(string[] args)
        {
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
                Console.Error.WriteLine("Couldn't create uploads directory.");
                Environment.Exit(1);
            }
        }
    }
}
