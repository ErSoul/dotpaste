using System.Timers;

namespace dotpaste.Utils
{
    public static class Action
    {
        public static void HandleFileDeletion(this WebApplication app, string filePath)
        {
            var deleteFileTimer = new System.Timers.Timer((DateTime.Now.AddHours(Constants.DELETE_FILE_INTERVAL) - DateTime.Now).TotalMilliseconds);
            deleteFileTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
            {
                if (File.Exists(filePath))   //-- Check if file exists 'cause the main timer might have clean it up.
                {
                    app.Logger.LogDebug("Deleting file '{file}'", filePath);
                    File.Delete(filePath);
                    app.Logger.LogDebug("Deleted file '{file}'", filePath);
                }
            };
            deleteFileTimer.Start();
        }
    }
}
