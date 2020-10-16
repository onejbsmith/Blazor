using Newtonsoft.Json.Linq;
using Syncfusion.Blazor.CircularGauge;
using Syncfusion.Blazor.PdfViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorTrader.Data
{
    /// <summary>
    /// To hold realtime arrays of streamed data 
    /// </summary>
    public class TDAStreamerData
    {
        static public Components.PrintsDashboard Dashboard = null;
        public static bool isRealTime = true;

        static public event Action OnStatusesChanged;
        private static void StatusChanged() => OnStatusesChanged?.Invoke();
        /// <summary>
        /// These can be seeeded form the database in case of resume or backtesting
        /// </summary>
        public static Dictionary<string, List<Quote_Content>> quotes { get; set; } = new Dictionary<string, List<Quote_Content>>() { { "SPY", new List<Quote_Content>() } };
        public static Dictionary<string, List<TimeSales_Content>> timeSales { get; set; } = new Dictionary<string, List<TimeSales_Content>>() { { "SPY", new List<TimeSales_Content>() } };
        public static Dictionary<string, Dictionary<int, Chart_Content>> chart { get; set; } = new Dictionary<string, Dictionary<int, Chart_Content>>() { { "SPY", new Dictionary<int, Chart_Content>() } };

        public static List<BookDataItem> lstAsks = new List<BookDataItem>();
        public static List<BookDataItem> lstBids = new List<BookDataItem>();
        public static List<BookDataItem> lstAllAsks = new List<BookDataItem>();
        public static List<BookDataItem> lstAllBids = new List<BookDataItem>();


        public static List<string> timeAndSalesFields { get; set; } = FilesManager.GetCSVHeader(new TimeSales_Content()).Split(',').ToList();

        public static string bidPrice { get; set; }
        public static string askPrice { get; set; }
        public static long quoteLatency { get; set; }
        public static int printLevelCount(string symbol, int level)
        {
            return timeSales[symbol].Where(t => t.level == level).Count();
        }

        public static int printLevelCount(string symbol, int level, int seconds)
        {
            if (seconds == 0) return printLevelCount(symbol, level);
            long lastNseconds = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;
            return TDAStreamerData.timeSales[symbol].Where(t => t.level == level && t.time >= lastNseconds).Count();
        }

        public static float printLevelSum(string symbol, int level)
        {
            return TDAStreamerData.timeSales[symbol].Where(t => t.level == level).Sum(t => t.size);
        }

        public static float printLevelSum(string symbol, int level, int seconds)
        {
            if (seconds == 0) return printLevelSum(symbol, level);

            long lastNseconds = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;

            return TDAStreamerData.timeSales[symbol].Where(t => t.level == level && t.time >= lastNseconds).Sum(t => t.size);
        }

        public static int printCount(string symbol)
        {
            return TDAStreamerData.timeSales[symbol].Count();
        }

        public static int printCount(string symbol, int seconds)
        {
            if (seconds == 0) return printCount(symbol);

            long lastNseconds = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;
            return TDAStreamerData.timeSales[symbol].Where(t => t.time >= lastNseconds).Count();
        }

        public static float printSum(string symbol)
        {
            return TDAStreamerData.timeSales[symbol].Sum(t => t.size);
        }

        public static float printSum(string symbol, int seconds)
        {
            if (seconds == 0) return printSum(symbol);

            long lastNseconds = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;

            return TDAStreamerData.timeSales[symbol].Where(t => t.time >= lastNseconds).Sum(t => t.size);
        }
        public static TDAUserPrincipalInfo getUserPrincipalsResponse()
        {
            string _TDAUserPrincipalJson = System.IO.File.ReadAllText("TDAUserPrincipalAuth.json");
            var userPrincipalsResponse = System.Text.Json.JsonSerializer.Deserialize<TDAUserPrincipalInfo>(_TDAUserPrincipalJson);
            return userPrincipalsResponse;
        }
        public static TDAStreamingCredentials getPrincipalsCredentials(TDAUserPrincipalInfo userPrincipalsResponse)
        {
            //Converts ISO-8601 response in snapshot to ms since epoch accepted by Streamer
            DateTime d2 = DateTime.Parse((userPrincipalsResponse.streamerInfo.tokenTimestamp), null, System.Globalization.DateTimeStyles.RoundtripKind);
            var tokenTimeStampAsMs = new DateTimeOffset(d2).ToUnixTimeMilliseconds();
            var credentials = new TDAStreamingCredentials()
            {
                userid = userPrincipalsResponse.accounts[0].accountId,
                token = userPrincipalsResponse.streamerInfo.token,
                company = userPrincipalsResponse.accounts[0].company,
                segment = userPrincipalsResponse.accounts[0].segment,
                cddomain = userPrincipalsResponse.accounts[0].accountCdDomainId,
                usergroup = userPrincipalsResponse.streamerInfo.userGroup,
                accesslevel = userPrincipalsResponse.streamerInfo.accessLevel,
                authorized = "Y",
                timestamp = tokenTimeStampAsMs,
                appid = userPrincipalsResponse.streamerInfo.appId,
                acl = userPrincipalsResponse.streamerInfo.acl
            };
            return credentials;
        }

        public static void deserializeUserPrincipalInfo(ref string _TDAUserPrincipalJson, ref string _credentials)
        {
            /// Read json string  from file
            _TDAUserPrincipalJson = System.IO.File.ReadAllText("TDAUserPrincipalAuth.json");
            try
            {
                var userPrincipalsResponse = System.Text.Json.JsonSerializer.Deserialize<TDAUserPrincipalInfo>(_TDAUserPrincipalJson);
                var credentials = getPrincipalsCredentials(userPrincipalsResponse);
                _credentials = System.Text.Json.JsonSerializer.Serialize<TDAStreamingCredentials>(credentials,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        // Service Requests

        public static string getServiceRequest(string serviceName, string symbol = "SPY")
        {
            var userPrincipalsResponse = getUserPrincipalsResponse();
            switch (serviceName)
            {

                case "LOGIN":

                    var credentials = getPrincipalsCredentials(userPrincipalsResponse);
                    string _credentials = System.Text.Json.JsonSerializer.Serialize<TDAStreamingCredentials>(credentials,
                        new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });

                    var loginRequest = new Request()
                    {
                        service = "ADMIN",
                        command = "LOGIN",
                        requestid = "0",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new Parameters()
                        {
                            credential = jsonToQueryString(_credentials),
                            token = userPrincipalsResponse.streamerInfo.token,
                            version = "1.0"
                        }
                    };


                    TDAStreamingRequests tdaStreamingRequests = new TDAStreamingRequests() { requests = new Request[1] };
                    tdaStreamingRequests.requests[0] = loginRequest;
                    string _loginRequest = System.Text.Json.JsonSerializer.Serialize<TDAStreamingRequests>(tdaStreamingRequests,
                        new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });

                    return _loginRequest;

                case "TIMESAFLE_EQUITY":
                    var timeSalesRequest = new dataRequest()
                    {
                        service = "TIMESALE_EQUITY",
                        requestid = "2",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4"
                        }

                    };
                    dataRequests tsReqs = new dataRequests() { requests = new dataRequest[1] };
                    tsReqs.requests[0] = timeSalesRequest;
                    var _timeSalesRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(tsReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _timeSalesRequest;

                case "LISTED_BOOK":
                    var listedBookRequest = new dataRequest()
                    {
                        service = "LISTED_BOOK",
                        requestid = "2",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4,5,6,7,8,9"
                        }

                    };
                    dataRequests lbReqs = new dataRequests() { requests = new dataRequest[1] };
                    lbReqs.requests[0] = listedBookRequest;
                    var _listedBookRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(lbReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _listedBookRequest;

                case "QUOTE":
                    var quoteRequest = new dataRequest()
                    {
                        service = "QUOTE",
                        requestid = "3",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4,5,6,7,8,9,19,11,12,15"
                        }

                    };
                    dataRequests qtReqs = new dataRequests() { requests = new dataRequest[1] };
                    qtReqs.requests[0] = quoteRequest;
                    var _quoteRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(qtReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _quoteRequest;

                case "CHART_EQUITY":
                    var chartRequest = new dataRequest()
                    {
                        service = "CHART_EQUITY",
                        requestid = "4",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4,5,6,7"
                        }

                    };
                    dataRequests chReqs = new dataRequests() { requests = new dataRequest[1] };
                    chReqs.requests[0] = chartRequest;
                    var _chartRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(chReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _chartRequest;

                case "ALL":
                    var timeSales = new dataRequest()
                    {
                        service = "TIMESALE_EQUITY",
                        requestid = "4",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4"
                        }

                    };
                    var quote = new dataRequest()
                    {
                        service = "QUOTE",
                        requestid = "2",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4,5,6,7,8,9,19,11,12,15"
                        }

                    };

                    var chart = new dataRequest()
                    {
                        service = "CHART_EQUITY",
                        requestid = "3",
                        command = "SUBS",
                        account = userPrincipalsResponse.accounts[0].accountId,
                        source = userPrincipalsResponse.streamerInfo.appId,
                        parameters = new dataRequestParameters()
                        {
                            keys = $"{symbol}",
                            fields = "0,1,2,3,4,5,6,7"
                        }

                    };

                    dataRequests allReqs = new dataRequests() { requests = new dataRequest[3] };
                    allReqs.requests[2] = timeSales;
                    allReqs.requests[0] = quote;
                    allReqs.requests[1] = chart;
                    var _allRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(allReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _allRequest;

                default:
                    return "";

            }

        }

        // Utility
        static string jsonToQueryString(string json)
        {
            var dict = GetFlat(json);
            var saKeyValues = new List<string>();
            foreach (var key in dict.Keys)
                saKeyValues.Add(key + "=" + dict[key]);

            return string.Join("&", saKeyValues.ToArray());
        }

        static Dictionary<string, JsonElement> GetFlat(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                return document.RootElement.EnumerateObject()
                    .SelectMany(p => GetLeaves(null, p))
                    .ToDictionary(k => k.Path, v => v.P.Value.Clone()); //Clone so that we can use the values outside of using
            }
        }

        static IEnumerable<(string Path, JsonProperty P)> GetLeaves(string path, JsonProperty p)
        {
            path = (path == null) ? p.Name : path + "." + p.Name;
            if (p.Value.ValueKind != JsonValueKind.Object)
                yield return (Path: path, P: p);
            else
                foreach (JsonProperty child in p.Value.EnumerateObject())
                    foreach (var leaf in GetLeaves(path, child))
                        yield return leaf;
        }
        ///
        public static async Task<TDAStockQuote> GetStaticQuote(string sSymbol)
        {
            /// TODO: TDA Streaming quotes vs using Thread.Sleep 2000.
            /// TODO: Save option quotes with spreads or spreads to files for sparkline?
            /// DONE: Need to understand TDA Auths and how Refresh Token is used NEEDS WORK
            /// DONE: Need a way to store the Auth info on server and refresh it as needed
            /// 
            var quote = new TDAStockQuote();
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {

                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://api.tdameritrade.com/v1/marketdata/{sSymbol}/quotes?apikey={TDAConstants._apiKey}");
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {TDATokens._TDAAccessToken}");
                response = await httpClient.SendAsync(request);

                TDANotifications.quoteStatus = response.StatusCode.ToString();

                response.EnsureSuccessStatusCode();
                var content = await response.Content?.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(content);
                var sJson = document.RootElement.GetProperty(sSymbol).ToString();
                quote = JsonSerializer.Deserialize<TDAStockQuote>(sJson);
                return quote;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                if (response.StatusCode.ToString() == "Unauthorized" || response.StatusCode.ToString() == "BadRequest")
                {
                    //await GetAuthenticationAsync();
                }
                return quote;
            }
        }
        public static async Task captureTdaServiceData(string svcFieldedJson)
        {
            var svcJsonObject = JObject.Parse(svcFieldedJson);
            var svcName = svcJsonObject["service"].ToString();
            var contents = svcJsonObject["content"];
            var timeStamp = Convert.ToInt64(svcJsonObject["timestamp"]);
            foreach (var contentObj in contents)
            {
                var content = contentObj.ToString();
                var symbol = contentObj["key"].ToString();

                switch (svcName)
                {
                    case "TIMESALE_EQUITY":
                        timeSalesDecode(svcName, content, symbol);
                        break;
                    case "QUOTE":
                        await quoteDecode(symbol);
                        break;
                    case "LISTED_BOOK":
                        listedBookDecode(content);
                        break;

                    case "CHART_EQUITY":
                        chartDecode(content, symbol);
                        break;
                    default:
                        break;
                }

            }
            StatusChanged();
        }

        static double sumBidSize = 0d;
        static double sumAskSize = 0d;

        private static void listedBookDecode(string content)
        {
            var all = JObject.Parse(content);
            var bids = all["2"];
            var asks = all["3"];
            lstBids.Clear();
            lstAsks.Clear();


            /// Grab all raw bids
            /// Cosolidate into three bid groups
            /// do same for asks
            /// Add bids then asks to display set
            /// 
            var n = bids.Count();

            var basePrice = Convert.ToDecimal(((Newtonsoft.Json.Linq.JValue)bids[0]["0"]).Value);
            for (int i = 0; i < n; i++)
            {
                var price = Convert.ToDecimal(((Newtonsoft.Json.Linq.JValue)bids[i]["0"]).Value);
                var size = Convert.ToDouble(((Newtonsoft.Json.Linq.JValue)bids[i]["1"]).Value);

                if (Math.Abs(price - basePrice) < 0.30m)
                {
                    var bid = new BookDataItem() { Price = price, Size = size, time = DateTime.Now };
                    lstBids.Add(bid);
                    lstAllBids.Add(bid);
                    //sumBidSize += size;
                }


            }

            n = asks.Count();
            var baseAskPrice = Convert.ToDecimal(((Newtonsoft.Json.Linq.JValue)asks[0]["0"]).Value);
            for (int i = 0; i < n; i++)
            {
                var price = Convert.ToDecimal(((Newtonsoft.Json.Linq.JValue)asks[i]["0"]).Value);
                var size = Convert.ToDouble(((Newtonsoft.Json.Linq.JValue)asks[i]["1"]).Value);
                if (Math.Abs(price - baseAskPrice) < 0.30m)
                {
                    var ask = new BookDataItem() { Price = price, Size = size, time = DateTime.Now };
                    lstAsks.Add(ask);
                    lstAllAsks.Add(ask);
                    //sumAskSize += size;
                }
            }
            //lstAllBids.Add(new BookDataItem() { Price = baseAskPrice, Size = sumAskSize });
            //lstAllBids.Add(new BookDataItem() { Price = basePrice, Size = sumBidSize });

        }

        private static void chartDecode(string content, string symbol)
        {
            var chart = JsonSerializer.Deserialize<Chart_Content>(content);

            /// Write to database
            if (!TDAStreamerData.chart.ContainsKey(symbol))
                TDAStreamerData.chart.Add(symbol, new Dictionary<int, Chart_Content>());
            if (TDAStreamerData.chart[symbol].ContainsKey(chart.sequence))
                TDAStreamerData.chart[symbol][chart.sequence] = chart;
            else
                TDAStreamerData.chart[symbol].Add(chart.sequence, chart);
        }

        private static async Task quoteDecode(string symbol)
        {
            var qt = await TDAApiService.GetQuote(symbol);
            if (!TDAParameters.staticQuote.ContainsKey(symbol))
                TDAParameters.staticQuote.Add(symbol, new Quote_Content() { key = symbol });
            var staticQuoteToUpdate = TDAParameters.staticQuote[symbol];
            staticQuoteToUpdate.askPrice = qt.askPrice;
            staticQuoteToUpdate.bidPrice = qt.bidPrice;
            staticQuoteToUpdate.askSize = qt.askSize;
            staticQuoteToUpdate.askPrice = qt.askPrice;
            staticQuoteToUpdate.bidSize = qt.bidSize;
            staticQuoteToUpdate.lastPrice = qt.lastPrice;
            staticQuoteToUpdate.lastSize = qt.lastSize;
            staticQuoteToUpdate.quoteTime = qt.quoteTimeInLong;
            staticQuoteToUpdate.tradeTime = qt.tradeTimeInLong;

            if (qt.bidPrice > 0)
                bidPrice = qt.bidPrice.ToString("n2");
            if (qt.askPrice > 0)
                askPrice = qt.askPrice.ToString("n2");
        }

        private static void timeSalesDecode(string svcName, string content, string symbol)
        {
            if (!TDAStreamerData.timeSales.ContainsKey(symbol))
                TDAStreamerData.timeSales.Add(symbol, new List<TimeSales_Content>());

            /// Get current time and sales from streamer content
            var timeAndSales = JsonSerializer.Deserialize<TimeSales_Content>(content);
            var prevTimeAndSales = timeAndSales;
            if (TDAStreamerData.timeSales[symbol].Count > 0)
                prevTimeAndSales = TDAStreamerData.timeSales[symbol].Last();

            /// Combine bid/ask with time & sales and write to database
            /// Need to match time of print and time of quote to get accuarate buys/sells
            //Debugger.Break();

            if (TDAParameters.staticQuote.ContainsKey(symbol))
            {
                var stockQuote = TDAParameters.staticQuote[symbol];

                // Debug.Print($"Time in phase? {staticQuote.quoteTime}<{timeAndSales.time} = {staticQuote.quoteTime < timeAndSales.time}");
                quoteLatency = stockQuote.quoteTime - timeAndSales.time;
                timeAndSales.bid = stockQuote.bidPrice;
                timeAndSales.ask = stockQuote.askPrice;
                timeAndSales.askSize = stockQuote.askSize;
                timeAndSales.bidSize = stockQuote.bidSize;
                timeAndSales.last = stockQuote.lastPrice;
                timeAndSales.lastSize = stockQuote.lastSize;
                timeAndSales.quoteTime = stockQuote.quoteTime;
                timeAndSales.tradeTime = stockQuote.tradeTime;
                timeAndSales.bidIncr = timeAndSales.bid < prevTimeAndSales.bid ? prevTimeAndSales.bid - timeAndSales.bid : 0; ;
                timeAndSales.askIncr = timeAndSales.ask > prevTimeAndSales.ask ? timeAndSales.ask - prevTimeAndSales.ask : 0;
                var bid = timeAndSales.bid;
                var ask = timeAndSales.ask;
                var price = timeAndSales.price;
                timeAndSales.level = bid == 0 || ask == 0 || price == 0 ? 0 :
                 price < bid ? 1 : price == bid ? 2 : price > bid && price < ask ? (price - bid < 0.01 ? 2 : (ask - price < 0.01 ? 4 : 3)) : price == ask ? 4 : price > ask ? 5 : 0;

                /// Add current time & sales to list for symbol
                TDAStreamerData.timeSales[symbol].Add(timeAndSales);
                /// save to csv
                var lstValues = new List<string>();
                foreach (string name in timeAndSalesFields)
                {
                    lstValues.Add($"{timeAndSales[name]}");
                }
                string record = string.Join(',', lstValues) + "\n";
                if (isRealTime)
                {
                    string fileName = $"{svcName} {symbol} {DateTime.Now.ToString("MMM dd yyyy")}.csv";
                    if (!System.IO.File.Exists(fileName))
                    {
                        System.IO.File.AppendAllText(fileName, string.Join(",", timeAndSalesFields) + "\n");

                    }
                    System.IO.File.AppendAllText(fileName, record);
                }


            }
        }

        public static void getBookData(ref BookDataItem[] asksData, ref BookDataItem[] bidsData, int seconds, bool isPrintsSize, string symbol)
        {
            asksData = new BookDataItem[0];
            asksData = lstAsks.ToArray();

            bidsData = new BookDataItem[0];
            bidsData = lstBids.ToArray();
        }


        public static void getBookPieData(ref BookDataItem[] bookData, int seconds, bool isPrintsSize, string symbol)
        {
            bookData = new BookDataItem[2];
            //bookData = lstAllBids.ToArray();

            lstAllBids.RemoveAll(t => t.time < DateTime.Now.AddSeconds(-300));
            lstAllAsks.RemoveAll(t => t.time < DateTime.Now.AddSeconds(-300));


            double bidSize = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-seconds)).Sum(t => t.Size);
            double askSize = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-seconds)).Sum(t => t.Size);

            var allBids = new BookDataItem() { Price = lstAllBids[0].Price, Size = bidSize };
            var allAsks = new BookDataItem() { Price = lstAllAsks[0].Price, Size = askSize };

            bookData[0] = allBids;
            bookData[1] = allAsks;
        }

        public static void getBookCompositePieData(ref BookDataItem[] bookData, int seconds, bool isPrintsSize, string symbol)
        {
            bookData = new BookDataItem[2];
  
            double bidSize2 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-2)).Sum(t => t.Size)*8;
            double askSize2 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-2)).Sum(t => t.Size)*8;
            double bidSize10 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-10)).Sum(t => t.Size)*4;
            double askSize10 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-10)).Sum(t => t.Size)*4;
            double bidSize30 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-30)).Sum(t => t.Size)*2;
            double askSize30 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-30)).Sum(t => t.Size)*2;
            double bidSize60 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-60)).Sum(t => t.Size);
            double askSize60 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-60)).Sum(t => t.Size);

            var allBids = new BookDataItem() { Price = lstAllBids[0].Price, Size = bidSize2 + bidSize10 + bidSize30 + bidSize60 };
            var allAsks = new BookDataItem() { Price = lstAllAsks[0].Price, Size = askSize2 + askSize10 + askSize30 + askSize60 };

            bookData[0] = allBids;
            bookData[1] = allAsks;
        }

        public static void getPrintsData(ref DataItem[] printsData, int seconds, bool isPrintsSize, string symbol)
        {

            long oneMinuteAgo = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;

            if (seconds == 0) // means all prints (for the day)
            {
                if (isPrintsSize)  // Size of Prints (Volume)
                {
                    printsData[0].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1).Sum(t => t.size);
                    printsData[1].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 2).Sum(t => t.size);
                    printsData[2].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 3).Sum(t => t.size);
                    printsData[3].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4).Sum(t => t.size);
                    printsData[4].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 5).Sum(t => t.size);
                }
                else  // Number of Prints (Trades)
                {
                    printsData[0].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1).Count();
                    printsData[1].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 2).Count();
                    printsData[2].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 3).Count();
                    printsData[3].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4).Count();
                    printsData[4].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 5).Count();
                }
            }
            else // prints within the last n seconds
            {
                if (isPrintsSize) // Size of Prints (Volume)
                {
                    printsData[0].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 && t.time >= oneMinuteAgo).Sum(t => t.size);
                    printsData[1].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 2 && t.time >= oneMinuteAgo).Sum(t => t.size);
                    printsData[2].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 3 && t.time >= oneMinuteAgo).Sum(t => t.size);
                    printsData[3].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 && t.time >= oneMinuteAgo).Sum(t => t.size);
                    printsData[4].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 5 && t.time >= oneMinuteAgo).Sum(t => t.size);
                }
                else // Number of Prints
                {
                    printsData[0].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 && t.time >= oneMinuteAgo).Count();
                    printsData[1].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 2 && t.time >= oneMinuteAgo).Count();
                    printsData[2].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 3 && t.time >= oneMinuteAgo).Count();
                    printsData[3].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 && t.time >= oneMinuteAgo).Count();
                    printsData[4].Revenue = TDAStreamerData.timeSales[symbol].Where(t => t.level == 5 && t.time >= oneMinuteAgo).Count();
                }

            }
        }

        public static void getPrintsBuysSellsData(ref List<double> sellsData, ref List<double> buysData, int seconds, bool isPrintsSize, string symbol)
        {
            DateTime endTime = DateTime.MinValue;
            /// maybe use the most recent time in the ts data instead of Now
            try
            {
                long mruTime = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5).Max(t => t.time);
                endTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(mruTime);
            }
            catch (Exception)
            {
                endTime = DateTime.Now.ToUniversalTime();
            }

            //long nSecondsAgo = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;
            long nSecondsAgo = (long)(endTime.AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;

            if (seconds == 0) // means all prints (for the day)
            {
                if (isPrintsSize)  // Size of Prints (Volume per second)
                {
                    /// This is pulling the list of sizes for the whole time & sales,
                    /// we group by second to get the volume per second at each second
                    sellsData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * t.Sum(r => (double)r.size))
                        .ToList();
                    buysData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 || t.level == 2)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * t.Sum(r => (double)r.size))
                        .ToList();
                }
                else  // Number of Prints per second (Trades)
                {
                    sellsData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * (double)t.Sum(r => 1))
                        .ToList();
                    buysData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 || t.level == 2)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * (double)t.Sum(r => 1))
                        .ToList();
                }
            }
            else // prints within the last n seconds
            {
                if (isPrintsSize)  // Size of Prints (Volume per second)
                {
                    /// This is pulling the list of sizes for the whole time & sales,
                    /// we group by second to get the volume per second at each second
                    sellsData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5 && t.time >= nSecondsAgo)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * t.Sum(r => (double)r.size))
                        .ToList();
                    buysData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 || t.level == 2 && t.time >= nSecondsAgo)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * t.Sum(r => (double)r.size))
                        .ToList();
                }
                else  // Number of Prints per second (Trades)
                {
                    sellsData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5 && t.time >= nSecondsAgo)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * (double)t.Sum(r => 1))
                        .ToList();
                    buysData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 || t.level == 2 && t.time >= nSecondsAgo)
                        .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                        .Select(t => 1 * (double)t.Sum(r => 1))
                        .ToList();
                }
            }
        }

        public static void getPrintsMovementBuysSellsData(ref List<double> sellsData, ref List<double> buysData, int seconds, string symbol)
        {
            long nSecondsAgo = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;

            if (seconds == 0) // means all prints (for the day)
            {
                sellsData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5)
                    .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                    .Select(t => 1 * t.Sum(r => (double)r.askIncr))
                    .ToList();
                buysData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 || t.level == 2)
                    .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                    .Select(t => 1 * t.Sum(r => (double)r.bidIncr))
                    .ToList();
            }
            else // prints within the last n seconds
            {
                sellsData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 || t.level == 5 && t.time >= nSecondsAgo)
                    .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                    .Select(t => 1 * t.Sum(r => (double)r.askIncr))
                    .ToList();
                buysData = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 || t.level == 2 && t.time >= nSecondsAgo)
                    .GroupBy(t => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(t.time).Second)
                    .Select(t => 1 * t.Sum(r => (double)r.bidIncr))
                    .ToList();
            }

            //string sBuys = string.Join(',', buysData.Select(t => t.Revenue.ToString("n0")));
            //string sSells = string.Join(',', buysData.Select(t => t.Revenue.ToString("n0")));
            //Console.WriteLine("Buys=" + sBuys);
            //Console.WriteLine("Sells=" + sSells);
        }

    }
    public class DataItem
    {
        public string Quarter { get; set; }
        public double Revenue { get; set; }
    }

    public class BookDataItem
    {
        public decimal Price { get; set; }
        public double Size { get; set; }
        public DateTime time { get; set; }
    }


}
//case "xQUOTE":
//    // var polledQuote = new 
//    var partialQuote = JsonSerializer.Deserialize<Quote_Content>(content);
//    var qtSymbol = partialQuote.key;
//    if (!TDAParameters.staticQuote.ContainsKey(qtSymbol))
//        TDAParameters.staticQuote.Add(qtSymbol, new Quote_Content() { key = qtSymbol });
//    var staticQuoteToUpdate = TDAParameters.staticQuote[qtSymbol];
//    /// Update a static quote object here
//    /// TDAParameters.staticQuote["SPY"]["askPrice"]
//    /// partialQuote["askPrice"]
//    /// Write static quote to database
//    List<string> fields = TDAConstants.TDAResponseFields[svcName];
//    for (int j = 1; j < 13; j++)
//    {
//        string field = fields[j];
//        try
//        {
//            if (partialQuote[field] != null
//                && partialQuote[field].ToString() != "0"
//                && partialQuote[field].ToString() != "")

//                staticQuoteToUpdate[field] = partialQuote[field];
//        }
//        catch { }
//    }
//    if (!TDAStreamerData.quotes.ContainsKey(qtSymbol))
//        TDAStreamerData.quotes.Add(qtSymbol, new List<Quote_Content>());

//    /// Set the quote / trade time if they are 0
//    if (partialQuote.quoteTime == 0) staticQuoteToUpdate.quoteTime = timeStamp;
//    if (partialQuote.tradeTime == 0) staticQuoteToUpdate.tradeTime = timeStamp;

//    if (partialQuote.bidPrice > 0)
//        bidPrice = partialQuote.bidPrice.ToString("n2");
//    if (partialQuote.askPrice > 0)
//        askPrice = partialQuote.askPrice.ToString("n2");

//    TDAStreamerData.quotes[qtSymbol].Add(staticQuoteToUpdate);
//    break;
