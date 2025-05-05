using System.Diagnostics;
using Azure.Storage.Files.DataLake;

namespace BPSTest
{
    public static class EntityDownloader
    {
        public static async Task<long> DownloadData(this DataLakeDirectoryClient client, EntityType entityType, string targetDirectory, int maxDegreeOfParallelism = 1)
        {
            var stopwatch = Stopwatch.StartNew();
            var downloadedSize = 0L;
            var downloadedSizeLock = new object();

            var directoryClient = client.GetSubDirectoryClient(entityType.Location);
            if (!directoryClient.ExistsBpsFriendly())
            {
                Console.WriteLine($"Entity directory {entityType.Location} was not found.");
                return 0L;
            }

            var paths = directoryClient.GetPathsBpsFriendlyAsync();

            await Parallel.ForEachAsync(
                paths,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                async (path, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var localPath = Path.Combine(targetDirectory, path.Name);
                    if (path.IsDirectory.HasValue && path.IsDirectory.Value)
                    {
                        // Create directories (including empty ones).
                        if (!Directory.Exists(localPath))
                        {
                            Directory.CreateDirectory(localPath);
                        }
                    }
                    else
                    {
                        // Create the containing local directory and download the file.
                        var localDir = Path.GetDirectoryName(localPath)!;
                        if (!Directory.Exists(localDir))
                        {
                            Directory.CreateDirectory(localDir);
                        }

                        if (File.Exists(localPath))
                        {
                            if (path.LastModified.HasValue && File.GetLastWriteTime(localPath) >= path.LastModified.Value)
                            {
                                Console.WriteLine("Skipping download of file {0} because it's not older than the file in MDL.", path.Name);
                                return;
                            }
                            else
                            {
                                Console.WriteLine("The file {0} exists locally, but it's older than its MDL counterpart.", path.Name);
                            }
                        }

                        Console.WriteLine($"Downloading {path.Name}");
                        var fileClient = directoryClient.GetFileClient(path);

                        using (var localStream = File.OpenWrite(localPath))
                        {
                            await fileClient.ReadToAsync(localStream);
                        }

                        lock (downloadedSizeLock)
                        {
                            downloadedSize += path.ContentLength!.Value;
                        }

                        if (path.LastModified.HasValue)
                        {
                            File.SetLastWriteTime(localPath, path.LastModified.Value);
                        }
                    }
                });

            stopwatch.Stop();
            Console.WriteLine($"Downloaded {downloadedSize / 1024}kB in {stopwatch.ElapsedMilliseconds}ms.");

            return downloadedSize;
        }
    }
}
