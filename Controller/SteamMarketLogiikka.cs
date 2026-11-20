using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using HtmlDoc = HtmlAgilityPack.HtmlDocument;

namespace RustSkinScanner.Controller
{
    public class SteamMarketLogiikka
    {
        private readonly HttpClient _client;
        private readonly IMemoryCache _cache;
        private const int AppId = 252490;
        private const double Fee = 0.15;

        // Cache itemNameId:lle (vältetään turhia listings-pyyntöjä)
        private readonly Dictionary<string, long> _itemNameIdCache = new();

        // Staattinen dictionary tunnetuille ID:ille (voi laajentaa)
        private static readonly Dictionary<string, long> KnownItemIds = new()
        {
            { "Whiteout Facemask", 176238194 },
            { "Heat Seeker LR", 176539417 }
            // Lisää tähän uusia kun saat ID:t selville
        };

        public SteamMarketLogiikka(IMemoryCache cache)
        {
            _cache = cache;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                UseCookies = false // Emme käytä istuntoa tässä versiossa
            };

            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://steamcommunity.com/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Browser-mäiset headerit – Steam estää headless-kutsut ilman näitä
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            _client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            _client.DefaultRequestHeaders.Referrer = new Uri("https://steamcommunity.com/market/");
        }

        /// <summary>
        /// Hakee buy/sell-orderit histogram-endpointista
        /// </summary>
        public async Task<(decimal HighestBuyOrder, decimal LowestSell)> GetHistogramAsync(string marketHashName, long itemNameId)
        {
            if (itemNameId <= 0)
            {
                Debug.WriteLine($"Virhe: itemNameId = {itemNameId} (ei kelvollinen)");
                return (0m, 0m);
            }

            string url = $"market/itemordershistogram?country=FI&language=english&currency=3&item_nameid={itemNameId}&two_factor=0&norender=1";

            string jsonText;
            try
            {
                jsonText = await GetCachedAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Histogram-pyyntö epäonnistui {marketHashName}: {ex.Message}");
                return (0m, 0m);
            }

            try
            {
                using var json = JsonDocument.Parse(jsonText);
                var root = json.RootElement;

                decimal highestBuy = 0m;
                decimal lowestSell = 0m;

                // Buy order graph (ensimmäinen arvo = korkein buy)
                if (root.TryGetProperty("buy_order_graph", out var buyElem) &&
                    buyElem.ValueKind == JsonValueKind.Array &&
                    buyElem.GetArrayLength() > 0)
                {
                    var firstBuy = buyElem[0];
                    if (firstBuy.ValueKind == JsonValueKind.Array && firstBuy.GetArrayLength() >= 1)
                    {
                        highestBuy = firstBuy[0].TryGetDecimal(out var val) ? val : 0m;
                    }
                }

                // Sell order graph (ensimmäinen arvo = alin sell)
                if (root.TryGetProperty("sell_order_graph", out var sellElem) &&
                    sellElem.ValueKind == JsonValueKind.Array &&
                    sellElem.GetArrayLength() > 0)
                {
                    var firstSell = sellElem[0];
                    if (firstSell.ValueKind == JsonValueKind.Array && firstSell.GetArrayLength() >= 1)
                    {
                        lowestSell = firstSell[0].TryGetDecimal(out var val) ? val : 0m;
                    }
                }

                Debug.WriteLine($"Histogram {marketHashName}: Buy={highestBuy:F2}, Sell={lowestSell:F2}");
                return (highestBuy, lowestSell);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Histogram-parsinta epäonnistui {marketHashName}: {ex.Message}");
                return (0m, 0m);
            }
        }

