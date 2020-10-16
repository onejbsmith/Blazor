using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
/// <summary>
/// TODO: !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Get Source Control functioning
/// </summary>
namespace BlazorTrader.Data
{
    public class ComplexOptionChain
    {
        //}

        //public class Rootobject
        //{
        public string symbol { get; set; }
        public string status { get; set; }
        public UnderlyingStock underlying { get; set; }
        public string strategy { get; set; }
        public float interval { get; set; }
        public bool isDelayed { get; set; }
        public bool isIndex { get; set; }
        public float interestRate { get; set; }
        public float underlyingPrice { get; set; }
        public float volatility { get; set; }
        public float daysToExpiration { get; set; }
        public int numberOfContracts { get; set; }
        public float[] intervals { get; set; }
        public Monthlystrategylist[] monthlyStrategyList { get; set; }
        public Callexpdatemap callExpDateMap { get; set; }
        public Putexpdatemap putExpDateMap { get; set; }
    }

    public class UnderlyingStock
    {
        public string symbol { get; set; }
        public string description { get; set; }
        public float change { get; set; }
        public float percentChange { get; set; }
        public float close { get; set; }
        public long quoteTime { get; set; }
        public long tradeTime { get; set; }
        public float bid { get; set; }
        public float ask { get; set; }
        public float last { get; set; }
        public float mark { get; set; }
        public float markChange { get; set; }
        public float markPercentChange { get; set; }
        public int bidSize { get; set; }
        public int askSize { get; set; }
        public float highPrice { get; set; }
        public float lowPrice { get; set; }
        public float openPrice { get; set; }
        public int totalVolume { get; set; }
        public string exchangeName { get; set; }
        public float fiftyTwoWeekHigh { get; set; }
        public float fiftyTwoWeekLow { get; set; }
        public bool delayed { get; set; }
        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }

    public class Callexpdatemap
    {
    }

    public class Putexpdatemap
    {
    }

    public class Monthlystrategylist
    {
        public string month { get; set; }
        public int year { get; set; }
        public int day { get; set; }
        public int daysToExp { get; set; }
        public string secondaryMonth { get; set; }
        public int secondaryYear { get; set; }
        public int secondaryDay { get; set; }
        public int secondaryDaysToExp { get; set; }
        public string type { get; set; }
        public string secondaryType { get; set; }
        public bool leap { get; set; }
        public Optionstrategylist[] optionStrategyList { get; set; }
        public bool secondaryLeap { get; set; }
    }

    public class Optionstrategylist
    {
        public Primaryleg primaryLeg { get; set; }
        public Secondaryleg secondaryLeg { get; set; }
        public string strategyStrike { get; set; }
        public float strategyBid { get; set; }
        public float strategyAsk { get; set; }
    }

    public class Primaryleg
    {
        public string symbol { get; set; }
        public string putCallInd { get; set; }
        public string description { get; set; }
        public float bid { get; set; }
        public float ask { get; set; }
        public string range { get; set; }
        public float strikePrice { get; set; }
        public float totalVolume { get; set; }
    }

    public class Secondaryleg
    {
        public string symbol { get; set; }
        public string putCallInd { get; set; }
        public string description { get; set; }
        public float bid { get; set; }
        public float ask { get; set; }
        public string range { get; set; }
        public float strikePrice { get; set; }
        public float totalVolume { get; set; }
    }

    public class TDAOptionQuote
    {
        public string putCall { get; set; }
        public string symbol { get; set; }
        public string description { get; set; }
        public string exchangeName { get; set; }
        public float bid { get; set; }
        public float ask { get; set; }
        public float last { get; set; }
        public float mark { get; set; }
        public int bidSize { get; set; }
        public int askSize { get; set; }
        public string bidAskSize { get; set; }
        public int lastSize { get; set; }
        public float highPrice { get; set; }
        public float lowPrice { get; set; }
        public float openPrice { get; set; }
        public float closePrice { get; set; }
        public int totalVolume { get; set; }
        public long tradeDate { get; set; }
        public long tradeTimeInLong { get; set; }
        public long quoteTimeInLong { get; set; }
        public float netChange { get; set; }
        public float volatility { get; set; }
        public float delta { get; set; }
        public float gamma { get; set; }
        public float theta { get; set; }
        public float vega { get; set; }
        public float rho { get; set; }
        public int openInterest { get; set; }
        public float timeValue { get; set; }
        public float theoreticalOptionValue { get; set; }
        public float theoreticalVolatility { get; set; }
        [NotMapped]
        public object optionDeliverablesList { get; set; }
        public float strikePrice { get; set; }
        public long expirationDate { get; set; }
        public float daysToExpiration { get; set; }
        public string expirationType { get; set; }
        public long lastTradingDay { get; set; }
        public float multiplier { get; set; }
        public string settlementType { get; set; }
        public string deliverableNote { get; set; }
        public bool isIndexOption { get; set; }
        public float percentChange { get; set; }
        public float markChange { get; set; }
        public float markPercentChange { get; set; }
        public bool nonStandard { get; set; }
        public bool mini { get; set; }
        public bool inTheMoney { get; set; }
        public float prem { get; set; }
        public float prem2 { get; set; }
        public float maxLoss { get; set; }
        public float breakeven { get; set; }
        public float buyLongStrike { get; set; }
        public float collateral { get; set; } = 0;
        public float credit { get; set; } = 0;
        public int contracts { get; set; } = 1;
        public bool isChecked { get; set; } = false;
        public string buyOption { get; set; } = "";
        public int index { get; set; } = -1;
        public bool isManualContracts { get; set; } = false; // if true, allocate won't reset value
        //public TDAOptionQuote prevOption { get; set; } = new TDAOptionQuote(); // Save ref to prior quote for updating colors
        //public TDAOptionQuote buyLongOption { get; set; } = new TDAOptionQuote(); // Save ref to long leg's quote
        [NotMapped]
        public object this[string propertyName] // So can get properties by name string
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }

