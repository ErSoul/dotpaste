using Microsoft.AspNetCore.Mvc.Testing;

namespace dotpaste.Tests
{
    public class WebServer<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public WebServer()
        {
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "uploads")))
                Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "uploads"), true);
        }
    }
}