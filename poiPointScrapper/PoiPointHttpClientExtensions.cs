using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace poiPointScrapper
{
    public static class PoiPointHttpClientExtensions
    {
        private static readonly Regex FileIdRegex = new Regex("\\$\\.get\\('checkpayment\\.php',\\{\\ cmd:\\ \"uncheck\",\\ id:\\ me\\.id,\\ login:\\ \"\",\\ ctr:\\ \"([a-zA-Z0-9]+)\"}");

        public static async Task<IEnumerable<int>> GetPoiPointsIds(this PoiPointHttpClient httpClient)
        {
            var response = await httpClient.GetStringAsync("https://www.poipoint.pl/poi_all");
            var document = new HtmlDocument();
            document.LoadHtml(response);

            var a = document.DocumentNode
                .Descendants("a");
            var validA = a
                .Where(x => x.InnerText == "Pobierz POI");
            var attributeValues = validA
                .Select(x => x.GetAttributeValue("href", null))
                .Where(x => x != null);
            var stringIds = attributeValues
                .Select(x => x.Substring(x.LastIndexOf('=') + 1));
            var ids = stringIds
                .Select(int.Parse)
                .ToList();

            return ids;
        }

        public static async Task<(string fileId, IEnumerable<string> availableCountries)> GetGpxFileIdByPoiId(this PoiPointHttpClient httpClient, int poiId, string type)
        {
            var response = await httpClient.GetStringAsync($"https://www.poipoint.pl/pobierz-poi-opcje?id={type}&poi_id={poiId}");
            var document = new HtmlDocument();
            document.LoadHtml(response);

            var countriesCheckboxes = document.DocumentNode
                .Descendants("input")
                .Where(x => x.HasClass("css-checkbox"));
            var availableCountriesCheckboxes = countriesCheckboxes
                .Where(x => x.Attributes.All(y => y.Name != "disabled"));
            var availableCountries = availableCountriesCheckboxes
                .Select(x => x.GetAttributeValue("name", null))
                .Where(x => x != null)
                .ToArray();

            var fileIdMatch = FileIdRegex.Match(response);
            var fileId = fileIdMatch.Groups.Count == 2 
                ? fileIdMatch.Groups[1].Value 
                : null;

            return (fileId, availableCountries);
        }

        public static async Task<string> DownloadGpxFile(this PoiPointHttpClient httpClient, string fileId,
            IEnumerable<string> availableCountries)
        {
            foreach (var country in availableCountries)
            {
                using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://www.poipoint.pl/checkpayment.php?cmd=country&id={country}&ctr={fileId}");
                request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
                request.Headers.TryAddWithoutValidation("Accept", "*/*");
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36");
                request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
                request.Headers.TryAddWithoutValidation("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7");

                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"FAILED! FileId: {fileId}, country: {country}, availableCountries: {string.Join(", ", availableCountries)}");
                }
            }

            var fileResponse = await httpClient.GetAsync($"https://www.poipoint.pl/poi_download/poi_download.php?poi={fileId}");
            var file = await fileResponse.Content.ReadAsByteArrayAsync();
            var fileName = fileResponse.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
            var directory = "POI/" + fileName.Substring(0, fileName.IndexOf(".", StringComparison.Ordinal));
            directory = directory.Trim();
            var filePath = $"{directory}/{fileName}";

            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(filePath, file);

            return filePath;
        }

        public static async IAsyncEnumerable<string> DownloadIcons(this PoiPointHttpClient httpClient, int poiId, string directory = null)
        {
            var response = await httpClient.GetStringAsync($"https://www.poipoint.pl/pobierz-poi-ikony?poi_id={poiId}");
            var document = new HtmlDocument();
            document.LoadHtml(response);

            var forms = document.DocumentNode
                .Descendants("form");
            var actions = forms
                .Select(x => x.GetAttributeValue("action", null))
                .Where(x => x != null);
            var stringIds = actions
                .Select(x => x.Substring(x.LastIndexOf('=') + 1));
            var ids = stringIds
                .Select(int.Parse)
                .ToList();

            foreach (var (iconId, index) in ids.Select((value, i) => (value, i)))
            {
                var fileResponse = await httpClient.GetAsync($"https://www.poipoint.pl/poi_icon/icon_poi_PNG_62.php?id={iconId}");
                var file = await fileResponse.Content.ReadAsByteArrayAsync();
                var fileName = fileResponse.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
                directory ??= "POI/" + fileName.Substring(0, fileName.IndexOf(".", StringComparison.Ordinal));

                Directory.CreateDirectory(directory);
                if (File.Exists($"{directory}/{fileName}"))
                {
                    fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{index}{Path.GetExtension(fileName)}";
                }
                await File.WriteAllBytesAsync($"{directory}/{fileName}", file);

                yield return fileName;
            }
        }
    }
}