    public class TDASingleOptionQuotes
    {
        public string symbol { get; set; }
        public string status { get; set; }
        public UnderlyingStock underlying { get; set; }
        public string strategy { get; set; }
        public float interval { get; set; }
        public bool isDelayed { get; set; }
        public bool isIndex { get; set; }
        public float interestRate { get; set; }
        public float underlyingPrice { get; set; }
        public float volatility { get; set; }
        public float daysToExpiration { get; set; }
        public float numberOfContracts { get; set; }
        public Dictionary<string, Dictionary<string, TDAOptionQuote[]>> callExpDateMap { get; set; }
        public Dictionary<string, Dictionary<string, TDAOptionQuote[]>> putExpDateMap { get; set; }
    }

    public class TDAmertradeApiEndpoints
    {

        public static readonly string _apiKey = "SMITH4035";

        public static readonly string OptionsChainsUrl = $"https://api.tdameritrade.com/v1/marketdata/chains?apikey={_apiKey}&symbol={{0}}&contractType=ALL&strikeCount={{2}}&includeQuotes=TRUE&strategy=SINGLE&range=ALL&fromDate={{1}}&toDate={{1}}&optionType=S";
        public static readonly string SpreadsUrl = $"https://api.tdameritrade.com/v1/marketdata/chains?apikey=SMITH4035&symbol=AMZN&contractType=CALL&strikeCount=15&includeQuotes=TRUE&strategy=VERTICAL&range=OTM&fromDate=2020-05-01&toDate=2020-05-01&optionType=S";
        //public static readonly string AuthorizationUrl = $"https://auth.tdameritrade.com/auth?response_type=code&redirect_uri=http%3A%2F%2Flocalhost&client_id={_apiKey}%40AMER.OAUTHAP";
        public static readonly string AuthorizationUrl = $"https://auth.tdameritrade.com/oauth?client_id={_apiKey}%40AMER.OAUTHAP&response_type=code&redirect_uri=http%3A%2F%2Flocalhost&lang=en-us";
        public static readonly string AuthenticationUrl = $"https://developer.tdameritrade.com/authentication/apis/post/token-0";
        public static readonly string ApiUrl = $"https://developer.tdameritrade.com/apis";
        public static readonly string UserPrincipalsAuthorizationUrl = $"https://developer.tdameritrade.com/user-principal/apis/get/userprincipals-0";
        public static readonly string Quotes = $"https://api.tdameritrade.com/v1/marketdata/{{sSymbol}}/quotes?apikey={TDAConstants._apiKey}";
        public static readonly string PriceHistory = $"https://api.tdameritrade.com/v1/marketdata/{{sSymbol}}/pricehistory?apikey={TDAConstants._apiKey}&periodType=month&period=1&frequencyType=daily&frequency=1";
        public static readonly string EchoWebsocketUrl = $"wss://echo.wss-websocket.net";
        public static readonly string streamerSocketUrl = $"wss://streamer-ws.tdameritrade.com/ws";

    }
    public class TDAConstants
    {
        public static readonly string _apiKey = "SMITH4035";

