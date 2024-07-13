using dotpaste.Utils;
using Microsoft.AspNetCore.Mvc;

namespace dotpaste.Endpoints
{
    public static class GetIndex
    {
        public static void MapIndex(this WebApplication app)
        {
            app.MapGet("/", (HttpContext context, [FromServices] Constants Props) =>
            {
                var userAgent = context.Request.Headers.UserAgent.ToString();

                if (Array.Exists(Props.UserAgents, check => userAgent.Contains(check, StringComparison.InvariantCultureIgnoreCase)))
                    return Results.File("index.html", "text/html");

                return Results.File("index.txt", "text/plain");
            }).WithName("Index");
        }
    }
}
