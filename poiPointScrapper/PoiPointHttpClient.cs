using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace poiPointScrapper
{
    public class PoiPointHttpClient : HttpClient
    {
        private static readonly HttpClientHandler Handler = new HttpClientHandler
        {
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All,
            ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true
        };

        public PoiPointHttpClient(): base(Handler)
        {

        }

        public async Task Login(string email, string password)
        {
            using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.poipoint.pl/login");
            request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
            request.Headers.TryAddWithoutValidation("Cache-Control", "max-age=0");
            request.Headers.TryAddWithoutValidation("Origin", "https://www.poipoint.pl");
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
            request.Headers.TryAddWithoutValidation("Referer", "https://www.poipoint.pl/login");
            request.Headers.TryAddWithoutValidation("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7");

            request.Content = new StringContent($"usr_email={HttpUtility.UrlEncode(email)}&passwd={password}&referer=https%3A%2F%2Fwww.poipoint.pl%2F&remember=1&doLogin=Zaloguj");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

            var response = await SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!result.Contains("Wyloguj"))
            {
                throw new ArgumentException();
            }
        }
    }
}