        public static readonly string _TDASinglesUrl = $"https://api.tdameritrade.com/v1/marketdata/chains?apikey={_apiKey}&symbol={{0}}&contractType=ALL&strikeCount={{2}}&includeQuotes=TRUE&strategy=SINGLE&range=ALL&fromDate={{1}}&toDate={{1}}&optionType=S";
        public static readonly string _TDASpreadsUrl = $"https://api.tdameritrade.com/v1/marketdata/chains?apikey=SMITH4035&symbol=AMZN&contractType=CALL&strikeCount=15&includeQuotes=TRUE&strategy=VERTICAL&range=OTM&fromDate=2020-05-01&toDate=2020-05-01&optionType=S";
        //public static readonly string _TDAAuthorizationUrl = $"https://auth.tdameritrade.com/auth?response_type=code&redirect_uri=http%3A%2F%2Flocalhost&client_id={_apiKey}%40AMER.OAUTHAP";
        public static readonly string _TDAAuthorizationUrl = $"https://auth.tdameritrade.com/oauth?client_id={_apiKey}%40AMER.OAUTHAP&response_type=code&redirect_uri=http%3A%2F%2Flocalhost&lang=en-us";
        public static readonly string _TDAAuthenticationUrl = $"https://developer.tdameritrade.com/authentication/apis/post/token-0";
        public static readonly string _TDApiUrl = $"https://developer.tdameritrade.com/apis";

        /// Notification color success values
        public static readonly string Notify_Auth_Success = "OK";
        public static readonly string Notify_Option_Success = "OK";
        public static readonly string Notify_Quote_Success = "OK";

        /// Notification color original state values
        public static readonly string Notify_Auth_Original = "UNSET";
        public static readonly string Notify_Option_Original = "NONE";
        public static readonly string Notify_Quote_Original = "UNUSED";

