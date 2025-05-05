using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Files.DataLake;
using BPSTest;

class Program
{
    static async Task Main(string[] args)
    {
        const bool useBps = true;

        // Number of entities (interaction types) being processed in parallel and number of files being downloaded in parallel for a particular entity.
        // For the entire app, the maximum number of possible parallel downloads is (maxParallelEntityProcessing * maxParallelDownloadsWithinEntity).
        const int maxParallelEntityProcessing = 5;
        const int maxParallelDownloadsWithinEntity = 10;

        Console.WriteLine("Utility for fetching all CI-J analytics data from MDL for a specific Dataverse organization using the Blob proxy service.");
        Console.WriteLine();

        string? dataverseUrl;
        string? downloadTargetDirectory;
        if (ParseArgs(args, out dataverseUrl, out downloadTargetDirectory))
        {
            Console.WriteLine("Getting BPS URL and token from Dataverse...");
            var dataverseClient = new DataverseClient(dataverseUrl!);
            var uri = await dataverseClient.GetMdlAccessUri(useBps);

            Console.WriteLine("URL and token retrieved successfully. Starting MDL data download.");
            var mdlClient = new DataLakeDirectoryClient(uri);

            var entities = new List<EntityType>();
            var manifestFile = mdlClient.GetFileClient("default.manifest.cdm.json");
            using (var manifestStream = manifestFile.OpenRead())
            using (var manifestDoc = JsonDocument.Parse(manifestStream))
            {
                var entitiesElement = manifestDoc.RootElement.GetProperty("entities");

                Console.WriteLine("Found the following entities:");
                foreach (var entity in entitiesElement.EnumerateArray())
                {
                    var name = entity.GetProperty("entityName").GetString();
                    if (entity.GetProperty("dataPartitions").GetArrayLength() != 0) { 
                        var location = entity.GetProperty("dataPartitions").EnumerateArray().First().GetProperty("location").GetString();
                        entities.Add(new EntityType(name!, location!));
                        Console.WriteLine($"{name}: {location}");
                    }
                    
                }

                Console.WriteLine();
            }

            var entitiesToDownload = entities.ToArray(); // Filter entity types here, if needed

            var stopwatch = Stopwatch.StartNew();
            var downloadedSize = 0L;
            object downloadedSizeLock = new object();
            await Parallel.ForEachAsync(
                entitiesToDownload,
                new ParallelOptions { MaxDegreeOfParallelism = maxParallelEntityProcessing },
                async (entity, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    Console.WriteLine($"Processing entity {entity.Name}");
                    var currentDownloadSize = await mdlClient.DownloadData(entity, downloadTargetDirectory!, maxParallelDownloadsWithinEntity);
                    lock (downloadedSizeLock)
                    {
                        downloadedSize += currentDownloadSize;
                    }
                });

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine("Completed successfully.");
            Console.WriteLine($"Total download: {downloadedSize / 1024}kB in {stopwatch.ElapsedMilliseconds}ms.");
        }
    }

    static bool ParseArgs(string[] args, out string? dataverseUrl, out string? downloadTargetDirectory)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("BPSTest.ADLS.exe {orgUrl} {localDir}");
            Console.WriteLine();
            Console.WriteLine("{orgUrl} - URL of the Dataverse organization to download MDL analytics data from, e.g.: https://myorg.crm.dynamics.com");
            Console.WriteLine("{localDir} - Path to the local directory to download data to, e.g.: C:\\AnalyticsData");
            dataverseUrl = null;
            downloadTargetDirectory = null;
            return false;
        }

        dataverseUrl = args[0];
        downloadTargetDirectory = args[1];
        return true;
    }
}
