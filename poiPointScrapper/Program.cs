using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poiPointScrapper
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.Write("User: ");
            var user = Console.ReadLine();
            Console.Write("Password: ");
            var password = GetPassword();
            Console.Write("Type of POI format (eg. if you want get files like the one from https://www.poipoint.pl/pobierz-poi-opcje?poi=AD-Serwis&id=vwgpx&poi_id=170 type value of 'id' parameter, here would be: vwgpx)\nType of POI format: ");
            var type = Console.ReadLine();

            var httpClient = new PoiPointHttpClient();
            try
            {
                await httpClient.Login(user, password);
                Console.WriteLine("Logged successfully!");
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"Failed to login with user: {user}. Exiting...");
                Console.ReadKey();
                return;
            }

            var ids = (await httpClient.GetPoiPointsIds()).ToList();
            var idsCount = ids.Count;
            Console.WriteLine($"Detected {idsCount} POI types");

            var alreadyCreatedPOIs = Directory.GetDirectories("POI");
            Console.WriteLine($"Detected {alreadyCreatedPOIs} already fetched POIs. Skipping...");

            foreach (var (id, index) in ids.Skip(alreadyCreatedPOIs.Length).Select((value, i) => (value, i)))
            {
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                var (fileId, availableCountries) = await httpClient.GetGpxFileIdByPoiId(id, type);
                var gpxFilePath = await httpClient.DownloadGpxFile(fileId, availableCountries);
                stopwatch.Stop();

                var gpxFile = Path.GetFileName(gpxFilePath);
                var gpxDirectory = Path.GetDirectoryName(gpxFilePath);

                Console.WriteLine($"Downloaded {gpxFile} in {stopwatch.ElapsedMilliseconds}ms");

                stopwatch.Restart();
                await foreach (var icon in httpClient.DownloadIcons(id, gpxDirectory))
                {
                    stopwatch.Stop();
                    Console.WriteLine($"Downloaded {icon} in {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Restart();
                }

                var currentIndex = index + alreadyCreatedPOIs.Length + 1;
                Console.WriteLine($"Finished {currentIndex}/{idsCount} ({(double)currentIndex / idsCount:P})");
            }
        }

        private static string GetPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return password.ToString();
        }
    }
}