        public static List<string> TDAresponse_TimeSalesFields = new List<string>() { "symbol", "time", "price", "size", "sequence" };
        public static List<string> TDAresponse_QuoteFields = new List<string>() { "symbol", "bidPrice", "askPrice", "lastPrice", "bidSize", "askSize", "askID", "bidID", "totalVolume", "lastSize", "tradeTime", "quoteTime", "highPrice", "", "", "lowPrice", "bidTick", "closePrice", "exchangeID", "", "", "", "", "", "", "", "marginable", "shortable", "islandBid", "islandAsk", "islandVolume", "quoteDay", "tradeDay", "volatility", "description", "", "lastID", "digits", "openPrice", "", "", "", "", "netChange", "", "", "52 WeekHigh", "52WeekLow", "pERatio", "dividendAmount", "dividendYield", "islandBidSize", "islandAskSize", "nAV", "fundPrice", "exchangeName", "dividendDate", "regularMarketQuote", "regularMarketTrade", "regularMarketLastPrice", "regularMarketLastSize", "regularMarketTradeTime", "regularMarketTradeDay", "regularMarketNetChange" };
        //"1,2,3,4,5,6,7,8,9,19,11,12,15"
        public static List<string> TDAresponse_ChartFields = new List<string>() { "symbol", "open", "high", "low", "close", "volume", "sequence", "time" };
        public static List<string> TDAresponse_ListedBookFields = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        public static Dictionary<string, List<string>> TDAResponseFields = new Dictionary<string, List<string>>()
        {
            { "QUOTE", TDAresponse_QuoteFields },
            {"CHART_EQUITY",TDAresponse_ChartFields},
            {"TIMESALE_EQUITY",TDAresponse_TimeSalesFields},
            {"LISTED_BOOK", TDAresponse_ListedBookFields }
        };
    }
    public class TDATokens
    {
        public static string _TDADecodedAuthCode = "cP4qxZypboTiJ26YNpxKWDZIS1e6loGANH99GKpzTMNl37sCVRqNb6D7hBI7Yu0QGACpFlVhRM447UtASiyScqkGQtA+UqJh5t250/Onthz9L/tMS5hfbz+stIG/KNKXzUf9aYTNKWSbL7ncbq6pQbF2xk/zhQt+7OOOPFVmRANvUV7qb9jjEWkUEF0cQJVKCIYcD7ml2b4sS5DgDP/PPb9DVZNKHlBPbfTgSBwpdH+xbsG+Q3LKRoTFAwW3yrfhA+ucopreXVElKVyluv9o2S9Vmp24tsLYSygTC5VmfQU2vvaoSrFoCv96ymsZ1bJVvhgdhQGyMH44x2uwkEkokycpEvQBBtCybVOrKXZXu+MN3SPL+CWsJWTtaqzoUdlckpihR7Tikh4a1GnCupGK44zTbkrVWBKfmWTQyqxPnZzLg3erTBQT+8CaDiQ100MQuG4LYrgoVi/JHHvlvO2G/XQu905727a5q+I0VfKGKv53exyxgB3GlvvvH0sgPQVv7u9Yu87622AwdtcIkYmPgobovpA79r8zUQb3syHHvogTAPYlvHqbPY+Ip5jezF7YR5fdFHSFkvPZK8EQqTMxuO0Lclt8LqgvD85EYwPr0k7BZEKqNdHzuo9bRERyhgvb59EgnW8n2lNlVIJmpUd5BTVDSnIBEGY7COSJ8xpaiN/o8dyl29elsNQCKsxdrTqzZv1J4j3jfFZ+hB/o9lEYIqHXCn3+kicPY63NYPCkSS+xh0fLbrCzRSxBGuki3I34SiDTwNIOgOPxOSe1JJaayAqCKfnNBwmRnPG9Abkvdt4i5w2mh6F0QRFWzb9NCAATQW2jwPEJbJrO28fdHH/eKvCP62kFj5AKZs2NwT2gT9rWF0HrQ6HultpUniBTjBbqYdmEna5xgus=212FD3x19z9sWBHDJACbC00B75E";
        public static string _TDAAuthCode = "cP4qxZypboTiJ26YNpxKWDZIS1e6loGANH99GKpzTMNl37sCVRqNb6D7hBI7Yu0QGACpFlVhRM447UtASiyScqkGQtA%2BUqJh5t250%2FOnthz9L%2FtMS5hfbz%2BstIG%2FKNKXzUf9aYTNKWSbL7ncbq6pQbF2xk%2FzhQt%2B7OOOPFVmRANvUV7qb9jjEWkUEF0cQJVKCIYcD7ml2b4sS5DgDP%2FPPb9DVZNKHlBPbfTgSBwpdH%2BxbsG%2BQ3LKRoTFAwW3yrfhA%2BucopreXVElKVyluv9o2S9Vmp24tsLYSygTC5VmfQU2vvaoSrFoCv96ymsZ1bJVvhgdhQGyMH44x2uwkEkokycpEvQBBtCybVOrKXZXu%2BMN3SPL%2BCWsJWTtaqzoUdlckpihR7Tikh4a1GnCupGK44zTbkrVWBKfmWTQyqxPnZzLg3erTBQT%2B8CaDiQ100MQuG4LYrgoVi%2FJHHvlvO2G%2FXQu905727a5q%2BI0VfKGKv53exyxgB3GlvvvH0sgPQVv7u9Yu87622AwdtcIkYmPgobovpA79r8zUQb3syHHvogTAPYlvHqbPY%2BIp5jezF7YR5fdFHSFkvPZK8EQqTMxuO0Lclt8LqgvD85EYwPr0k7BZEKqNdHzuo9bRERyhgvb59EgnW8n2lNlVIJmpUd5BTVDSnIBEGY7COSJ8xpaiN%2Fo8dyl29elsNQCKsxdrTqzZv1J4j3jfFZ%2BhB%2Fo9lEYIqHXCn3%2BkicPY63NYPCkSS%2Bxh0fLbrCzRSxBGuki3I34SiDTwNIOgOPxOSe1JJaayAqCKfnNBwmRnPG9Abkvdt4i5w2mh6F0QRFWzb9NCAATQW2jwPEJbJrO28fdHH%2FeKvCP62kFj5AKZs2NwT2gT9rWF0HrQ6HultpUniBTjBbqYdmEna5xgus%3D212FD3x19z9sWBHDJACbC00B75E";
        public static string _TDAAccessToken = "g7RqVc3k1VMb68IIC7zXwqlfP1pXROUN806jsmNSolNYP7zSzNHy8fjYq00wW+Njp4IVA+r0J9xZCYR/bMQl9w7yn2Eica9+dzsEUx2iPVCXKjZfkKyvsa0Z6edqqd7sszN5QVxg8thT5Hs3FBGzpPoDbkkhpb9kzuhQ3BRXPKGJljivwrIn9DnoSlx0+W7OzYjkWmmwQknsQBMDdqauxcs8kj1HTukRxRcY+LFgzrSEnwkTvOjO8gtyAjxN/9K5xxMuyvr9S9V64HjnM6aP8br6QB4z0ZmGrzuw0BoGjQBAcKsy4JJFLcEDoRZR4D82Cf7H3vsnZrTrJbpeSaB9GNtfU9QbYRyA2se8NwsrzOtxRv15t4CDOWdU6WWBIUV97ElcGnIAwzBU6fgWhIgAYbhm50pINtB1NM5M+vKqjM71XjrT4CfbiGC0ZyimL30UUw/3O8Nqe0/0dyK+bvJNEYQmDVLiCS2yaby6PiNDwqXx7ls4repXwDlHfZULwBR4q/4UY20dxew2hV9F7ku2DSx2eoOuO5INBEs+y100MQuG4LYrgoVi/JHHvlUeJlPtKu+TnPY1RmBU/VpwuQuaZDIjl7MNjL10KPxnpBdD7JMVq5/PciQbwse7L1r6VaVe3UMX1cn+UMvFWBMuwOp7AbeMyPXYK2qLWi522uADRa3OFbHEHVj5ZxGKR9Ze89WaZ1dgUZiYetyHCar+/vbJUuF2JOt8E+NbQZHr4IfZP/ZrU5LBHMEovyziiifX6fsHuRYy8axnbg+6S+HD/yXJXsZ3NWkmeTWK0fdvIb7sEw+XhzakM/O9A5ZD6CrmmZ7zadtzqWTHwYYDQd5pPN2re0cTdkdzy2Mj+DxFrEODPdTILyFpKBvtzIhdkqwR8gkprI4/k92LGdkZUE/A825/yLa9U7yHzc1YFcT178Htn3OZjEnOvC4kz3yaX0ctKRoCZL0azXzq9AQKb+SYWqaM0LlKfE5UjglOVuNbMlOZLrhC/q6S+RsUJ1U0hMQrL2NGi+IXngaM/JowleTdNPcQ8V2fBa+YInsAYhFyCpUO6xNFzGlGChoOxes6adLXYNSyOr2Iyb+AWLN39VHXWdDgayjlrTXRhcFr212FD3x19z9sWBHDJACbC00B75E";
        public static string _TDARefreshToken = "Z6wYMGGjVumAxDG66CtKQDqPhVucuj+rdEd9j/293WBQ38aD8gm6GTur+7Hayf1SfYUFC58+Qwnxe2tZwWEINLfmW13if46005f519XVVUmO+ZL2rfWBZg5klfcUdVhgcqyplJJzJrlsvIYSUHjTiJEg/tdj2U051jfptgzOpmuKRjVp0iM6ZLMcvV6gmI0Ex7yqmI2Nc5aSuBT7JI/1loT29AatrMvA/kRGVJrfA+fMOSmPqc4gzrDiGhQYbJgM2LRI2Y6puFj2h4fFAPUBQsqhYfirjrL/uULgeD8MEbtT7o+L5hEOSOv/ZzLP1cwC0ao5x7B/oTTrh1GXyoNySCzr9CPW9iDwOC0BuPFP0/tf8Oi+NpZ4o59wefbZsv2P4Pi7fCEITApBSdMlKzp0wH2Oz473n5FnwBibIVLNT/abN8Pd4pcZP3uM7Ht100MQuG4LYrgoVi/JHHvlfx98O2Sn+OBdl/i/78rdi7LR0Y0yumrhgy4RTZzCACAQlNKkvrod/ceMuTru/9b3xnW0ysKTMM1CncevwmG5r4KaK7vEUerAJKw/U61x0P8wOYQJ6FoNTEjYrAINbpym0ekUqA2egPVRkjIgKOgMlH9HF8Uwf2UZuQpc1I71dM0sYfKI3gZ92GbqStGrrRwosNSbfRfpVO/b7wjEe89BN2o4FUeI+nr6Ifs3TqcxYKSqmQKxkCUM22nDdMRFjQ2OXCpOyMUHv+lGO3mcObptWLPJXU9no9JMSV5FwUCQiDePVECmczrAzezXk2bew4LbXslYzIHKVDda890n7bNrMgknfe4/3zxic96h7a5ZAPGDe+Y4Sj7Mb8UhwA4ZOKoiSP1QrQfT/4+NErcjKIjOs+hlDwH+EheLUXkEkgcrI1SxkZy4n/bke8ASRSU=212FD3x19z9sWBHDJACbC00B75E";
    }
    /// <summary>
    /// NOTODO: Implement MVVM in Blazor so channging these properties updates the UI
    /// </summary>
    public class TDANotifications
    {
        public static string TDAAuthStatus { get; set; } = TDAConstants.Notify_Auth_Original;
        public static string optionStatus { get; set; } = TDAConstants.Notify_Option_Original;
        public static string quoteStatus { get; set; } = TDAConstants.Notify_Quote_Original;

