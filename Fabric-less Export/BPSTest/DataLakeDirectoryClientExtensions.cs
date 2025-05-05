namespace BPSTest
{
    using Azure.Storage.Files.DataLake;
    using Azure.Storage.Files.DataLake.Models;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Web;

    public static class DataLakeDirectoryClientExtensions
    {
        public static DataLakeFileClient GetFileClient(this DataLakeDirectoryClient client, PathItem path)
        {
            if (!path.Name.StartsWith(client.Path))
            {
                throw new ArgumentException("The path provided is not located in the specified directory.");
            }

            return client.GetFileClient(path.Name.Substring(client.Path.Length));
        }

        public static DataLakeFileClient GetFileClient(this DataLakeDirectoryClient client, BpsPathItem path)
        {
            // ADLS path convention
            if (path.Name.StartsWith(client.Path))
            {
                return client.GetFileClient(path.Name.Substring(client.Path.Length));
            }

            // BPS path convention
            var pathWithoutContainer = client.Path.Split('/', 2)[1];
            if (!pathWithoutContainer.EndsWith('/'))
            {
                pathWithoutContainer += "/";
            }

            if (!path.Name.StartsWith(pathWithoutContainer))
            {
                throw new ArgumentException("The path provided is not located in the specified directory.");
            }

            return client.GetFileClient(path.Name.Substring(pathWithoutContainer.Length));
        }

        public static async IAsyncEnumerable<BpsPathItem> GetPathsBpsFriendlyAsync(this DataLakeDirectoryClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            var pathParts = client.Uri.LocalPath.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
            if (!pathParts[0].Equals("storage", StringComparison.InvariantCultureIgnoreCase))
            {
                // The URL does not match the BPS URL convention. Assume ADLSv2 is targeted directly.
                var paths = client.GetPaths();
                foreach (var p in paths)
                {
                    yield return new BpsPathItem { Name = p.Name, IsDirectory = p.IsDirectory, ContentLength = p.ContentLength };
                }
            }

            if (pathParts.Length != 3)
            {
                throw new ArgumentException("The path of the BPS URL must contain 'storage', container name and directory name; separated by slashes (/).");
            }

            var baseUri = new Uri(client.Uri.GetComponents(UriComponents.Scheme | UriComponents.HostAndPort, UriFormat.Unescaped));
            var path = pathParts[0] + "/" + pathParts[1];
            var query = client.Uri.Query + "&recursive=false&resource=filesystem&directory=" + pathParts[2];
            var alteredUri = new Uri(baseUri, path + query);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new HttpRequestMessage(HttpMethod.Get, alteredUri);

            while (true)
            {
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
                {
                    var root = doc.RootElement;

                    foreach (var pathNode in root.GetProperty("paths").EnumerateArray())
                    {
                        JsonElement isDirectoryNode;
                        JsonElement contentLengthNode;
                        JsonElement lastModifiedNode;
                        DateTime lastModified;
                        yield return new BpsPathItem
                        {
                            Name = pathNode.GetProperty("name").GetString()!,
                            IsDirectory = pathNode.TryGetProperty("isDirectory", out isDirectoryNode) ? bool.Parse(isDirectoryNode.GetString()!) : null,
                            ContentLength = pathNode.TryGetProperty("contentLength", out contentLengthNode) ? long.Parse(contentLengthNode.GetString()!) : null,
                            LastModified = pathNode.TryGetProperty("lastModified", out lastModifiedNode) && DateTime.TryParse(lastModifiedNode.GetString(), out lastModified) ? lastModified : null
                        };
                    }
                }

                if (response.Headers.Contains("x-ms-continuation"))
                {
                    request = new HttpRequestMessage(HttpMethod.Get, alteredUri + "&continuation=" + HttpUtility.UrlEncode(response.Headers.GetValues("x-ms-continuation").Single()));
                }
                else
                {
                    break;
                }
            }
        }

        public static bool ExistsBpsFriendly(this DataLakeDirectoryClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            try
            {
                return client.Exists();
            }
            catch (JsonException) // For a more specific exception catch: catch (JsonException e) when (e.Message.StartsWith("The input does not contain any JSON tokens."))
            {
                return false;
            }
        }
    }
}
