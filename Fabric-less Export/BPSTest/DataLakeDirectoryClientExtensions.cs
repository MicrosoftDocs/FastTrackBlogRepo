namespace BPSTest
{
    using System.Text.Json;
    using Azure.Storage.Files.DataLake;
    using Azure.Storage.Files.DataLake.Models;

    public static class DataLakeDirectoryClientExtensions
    {
        public static DataLakeFileClient GetFileClient(this DataLakeDirectoryClient client, PathItem path)
        {
            string localPath;
            if (path.Name.StartsWith(client.Path))
            {
                localPath = path.Name.Substring(client.Path.Length);
            }
            else
            {
                var clientPathWithoutContainer = client.Path.Split('/', 2)[1];
                if (path.Name.StartsWith(clientPathWithoutContainer))
                {
                    localPath = path.Name.Substring(clientPathWithoutContainer.Length);
                }
                else
                {
                    throw new ArgumentException("The path provided is not located in the specified directory.");
                }
            }

            if (localPath.StartsWith("/"))
            {
                localPath = localPath.Substring(1);
            }

            return client.GetFileClient(localPath);
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