        public static string errorMessage { get; set; } ="";
        private static DateTime currentDateTime = DateTime.Now;
        internal static TimeZoneInfo est = null;

        public static DateTime _currentDateTime
        {
            get
            {
                // show as EST
                if (est == null) setEST();
                return TimeZoneInfo.ConvertTime(currentDateTime.ToLocalTime(), est);
            }
            set { currentDateTime = value; }
        }

        private static void setEST()
        {
            // Retrieve the time zone for Eastern Standard Time (U.S. and Canada).
            try
            {
                est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                Console.WriteLine("Unable to retrieve the Eastern Standard time zone.");
                return;
            }
            catch (InvalidTimeZoneException)
            {
                Console.WriteLine("Unable to retrieve the Eastern Standard time zone.");
                return;
            }
        }
        //private static string  _pageName = "Home";
        //public static string  pageName
        //{
        //    get { return _pageName; }
        //    set { 
        //        _pageName = value; 
        //        OnStatusesChanged?.Invoke(); 
        //    }
        //}

        //static public event Action OnStatusesChanged;
        //private void StatusChanged() => OnStatusesChanged?.Invoke();
    }
    public class TDACapturedNotifications
    {
        public string TDAAuthStatus { get; set; } = "UNSET";
        public string optionStatus { get; set; } = "NONE";
        public string quoteStatus { get; set; } = "UNUSED";
    }
    public class TDAParameters
    {
        private static TDAOptionQuote[][] _lstOptions;
        public static TDAOptionQuote[][] lstOptions
        {
            get { return _lstOptions; }
            set { _lstOptions = value; }
        }