        /// <summary>
        /// Hakee hintahistorian ja laskee keskiarvot + 24h vol
        /// </summary>
        public async Task<(decimal Avg7d, decimal Avg30d, int Volume24h)> GetPriceHistoryAsync(string marketHashName)
        {
            string encodedName = Uri.EscapeDataString(marketHashName);
            string url = $"market/pricehistory/?appid={AppId}&market_hash_name={encodedName}&language=english";

            string jsonText;
            try
            {
                jsonText = await GetCachedAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pricehistory-pyyntö epäonnistui {marketHashName}: {ex.Message}");
                return (0m, 0m, 0);
            }

            try
            {
                using var json = JsonDocument.Parse(jsonText);
                var root = json.RootElement;

                if (!root.TryGetProperty("prices", out var pricesElem) ||
                    pricesElem.ValueKind != JsonValueKind.Array ||
                    pricesElem.GetArrayLength() == 0)
                {
                    Debug.WriteLine($"Ei hintadataa {marketHashName}");
                    return (0m, 0m, 0);
                }

                var pricesList = pricesElem.EnumerateArray().ToList();

                // Avg7d (viimeiset 7 päivää)
                var last7 = pricesList.TakeLast(7).ToList();
                decimal sum7 = last7.Sum(p => p[1].TryGetDecimal(out var price) ? price * p[2].GetInt32() : 0);
                int vol7 = last7.Sum(p => p[2].GetInt32());
                decimal avg7d = vol7 > 0 ? sum7 / vol7 : 0m;

                // Avg30d (viimeiset 30 päivää)
                var last30 = pricesList.TakeLast(30).ToList();
                decimal sum30 = last30.Sum(p => p[1].TryGetDecimal(out var price) ? price * p[2].GetInt32() : 0);
                int vol30 = last30.Sum(p => p[2].GetInt32());
                decimal avg30d = vol30 > 0 ? sum30 / vol30 : 0m;

                // Volume24h (viimeisin entry)
                int volume24h = pricesList.Count > 0 ? pricesList[^1][2].GetInt32() : 0;

                Debug.WriteLine($"Pricehistory {marketHashName}: Avg7d={avg7d:F2}, Avg30d={avg30d:F2}, Vol24h={volume24h}");
                return (avg7d, avg30d, volume24h);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pricehistory-parsinta epäonnistui {marketHashName}: {ex.Message}");
                return (0m, 0m, 0);
            }
        }

        /// <summary>
        /// Hakee item_nameid listings-sivulta (tai cachesta / tunnetuista)
        /// </summary>
        public async Task<long> GetItemNameIdAsync(string marketHashName)
        {
            string normalized = marketHashName.Trim();

            // 1. Tarkista staattinen dictionary
            if (KnownItemIds.TryGetValue(normalized, out long knownId) && knownId > 0)
            {
                Debug.WriteLine($"Known ID: {normalized} → {knownId}");
                return knownId;
            }

            // 2. Tarkista oma cache
            if (_itemNameIdCache.TryGetValue(normalized, out long cachedId) && cachedId > 0)
            {
                Debug.WriteLine($"Cache hit ID: {normalized} → {cachedId}");
                return cachedId;
            }

            // 3. Hae listings-sivulta
            string encoded = Uri.EscapeDataString(normalized);
            string url = $"market/listings/{AppId}/{encoded}";

            Debug.WriteLine($"Haetaan itemNameId: {url}");

            string html;
            try
            {
                html = await GetCachedAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Listings-pyyntö epäonnistui {normalized}: {ex.Message}");
                return 0;
            }

            var doc = new HtmlDoc();
            doc.LoadHtml(html);

            var script = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'Market_LoadOrderSpread')]");
            if (script == null)
            {
                Debug.WriteLine($"Ei löytynyt Market_LoadOrderSpread-scriptiä: {normalized}");
                return 0;
            }

            var match = Regex.Match(script.InnerText, @"Market_LoadOrderSpread\(\s*(\d+)\s*\)");
            if (!match.Success)
            {
                Debug.WriteLine($"Regex ei osunut: {normalized}");
                return 0;
            }

            if (long.TryParse(match.Groups[1].Value, out long itemId) && itemId > 0)
            {
                _itemNameIdCache[normalized] = itemId;
                Debug.WriteLine($"itemNameId löytyi: {normalized} → {itemId}");
                return itemId;
            }

            Debug.WriteLine($"Virheellinen itemId: {normalized}");
            return 0;
        }

        /// <summary>
        /// Hakee URL:n sisällön cachesta tai verkosta
        /// </summary>
        private async Task<string> GetCachedAsync(string url)
        {
            if (_cache.TryGetValue(url, out string cached) && !string.IsNullOrEmpty(cached))
            {
                Debug.WriteLine($"Cache hit: {url}");
                return cached;
            }

            Debug.WriteLine($"Fetching: {url}");

            for (int retry = 0; retry < 3; retry++)
            {
                try
                {
                    var response = await _client.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Debug.WriteLine($"429 Too Many Requests → odotetaan 60s");
                        await Task.Delay(60000);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Virhe {response.StatusCode}: {errorBody}");
                        throw new HttpRequestException($"Steam palautti {response.StatusCode}: {errorBody}");
                    }

                    var text = await response.Content.ReadAsStringAsync();
                    _cache.Set(url, text, TimeSpan.FromMinutes(10));

                    // Satunnainen viive botin estoon
                    await Task.Delay(3000 + Random.Shared.Next(2000, 8000));

                    return text;
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"HTTP-virhe (retry {retry + 1}/3): {ex.Message}");
                    if (retry < 2) await Task.Delay(10000 * (retry + 1));
                }
            }

            throw new Exception($"Kaikki retryt epäonnistuivat: {url}");
        }
    }
}