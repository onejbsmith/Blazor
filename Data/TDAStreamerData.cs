using Newtonsoft.Json.Linq;
using Syncfusion.Blazor.CircularGauge;
using Syncfusion.Blazor.PdfViewer;
using Syncfusion.Blazor.RichTextEditor.Internal;
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
        public static bool isRealTime = false;

        static public event Action OnStatusesChanged;
        static public event Action OnActiveStatusChanged;
        static public event Action OnTimeSalesStatusChanged;
        private static void StatusChanged() => OnStatusesChanged?.Invoke();
        private static void ActiveStatusChanged() => OnActiveStatusChanged?.Invoke();
        private static void TimeSalesStatusChanged() => OnTimeSalesStatusChanged?.Invoke();
        /// <summary>
        /// These can be seeeded form the database in case of resume or backtesting
        /// </summary>
        /// 
        public static Dictionary<DateTime, Quote_BidAskLast> dictQuotes { get; set; } = new Dictionary<DateTime, Quote_BidAskLast>();
        public static Dictionary<string, List<Quote_Content>> quotes { get; set; } = new Dictionary<string, List<Quote_Content>>() { { "QQQ", new List<Quote_Content>() } };
        public static Dictionary<string, List<TimeSales_Content>> timeSales { get; set; } = new Dictionary<string, List<TimeSales_Content>>() { { "QQQ", new List<TimeSales_Content>() } };
        public static Dictionary<string, Dictionary<int, Chart_Content>> chart { get; set; } = new Dictionary<string, Dictionary<int, Chart_Content>>() { { "QQQ", new Dictionary<int, Chart_Content>() } };

        public static List<BookDataItem> lstAsks = new List<BookDataItem>();
        public static List<BookDataItem> lstBids = new List<BookDataItem>();
        public static List<BookDataItem> lstAllAsks = new List<BookDataItem>();
        public static List<BookDataItem> lstAllBids = new List<BookDataItem>();

        private static List<OptionQuote_Response> _lstOptions = new List<OptionQuote_Response>();

        private static Dictionary<string, DateTime> dictFromTime = new Dictionary<string, DateTime>();
        private static Dictionary<string, Int32> dictCounts = new Dictionary<string, Int32>();
        private static Dictionary<string, Int32> dictVolume = new Dictionary<string, Int32>();
        public static List<OptionQuote_Response> lstOptions
        {
            get { return _lstOptions; }
            set { _lstOptions = value; }
        }
        public static Dictionary<string, Actives> dictActives = new Dictionary<string, Actives>();
        public static Quote_Content[] quote { get; set; }

        public static string chain { get; set; } //
        public static DateTime expiryDate { get; set; } //
        public static double strike { get; set; } //
        public static List<string> timeAndSalesFields { get; set; } = FilesManager.GetCSVHeader(new TimeSales_Content()).Split(',').ToList();

        public static string bidPrice { get; set; }
        public static string askPrice { get; set; }
        public static long quoteLatency { get; set; }
        public static Dictionary<float, Dictionary<DateTime, double>> values { get;  set; } = new Dictionary<float, Dictionary<DateTime,double>>();

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

        public static string getLoginRequest(bool isLogout = false)
        {
            var userPrincipalsResponse = getUserPrincipalsResponse();

            var credentials = getPrincipalsCredentials(userPrincipalsResponse);
            string _credentials = System.Text.Json.JsonSerializer.Serialize<TDAStreamingCredentials>(credentials,
                new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });


            Request loginRequest = Request_Login(userPrincipalsResponse, _credentials);
            if (isLogout) loginRequest = Request_Logout(userPrincipalsResponse, _credentials);

            TDAStreamingRequests tdaStreamingRequests = new TDAStreamingRequests() { requests = new Request[1] };
            tdaStreamingRequests.requests[0] = loginRequest;
            string _loginRequest = System.Text.Json.JsonSerializer.Serialize<TDAStreamingRequests>(tdaStreamingRequests,
                new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });

            return _loginRequest;
        }

        private static Request Request_Login(TDAUserPrincipalInfo userPrincipalsResponse, string _credentials)
        {
            return new Request()
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
        }
        private static Request Request_Logout(TDAUserPrincipalInfo userPrincipalsResponse, string _credentials)
        {
            return new Request()
            {
                service = "ADMIN",
                command = "LOGOUT",
                requestid = "0",
                account = userPrincipalsResponse.accounts[0].accountId,
                source = userPrincipalsResponse.streamerInfo.appId,
                parameters = new Parameters()
                {
                }
            };
        }

        public static async Task<string> getServiceRequest(IEnumerable<int> values, string symbol = "QQQ")
        {

            var userPrincipalsResponse = getUserPrincipalsResponse();
            //string[] valuesName = new string[] { "ALL", "NASDAQ_BOOK", "TIMESALE_EQUITY", "CHART_EQUITY", "OPTION", "QUOTE","ACTIVES" };

            if (values.Contains(4))  // options
            {
                var qt = await GetStaticQuote("QQQ");
                SetOptionStrike(qt);
            }

            dataRequest bookRequest = Request_Book(symbol, userPrincipalsResponse);
            dataRequest qtRequest = Request_Quote(symbol, userPrincipalsResponse);
            dataRequest optRequest = Request_OptionQuote(symbol, chain, userPrincipalsResponse);
            dataRequest timeSales = Request_TimeSales(symbol, userPrincipalsResponse);
            dataRequest chartRequest = Request_Chart(symbol, userPrincipalsResponse);
            dataRequest[] dataRequests = new dataRequest[] { null, bookRequest, timeSales, chartRequest, optRequest, qtRequest };


            var servicesSelected = values.Where(i => i != 6).Select(i => dataRequests[i]).ToArray();
            if (values.Contains(6)) // Actives
                servicesSelected = servicesSelected.Concat(Request_Actives(symbol, userPrincipalsResponse)).ToArray();

            dataRequests allReqs = new dataRequests() { requests = servicesSelected };

            var _allRequests = System.Text.Json.JsonSerializer.Serialize<dataRequests>(allReqs,
            new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
            return _allRequests;
        }

        private static dataRequest[] Request_Actives(string symbol, TDAUserPrincipalInfo userPrincipalsResponse)
        {
            var requests = new dataRequest[]
            {
                new dataRequest{
                    service = "ACTIVES_NASDAQ",
                    requestid = "7",
                    command = "SUBS",
                    account = userPrincipalsResponse.accounts[0].accountId,
                    source = userPrincipalsResponse.streamerInfo.appId,
                    parameters = new dataRequestParameters()
                    {
                        keys = "NASDAQ-ALL,NASDAQ-3600, NASDAQ-31800, NASDAQ-600, NASDAQ-300, NASDAQ-60",
                        fields = "0,1"
                    }

                },
                new dataRequest{
                    service = "ACTIVES_NYSE",
                    requestid = "8",
                    command = "SUBS",
                    account = userPrincipalsResponse.accounts[0].accountId,
                    source = userPrincipalsResponse.streamerInfo.appId,
                    parameters = new dataRequestParameters()
                    {
                        keys = "NYSE-ALL,NYSE-3600, NYSE-31800, NYSE-600, NYSE-300, NYSE-60",
                        fields = "0,1"
                    }
                },
                new dataRequest{
                    service = "ACTIVES_OPTIONS",
                    requestid = "9",
                    command = "SUBS",
                    account = userPrincipalsResponse.accounts[0].accountId,
                    source = userPrincipalsResponse.streamerInfo.appId,
                    parameters = new dataRequestParameters()
                    {
                        keys = "CALLS-ALL,CALLS-3600, CALLS-1800, CALLS-600, CALLS-300, CALLS-60,PUTS-ALL,PUTS-3600, PUTS-1800, PUTS-600, PUTS-300, PUTS-60",
                        fields = "0,1"
                    }
                }
            }

            ;

            return requests;
        }
        private static dataRequest Request_Chart(string symbol, TDAUserPrincipalInfo userPrincipalsResponse)
        {
            return new dataRequest()
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
        }

        public static string getServiceRequestOld(string serviceName, string symbol = "QQQ")
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

                case "NASDAQ_BOOK":
                    dataRequest listedBookRequest = Request_Book(symbol, userPrincipalsResponse);
                    dataRequests lbReqs = new dataRequests() { requests = new dataRequest[1] };
                    lbReqs.requests[0] = listedBookRequest;
                    var _listedBookRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(lbReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _listedBookRequest;

                case "QUOTE":
                    dataRequest quoteRequest = Request_Quote(symbol, userPrincipalsResponse);
                    dataRequests qtReqs = new dataRequests() { requests = new dataRequest[1] };
                    qtReqs.requests[0] = quoteRequest;
                    var _quoteRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(qtReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _quoteRequest;

                case "OPTION":
                    dataRequest optionRequest = Request_OptionQuote(symbol, chain, userPrincipalsResponse);
                    dataRequests opReqs = new dataRequests() { requests = new dataRequest[1] };
                    opReqs.requests[0] = optionRequest;
                    var _optionRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(opReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _optionRequest;

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
                    dataRequest bookRequest = Request_Book(symbol, userPrincipalsResponse);
                    dataRequest qtRequest = Request_Quote(symbol, userPrincipalsResponse);
                    dataRequest optRequest = Request_OptionQuote(symbol, chain, userPrincipalsResponse);
                    dataRequest timeSales = Request_TimeSales(symbol, userPrincipalsResponse);

                    dataRequests allReqs = new dataRequests() { requests = new dataRequest[4] };
                    allReqs.requests[3] = timeSales;
                    allReqs.requests[2] = bookRequest;
                    allReqs.requests[0] = optRequest;
                    allReqs.requests[1] = qtRequest;
                    var _allRequests = System.Text.Json.JsonSerializer.Serialize<dataRequests>(allReqs,
                    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                    return _allRequests;

                //case "ALL":
                //    dataRequest timeSales = Request_TimeSales(symbol, userPrincipalsResponse);
                //    var quote = new dataRequest()
                //    {
                //        service = "QUOTE",
                //        requestid = "2",
                //        command = "SUBS",
                //        account = userPrincipalsResponse.accounts[0].accountId,
                //        source = userPrincipalsResponse.streamerInfo.appId,
                //        parameters = new dataRequestParameters()
                //        {
                //            keys = $"{symbol}",
                //            fields = "0,1,2,3,4,5,6,7,8,9,19,11,12,15"
                //        }

                //    };

                //    var chart = new dataRequest()
                //    {
                //        service = "CHART_EQUITY",
                //        requestid = "3",
                //        command = "SUBS",
                //        account = userPrincipalsResponse.accounts[0].accountId,
                //        source = userPrincipalsResponse.streamerInfo.appId,
                //        parameters = new dataRequestParameters()
                //        {
                //            keys = $"{symbol}",
                //            fields = "0,1,2,3,4,5,6,7"
                //        }

                //    };

                //    dataRequests allReqs = new dataRequests() { requests = new dataRequest[3] };
                //    allReqs.requests[2] = timeSales;
                //    allReqs.requests[0] = quote;
                //    allReqs.requests[1] = chart;
                //    var _allRequest = System.Text.Json.JsonSerializer.Serialize<dataRequests>(allReqs,
                //    new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                //    return _allRequest;

                default:
                    return "";

            }

        }

        public static void SetOptionStrike(TDAStockQuote qt)
        {
            strike = Math.Floor(qt.mark);
            var optDate = expiryDate.ToString("MMddyy");

            var call = $"{optDate}C{strike + 1}";
            var put = $"{optDate}P{strike}";
            TDAStreamerData.chain = $"{call},{put}";
        }

        private static dataRequest Request_TimeSales(string symbol, TDAUserPrincipalInfo userPrincipalsResponse)
        {
            return new dataRequest()
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
        }

        private static dataRequest Request_OptionQuote(string symbol, string chain, TDAUserPrincipalInfo userPrincipalsResponse)
        {
            if (chain == null) return new dataRequest();
            var saKeys = chain.Split(',').Select(ch => $"{symbol}_{ch}");
            return new dataRequest()
            {
                service = "OPTION",
                requestid = "6",
                command = "SUBS",
                account = userPrincipalsResponse.accounts[0].accountId,
                source = userPrincipalsResponse.streamerInfo.appId,
                parameters = new dataRequestParameters()
                {
                    keys = string.Join(',', saKeys),
                    fields = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41"
                }

            };
        }

        private static dataRequest Request_Quote(string symbol, TDAUserPrincipalInfo userPrincipalsResponse)
        {
            return new dataRequest()
            {
                service = "QUOTE",
                requestid = "3",
                command = "SUBS",
                account = userPrincipalsResponse.accounts[0].accountId,
                source = userPrincipalsResponse.streamerInfo.appId,
                parameters = new dataRequestParameters()
                {
                    keys = $"{symbol}",
                    fields = "0,1,2,3,4,5,6,7,8,9,10,11,12,15"
                }

            };
        }

        private static dataRequest Request_Book(string symbol, TDAUserPrincipalInfo userPrincipalsResponse)
        {
            return new dataRequest()
            {
                service = "NASDAQ_BOOK",
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
                        Decode_TimeSales(svcName, content, symbol);
                        break;
                    case "QUOTE":
                        await Decode_Quote(content);
                        break;
                    case "OPTION":
                        await Decode_Option(content);
                        break;
                    case "NASDAQ_BOOK":
                        Decode_Book(content);
                        break;

                    case "CHART_EQUITY":
                        Decode_Chart(content, symbol);
                        break;

                    case "ACTIVES_NASDAQ":
                    case "ACTIVES_NYSE":
                    case "ACTIVES_OPTIONS":
                        Decode_Actives(content);
                        break;


                    default:
                        break;
                }

            }
            StatusChanged();
        }

        private static void Decode_Actives(string content)
        {
            ////var words =
            ////    from word in "57510;0;00:00:00;15:58:30;6;0:0:0;1:0:0;2:0:0;3:0:0;4:10:5234081:AAPL:230692:4.41:SYMC:87008:1.66:LVLT:86452:1.65:GOOG:79025:1.51:INTC:75896:1.45:QQQQ:73049:1.4:ATYT:71554:1.37:BRCM:68573:1.31:CSCO:59819:1.14:VPHM:58947:1.13;5:10:2096769681:QQQQ:112137833:5.35:LVLT:101695066:4.85:SUNW:68187431:3.25:INTC:65462262:3.12:JDSU:55355659:2.64:CSCO:50843090:2.42:MSFT:49805550:2.38:AAPL:48921314:2.33:ORCL:47951340:2.29:SIRI:35386791:1.69;"
            ////    .Split(";")
            ////    select word;
            ///
            var json = JObject.Parse(content);

            var data = content;
            var words =
                 from word in data.Split(";")
                 select word;

            if (words.Count() > 1)
            {
                var trade_symbols = words.ElementAt(5).Split(":").Where((x, i) => i % 3 == 0);
                var trade_index = Convert.ToInt16(trade_symbols.ElementAt(0));

                var trade_vols = words.ElementAt(5).Split(":").Where((x, i) => i % 3 == 1);
                var trade_totCount = Convert.ToInt16(trade_vols.ElementAt(0));

                var trade_pcts = words.ElementAt(5).Split(":").Where((x, i) => i % 3 == 2);
                var trade_totVol = Convert.ToInt64(trade_pcts.ElementAt(0));

                var shares_symbols = words.ElementAt(6).Split(":").Where((x, i) => i % 3 == 0);
                var shares_index = Convert.ToInt16(shares_symbols.ElementAt(0));

                var shares_vols = words.ElementAt(6).Split(":").Where((x, i) => i % 3 == 1);
                var shares_totCount = Convert.ToInt16(shares_vols.ElementAt(0));

                var shares_pcts = words.ElementAt(6).Split(":").Where((x, i) => i % 3 == 2);
                var shares_totVol = Convert.ToInt64(shares_pcts.ElementAt(0));

                var key = (((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JContainer)json.First).First).Value).ToString();

                var trades = new List<anActive>();
                var shares = new List<anActive>();

                for (int i = 1; i < trade_vols.Count(); i++)
                {
                    var sym = "T" + key + trade_symbols.ElementAt(i);
                    if (!dictFromTime.ContainsKey(sym))
                    {
                        dictFromTime.Add(sym, DateTime.Now);
                        dictCounts.Add(sym, 0);
                        dictVolume.Add(sym, 0);
                    }
                    if (dictVolume[sym] != Convert.ToInt32(trade_vols.ElementAt(i)))
                    {
                        dictCounts[sym] += 1;
                        dictVolume[sym] = Convert.ToInt32(trade_vols.ElementAt(i));
                    }

                    trades.Add(new anActive()
                    {
                        symbol = trade_symbols.ElementAt(i),
                        volume = Convert.ToInt32(trade_vols.ElementAt(i)),
                        percent = Convert.ToDecimal(trade_pcts.ElementAt(i)),
                        fromTime = dictFromTime[sym],
                        counts = dictCounts[sym]
                    });
                    trades.RemoveAll(t => t.fromTime < DateTime.Now.AddSeconds(-300));

                    sym = "S" + key + shares_symbols.ElementAt(i);
                    if (!dictFromTime.ContainsKey(sym))
                    {
                        dictFromTime.Add(sym, DateTime.Now);
                        dictCounts.Add(sym, 0);
                        dictVolume.Add(sym, 0);
                    }
                    if (dictVolume[sym] != Convert.ToInt32(shares_vols.ElementAt(i)))
                    {
                        dictCounts[sym] += 1;
                        dictVolume[sym] = Convert.ToInt32(shares_vols.ElementAt(i));
                    }

                    shares.Add(new anActive()
                    {
                        symbol = shares_symbols.ElementAt(i),
                        volume = Convert.ToInt32(shares_vols.ElementAt(i)),
                        percent = Convert.ToDecimal(shares_pcts.ElementAt(i).Replace("\"\r\n}", "")),
                        fromTime = dictFromTime[sym],
                        counts = dictCounts[sym]
                    });

                    shares.RemoveAll(t => t.fromTime < DateTime.Now.AddSeconds(-300));


                }

                var actives = new Actives()
                {
                    activeShares = shares,
                    activeTrades = trades
                };
                if (dictActives.ContainsKey(key))
                    dictActives[key] = actives;
                else
                    dictActives.Add(key, actives);

                //    
            }
            ActiveStatusChanged();
        }
        //public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> idict, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        //{
        //    foreach (var kvp in idict.Where(predicate).ToList())
        //    {
        //        idict.Remove(kvp.Key);
        //    }
        //}

        //static double sumBidSize = 0d;
        //static double sumAskSize = 0d;

        private static void Decode_Book(string content)
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

            if (n == 0) return;
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
            //lstAllBids.RemoveAll(t => t.time < DateTime.Now.AddSeconds(-300));

            n = asks.Count();
            if (n == 0) return;
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
            //lstAllAsks.RemoveAll(t => t.time < DateTime.Now.AddSeconds(-300));

            //lstAllBids.Add(new BookDataItem() { Price = baseAskPrice, Size = sumAskSize });
            //lstAllBids.Add(new BookDataItem() { Price = basePrice, Size = sumBidSize });

        }

        private static void Decode_Chart(string content, string symbol)
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

        private static Dictionary<string, OptionQuote_Response> dictOptions = new Dictionary<string, OptionQuote_Response>();
        private static async Task Decode_Option(string content)
        {

            var optQuote = JsonSerializer.Deserialize<OptionQuote_Response>(content);
            // keep a dictionary of option quotes and update it
            if (dictOptions.ContainsKey(optQuote.key))
                dictOptions[optQuote.key] = optQuote;
            else
                dictOptions.Add(optQuote.key, optQuote);
            dictOptions.RemoveAll((key, val) => val.QuoteDate < DateTime.Now.AddSeconds(-300));

            lstOptions.Clear();
            lstOptions = new List<OptionQuote_Response>(dictOptions.Values);

        }

        private static async Task Decode_Quote(string content)
        {
            try
            {
                quote = new Quote_Content[1];
                Quote_Content qt = JsonSerializer.Deserialize<Quote_Content>(content);

                quote[0] = qt;
                Quote_BidAskLast qbal = new Quote_BidAskLast()
                {
                    bidPrice = qt.bidPrice,
                    askPrice = qt.askPrice,
                    lastPrice = qt.lastPrice,
                    bidSize = qt.bidSize,
                    askSize = qt.askSize,
                    lastSize = qt.lastSize
                };

                dictQuotes.Add(qt.QuoteDate, qbal);
                //dictQuotes.RemoveAll((key, val) => key < DateTime.Now.AddSeconds(-300));
            }
            catch (Exception ex)
            {

            }


        }

        private static async Task<TDAStockQuote> GetStatic_Quote(string symbol)
        {
            var qt = await TDAApiService.GetQuote(symbol);
            if (!TDAParameters.staticQuote.ContainsKey(symbol))
            {
                TDAParameters.staticQuote.Add(symbol, new Quote_Content() { key = symbol });
                TDAParameters.staticQuote.RemoveAll((key, val) => val.QuoteDate < DateTime.Now.AddSeconds(-300));


            }
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

            return qt;
        }

        private static void Decode_TimeSales(string svcName, string content, string symbol)
        {
            if (dictQuotes.Count == 0) return;

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


            /// t.Key is Quote date and time, we want last quote before t&s time
            /// 
            DateTime timeOfTimeAndSales = timeAndSales.TimeDate;
            Quote_BidAskLast qt = dictQuotes.Where(t => t.Key < timeOfTimeAndSales).Last().Value;

            // Debug.Print($"Time in phase? {staticQuote.quoteTime}<{timeAndSales.time} = {staticQuote.quoteTime < timeAndSales.time}");
            timeAndSales.bid = dictQuotes.Where(t => t.Key < timeOfTimeAndSales && t.Value.bidPrice != 0).Last().Value.bidPrice;
            timeAndSales.ask = dictQuotes.Where(t => t.Key < timeOfTimeAndSales && t.Value.askPrice != 0).Last().Value.askPrice;
            //timeAndSales.askSize = dictQuotes.Where(t => t.Key < timeOfTimeAndSales && t.Value.askSize != 0).Last().Value.askSize;
            //timeAndSales.bidSize = dictQuotes.Where(t => t.Key < timeOfTimeAndSales && t.Value.bidSize != 0).Last().Value.bidSize;
            //timeAndSales.last = dictQuotes.Where(t => t.Key < timeOfTimeAndSales && t.Value.lastPrice != 0).Last().Value.lastPrice;
            //timeAndSales.lastSize = dictQuotes.Where(t => t.Key < timeOfTimeAndSales && t.Value.lastSize != 0).Last().Value.lastSize;
            //timeAndSales.bidIncr = timeAndSales.bid < prevTimeAndSales.bid ? prevTimeAndSales.bid - timeAndSales.bid : 0; ;
            //timeAndSales.askIncr = timeAndSales.ask > prevTimeAndSales.ask ? timeAndSales.ask - prevTimeAndSales.ask : 0;
            var bid = timeAndSales.bid;
            var ask = timeAndSales.ask;
            var price = timeAndSales.price;
            timeAndSales.level = bid == 0 || ask == 0 || price == 0 ? 0 :
             price < bid ? 1
             : price == bid ? 2
             : price > bid && price < ask ?
             (
                price - bid < 0.01 ? 2
                : (ask - price < 0.01 ? 4 : 3)
             )
             : price == ask ? 4
             : price > ask ? 5
             : 0;


            //timeAndSales.level = bid == 0 || ask == 0 || price == 0 ? 0 :
            //price < bid ? 1
            //: price == bid ? 2
            //: price > bid && price < ask ? 3
            //: price == ask ? 4
            //: price > ask ? 5
            //: 0;

            /// Add current time & sales to list for symbol
            TDAStreamerData.timeSales[symbol].Add(timeAndSales);
            //TDAStreamerData.timeSales[symbol].RemoveAll(t => t.TimeDate < DateTime.Now.AddSeconds(-300));
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

            TimeSalesStatusChanged();

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
            //lstAllAsks.RemoveAll(t => t.time < DateTime.Now.AddSeconds(-300));

            if (lstAllBids.Count == 0 || lstAllAsks.Count == 0) return;

            double bidSize = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-seconds)).Sum(t => t.Size);
            double askSize = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-seconds)).Sum(t => t.Size);

            var allBids = new BookDataItem() { Price = lstAllBids[0].Price, Size = bidSize };
            var allAsks = new BookDataItem() { Price = lstAllAsks[0].Price, Size = askSize };

            bookData[1] = allBids;
            bookData[0] = allAsks;
        }

        public static void getBookCompositePieData(ref BookDataItem[] bookData, int seconds, bool isPrintsSize, string symbol)
        {
            if (lstAllAsks.Count == 0) return;
            bookData = new BookDataItem[2];

            double bidSize2 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-2)).Sum(t => t.Size) * 8;
            double askSize2 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-2)).Sum(t => t.Size) * 8;
            double bidSize10 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-10)).Sum(t => t.Size) * 4;
            double askSize10 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-10)).Sum(t => t.Size) * 4;
            double bidSize30 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-30)).Sum(t => t.Size) * 2;
            double askSize30 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-30)).Sum(t => t.Size) * 2;
            double bidSize60 = lstAllBids.Where(t => t.time >= DateTime.Now.AddSeconds(-60)).Sum(t => t.Size);
            double askSize60 = lstAllAsks.Where(t => t.time >= DateTime.Now.AddSeconds(-60)).Sum(t => t.Size);

            var allBids = new BookDataItem() { Price = lstAllBids[0].Price, Size = bidSize2 + bidSize10 + bidSize30 + bidSize60 };
            var allAsks = new BookDataItem() { Price = lstAllAsks[0].Price, Size = askSize2 + askSize10 + askSize30 + askSize60 };

            bookData[1] = allBids;
            bookData[0] = allAsks;
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

    //public static class DictionaryExtensions
    //{
    //    public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic,
    //        Func<TKey, TValue, bool> predicate)
    //    {
    //        var keys = dic.Keys.Where(k => predicate(k, dic[k])).ToList();
    //        foreach (var key in keys)
    //        {
    //            dic.Remove(key);
    //        }
    //    }
    //}

}
//case "xQUOTE":
//    // var polledQuote = new 
//    var partialQuote = JsonSerializer.Deserialize<Quote_Content>(content);
//    var qtSymbol = partialQuote.key;
//    if (!TDAParameters.staticQuote.ContainsKey(qtSymbol))
//        TDAParameters.staticQuote.Add(qtSymbol, new Quote_Content() { key = qtSymbol });
//    var staticQuoteToUpdate = TDAParameters.staticQuote[qtSymbol];
//    /// Update a static quote object here
//    /// TDAParameters.staticQuote["QQQ"]["askPrice"]
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