        public static bool inTheMoney { get; set; } = false;
        public static string optionSymbol { get; set; } = "SPY";
        public static int optionNumContracts { get; set; } = 50;
        public static DateTime? optionExpDate { get; set; } = DateTime.Today.AddDays(((int)DayOfWeek.Friday - (int)DateTime.Today.DayOfWeek + 7) % 7);
        public static int optionNumStrikes { get; set; } = 7;
        public static int optionNumSpreadStrikes { get; set; } = 1;
        public static int optionNumSkipStrikes { get; set; } = 0;

        private static int _sellOptionIndex = -1;
        public static int sellOptionIndex
        {
            get { return _sellOptionIndex; }
            set
            {
                _sellOptionIndex = value;
                isSellOptionOn = _sellOptionIndex != -1;
            }
        }

        public static int optionNumDepthStrikes { get; set; } = 1;
        public static int allocate { get; set; } = 1;
        public static string allocation { get; set; } = "Average";
        public static Dictionary<string, bool> dictIsManualContracts { get; set; } = new Dictionary<string, bool>();  // if true, allocate won't reset value, reset by setting contracts = 0
        public static Dictionary<string, int> dictOptionContracts { get; set; } = new Dictionary<string, int>(); /// Stored so persist's refresh of option quote data
                                                                                                                 /// would be nicer if could init option.Contracts from this store since not getting from quote service anyway
                                                                                                                 /// but means you would use Description vs index to store the # co
                                                                                                                 /// 


        public static Dictionary<string, bool> dictOptionCheckbox { get; set; } = new Dictionary<string, bool>();  // if true, allocate won't reset value, reset by setting contracts = 0

        public static void setOptionCheckbox(string key, bool value)
        {
            if (dictOptionCheckbox.ContainsKey(key))
                dictOptionCheckbox[key] = value;
            else
                dictOptionCheckbox.Add(key, value);
        }

        /// public static Dictionary<string, int> dictOptionContracts { get; set; } = new Dictionary<string, int>(); /// Stored so persist's refresh of option quote data
        public static bool isManualContracts { get; set; } = false;
        public static bool showUnderlying { get; set; } = false;
        public static bool isSellOptionOn { get; set; } = false;

        public static string webSocketUrl { get; set; } = TDAmertradeApiEndpoints.EchoWebsocketUrl;
        public static string webSocketName { get; set; } = "Echo Server";
        public static Dictionary<string, Quote_Content> staticQuote { get; set; } = new Dictionary<string, Quote_Content>();
}
public class TDA
    {
        public static TDASingleOptionQuotes quotes { get; set; }
        public static TDASingleOptionQuotes prevQuotes { get; set; }

        public static TDAOptionQuote[][] callOptions { get; set; }
        public static TDAOptionQuote[][] putOptions { get; set; }

        public static TDAOptionQuote[][] otmCallOptions { get; set; }
        public static TDAOptionQuote[][] otmPutOptions { get; set; }

        public static TDAOptionQuote[][] prevCallOptions { get; set; }
        public static TDAOptionQuote[][] prevPutOptions { get; set; }
        protected float premiumAmt(TDAOptionQuote q)
        {
            return 1.0F;
        }
        public static string optionsDate(bool isPut)
        {
            string sDate = ((TDAOptionQuote[])((TDAOptionQuote[][])callOptions)[0])[0].description;
            sDate = string.Join(' ', sDate.Split(' ').Take(4));
            if (isPut)
                return "Puts " + sDate;
            else
                return "Calls " + sDate;
        }

        public class TDAStreamingData
        {
            public Datum[] data { get; set; }
        }

        public class Datum
        {
            public string service { get; set; }
            public long timestamp { get; set; }
            public string command { get; set; }
            public Content[] content { get; set; }
        }

