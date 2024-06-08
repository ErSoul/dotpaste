using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace dotpaste.Tests
{
    public class Index : IClassFixture<WebServer<Program>>
    {
        private readonly WebServer<Program> _server;
        private readonly HttpClient httpClient;

        public Index(WebServer<Program> server)
        {
            _server = server;
            httpClient = _server.CreateClient();
        }

        [Fact]
        public async Task IndexShouldReturnText()
        {
            var response = await httpClient.GetAsync("/");

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);

            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("cat my_file | curl -H 'Content-Type: text/plain' --data-binary @- http://mydomain[:$PORT]", content);
        }

        [Fact]
        public async Task IndexShouldReturnHTML()
        {
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/237.36 (KHTML, like Gecko) Chrome/99.0.0.0 Safari/34.3");
            var response = await httpClient.GetAsync("/");

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);

            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("<textarea", content);
        }

        [Fact]
        public async Task UploadContentShouldReturnALink()
        {
            var response = await httpClient.PostAsync("/", new FormUrlEncodedContent([new KeyValuePair<string, string>("content", "solo envio")]));

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();

            Assert.True(Uri.IsWellFormedUriString(content, UriKind.Absolute));
        }

        [Fact]
        public async Task UploadContentShouldRedirect()
        {
            var content = "this should be the request content to be uploaded";
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Safari");
            var response = await httpClient.PostAsync("/", new StringContent(content, MediaTypeHeaderValue.Parse("text/plain")));

            Assert.NotNull(response);
            Assert.Equal(content, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UploadFileShouldRedirect()
        {
            var dummyFileContent = "this should be the request content to be uploaded";
            var fileMock = new MemoryStream(Encoding.ASCII.GetBytes(dummyFileContent));
            var fileStream = new StreamContent(fileMock);
            fileStream.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

            var content = new MultipartFormDataContent
            {
                { fileStream, "content", "nombre.txt" }
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla");
            var response = await httpClient.PostAsync("/", content);

            Assert.NotNull(response);
            Assert.Equal("this should be the request content to be uploaded", await response.Content.ReadAsStringAsync());

            content.Dispose();
        }

        [Fact]
        public async Task UploadFileShouldReturnBadRequest()
        {
            var content = "this should be the request content to be uploaded";
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Safari");
            var response = await httpClient.PostAsync("/", new StringContent(content, MediaTypeHeaderValue.Parse("*/*")));

            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFileContentInHTML()
        {
            var fileContent = "Some random innecessary text";
            var fileName = "1234file";

            using (var fileStream = new FileStream(Path.Combine(Environment.CurrentDirectory, "uploads") + Path.DirectorySeparatorChar + fileName, FileMode.CreateNew))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine(fileContent);
            }

            var response = await httpClient.GetAsync($"/content/{fileName}?lang=text");

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.Contains(fileContent, responseContent);
            Assert.Contains("<link href=\"/css/prism.css\" rel=\"stylesheet\" />", responseContent);
        }

        [Fact]
        public async Task GetFileContentShouldReturnNotFound()
        {
            var fileName = "whateveritis";
            var response = await httpClient.GetAsync($"/content/{fileName}?lang=text");

            Assert.NotNull(response);
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