        public class Content
        {
            public dynamic _0 { get; set; }
            public dynamic _1 { get; set; }
            public dynamic _2 { get; set; }
            public dynamic _3 { get; set; }
            public dynamic _4 { get; set; }
            public dynamic _5 { get; set; }
            public dynamic _6 { get; set; }
            public dynamic _7 { get; set; }
            public dynamic _8 { get; set; }
            public dynamic _9 { get; set; }
            public dynamic _10 { get; set; }
            public dynamic _11 { get; set; }
            public dynamic _12 { get; set; }
            public dynamic _13 { get; set; }
            public dynamic _14 { get; set; }
            public dynamic _15 { get; set; }
            public dynamic _16 { get; set; }
            public dynamic _17 { get; set; }
            public dynamic _18 { get; set; }
            public dynamic _19 { get; set; }
            public dynamic _20 { get; set; }
            public dynamic _21 { get; set; }
            public dynamic _22 { get; set; }
            public dynamic _23 { get; set; }
            public dynamic _24 { get; set; }
            public dynamic _25 { get; set; }
            public dynamic _26 { get; set; }
            public dynamic _27 { get; set; }
            public dynamic _28 { get; set; }
            public dynamic _29 { get; set; }
        }

    }

    public class TDAAuthentication
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public int expires_in { get; set; }
        public int refresh_token_expires_in { get; set; }
        public string token_type { get; set; }
    }


    public class TDAStockQuotes
    {
        public TDAStockQuote stockQuote { get; set; }
    }

    public class TDAStockQuote
    {
        public string assetType { get; set; }
        public string assetMainType { get; set; }
        public string cusip { get; set; }
        public string assetSubType { get; set; }
        public string symbol { get; set; }
        public string description { get; set; }
        public float bidPrice { get; set; }
        public int bidSize { get; set; }
        public string bidId { get; set; }
        public float askPrice { get; set; }
        public int askSize { get; set; }
        public string askId { get; set; }
        public float lastPrice { get; set; }
        public int lastSize { get; set; }
        public string lastId { get; set; }
        public float openPrice { get; set; }
        public float highPrice { get; set; }
        public float lowPrice { get; set; }
        public string bidTick { get; set; }
        public float closePrice { get; set; }
        public float netChange { get; set; }
        public int totalVolume { get; set; }
        public long quoteTimeInLong { get; set; }
        public long tradeTimeInLong { get; set; }
        public float mark { get; set; }
        public string exchange { get; set; }
        public string exchangeName { get; set; }
        public bool marginable { get; set; }
        public bool shortable { get; set; }
        public float volatility { get; set; }
        public int digits { get; set; }
        public float _52WkHigh { get; set; }
        public float _52WkLow { get; set; }
        public float nAV { get; set; }
        public float peRatio { get; set; }
        public float divAmount { get; set; }
        public float divYield { get; set; }
        public string divDate { get; set; }
        public string securityStatus { get; set; }
        public float regularMarketLastPrice { get; set; }
        public int regularMarketLastSize { get; set; }
        public float regularMarketNetChange { get; set; }
        public long regularMarketTradeTimeInLong { get; set; }
        public float netPercentChangeInDouble { get; set; }
        public float markChangeInDouble { get; set; }
        public float markPercentChangeInDouble { get; set; }
        public float regularMarketPercentChangeInDouble { get; set; }
        public bool delayed { get; set; }
    }


    public class TDACandleList
    {
        public TDACandle[] candles { get; set; }
        public string symbol { get; set; }
        public bool empty { get; set; }
    }

    public class TDACandle
    {
        public float open { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public float close { get; set; }
        public int volume { get; set; }
        public long datetime { get; set; }
    }


    public class TDAUserPrincipalInfo
    {
        public string authToken { get; set; }
        public string userId { get; set; }
        public string userCdDomainId { get; set; }
        public string primaryAccountId { get; set; }
        public string lastLoginTime { get; set; }
        public string tokenExpirationTime { get; set; }
        public string loginTime { get; set; }
        public string accessLevel { get; set; }
        public bool stalePassword { get; set; }
        public Streamerinfo streamerInfo { get; set; }
        public string professionalStatus { get; set; }
        public Quotes quotes { get; set; }
        public Streamersubscriptionkeys streamerSubscriptionKeys { get; set; }
        public Account[] accounts { get; set; }
    }

    public class Streamerinfo
    {
        public string streamerBinaryUrl { get; set; }
        public string streamerSocketUrl { get; set; }
        public string token { get; set; }
        public string tokenTimestamp { get; set; }
        public string userGroup { get; set; }
        public string accessLevel { get; set; }
        public string acl { get; set; }
        public string appId { get; set; }
    }

    public class Quotes
    {
        public bool isNyseDelayed { get; set; }
        public bool isNasdaqDelayed { get; set; }
        public bool isOpraDelayed { get; set; }
        public bool isAmexDelayed { get; set; }
        public bool isCmeDelayed { get; set; }
        public bool isIceDelayed { get; set; }
        public bool isForexDelayed { get; set; }
    }

    public class Streamersubscriptionkeys
    {
        public Key[] keys { get; set; }
    }

    public class Key
    {
        public string key { get; set; }
    }

    public class Account
    {
        public string accountId { get; set; }
        public string displayName { get; set; }
        public string accountCdDomainId { get; set; }
        public string company { get; set; }
        public string segment { get; set; }
        public string acl { get; set; }
        public Authorizations authorizations { get; set; }
    }

    public class Authorizations
    {
        public bool apex { get; set; }
        public bool levelTwoQuotes { get; set; }
        public bool stockTrading { get; set; }
        public bool marginTrading { get; set; }
        public bool streamingNews { get; set; }
        public string optionTradingLevel { get; set; }
        public bool streamerAccess { get; set; }
        public bool advancedMargin { get; set; }
        public bool scottradeAccount { get; set; }
    }


    public class TDAStreamingCredentials
    {
        public string userid { get; set; }
        public string token { get; set; }
        public string company { get; set; }
        public string segment { get; set; }
        public string cddomain { get; set; }
        public string usergroup { get; set; }
        public string accesslevel { get; set; }
        public string authorized { get; set; }
        public long timestamp { get; set; }
        public string appid { get; set; }
        public string acl { get; set; }
    }


    public class TDAStreamingRequests
    {
        public Request[] requests { get; set; }
    }

    public class Request
    {
        public string service { get; set; }
        public string command { get; set; }
        public string requestid { get; set; }
        public string account { get; set; }
        public string source { get; set; }
        public Parameters parameters { get; set; }
    }

    public class Parameters
    {
        public string credential { get; set; }
        public string token { get; set; }
        public string version { get; set; }
    }

    public class dataRequests
    {
        public dataRequest[] requests { get; set; }
    }
    public class dataRequest
    {
        public string service { get; set; }
        public string requestid { get; set; }
        public string command { get; set; }
        public string account { get; set; }
        public string source { get; set; }
        public dataRequestParameters parameters { get; set; }
    }

    public class dataRequestParameters
    {
        public string keys { get; set; }
        public string fields { get; set; }
    }

    public class Quote_Response
    {
        public Quote_Datum[] data { get; set; }
    }

    public class Quote_Datum
    {
        public string service { get; set; }
        public long timestamp { get; set; }
        public string command { get; set; }
        public Quote_Content[] content { get; set; }
    }

    public class Quote_Content
    {
        public float bidPrice { get; set; }
        public float askPrice { get; set; }
        public float lastPrice { get; set; }
        public int bidSize { get; set; }
        public int askSize { get; set; }
        public string askID { get; set; }
        public string bidID { get; set; }
        public int totalVolume { get; set; }
        public int lastSize { get; set; }
        public long tradeTime { get; set; }
        public long quoteTime { get; set; }
        public float highPrice { get; set; }
        public float lowPrice { get; set; }
        public string key { get; set; }
        public bool delayed { get; set; }
       /// <summary>
       /// To access properties by name
       /// Because streamer only sends partial quote updates, 
       /// we need to figure out which properties have changed and 
       /// only update them in our static quote object
       /// </summary>
       /// <param name="propertyName"></param>
       /// <returns></returns>
       public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }

    public class Chart_Response
    {
        public Chart_Datum[] data { get; set; }
    }

    public class Chart_Datum
    {
        public string service { get; set; }
        public long timestamp { get; set; }
        public string command { get; set; }
        public Chart_Content[] content { get; set; }
    }

    public class Chart_Content
    {
        public float open { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public float close { get; set; }
        public float volume { get; set; }
        public int sequence { get; set; }
        public long time { get; set; }
        public int seq { get; set; }
        public string key { get; set; }
        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }

    public class TimeSales_Responses
    {
        public TimeSales_Response[] data { get; set; }
    }

    public class TimeSales_Response
    {
        public string service { get; set; }
        public long timestamp { get; set; }
        public string command { get; set; }
        public TimeSales_Content[] content { get; set; }
    }

    public class  TimeSales_Content
    {
        public int seq { get; set; }
        public string key { get; set; }        
        public long time { get; set; }
        public float price { get; set; }
        public float size { get; set; }
        public int sequence { get; set; }
        public float bid  { get; set; }
        public float ask  { get; set; }
        public float last  { get; set; }
        public int bidSize { get; set; }
        public int askSize { get; set; }
        public int lastSize { get; set; }
        public long quoteTime { get; set; }
        public long tradeTime { get; set; }
        public int level { get; set; }
        public float bidIncr { get; set; }
        public float askIncr { get; set; }
        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }

    public class CustomerClass
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }


}
