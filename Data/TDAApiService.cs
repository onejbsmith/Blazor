using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorTrader.Data
{
    public class TDAApiService
    {
        /// <summary>
        /// TODONT: Service needs to raise events so data can be updated in any Component that uses the service - NEEDS WORK
        /// </summary>

        public HttpClient _httpClient = new HttpClient();
        static public event Action OnStatusesChanged;
        private static void StatusChanged() => OnStatusesChanged?.Invoke();
        public TDAApiService()
        {

        }
        public TDAApiService(HttpClient client)
        {
            _httpClient = client;
        }
        public async Task<ComplexOptionChain> GetSpreadsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(TDAConstants._TDASpreadsUrl);
                response.EnsureSuccessStatusCode();

                using var responseContent = await response.Content.ReadAsStreamAsync();
                var xx = await JsonSerializer.DeserializeAsync<ComplexOptionChain>(responseContent);
                return xx;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return null;
            }
        }

        public async Task<TDASingleOptionQuotes> GetSinglesAsync(string sSymbol, DateTime? optionExpDate, int optionNumStrikes)
        {
            TDANotifications._currentDateTime = DateTime.Now;
            //OnStatusesChanged += TDAApiService_OnStatusesChanged;
            try
            {
                _httpClient.DefaultRequestHeaders.Add("access_token", TDAConstants._apiKey);

                string expDate = ((DateTime)optionExpDate).ToString("yyyy-MM-dd");
                var response = await _httpClient.GetAsync(string.Format(TDAConstants._TDASinglesUrl, sSymbol, expDate, optionNumStrikes));


                if (TDANotifications.optionStatus != response.StatusCode.ToString())
                {
                    TDANotifications.optionStatus = response.StatusCode.ToString();
                    StatusChanged();
                }
                response.EnsureSuccessStatusCode();
                using var responseContent = await response.Content.ReadAsStreamAsync();
                var optionQuotes = await JsonSerializer.DeserializeAsync<TDASingleOptionQuotes>(responseContent);

                var stock = await GetQuote(sSymbol);
                /// Update underlying with real-time info from quote
                if (stock.symbol == sSymbol)
                {

                    if (optionQuotes.underlying == null)
                    {
                        optionQuotes.underlying = new UnderlyingStock();
                        optionQuotes.underlying.symbol = sSymbol;
                    }

                    optionQuotes.underlying.quoteTime = stock.quoteTimeInLong;
                    optionQuotes.underlying.bid = stock.bidPrice;
                    optionQuotes.underlying.ask = stock.askPrice;
                    optionQuotes.underlying.close = stock.lastPrice;
                    optionQuotes.underlying.openPrice = stock.openPrice;
                    optionQuotes.underlying.percentChange = stock.markPercentChangeInDouble;
                }
                return optionQuotes;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                    TDANotifications.errorMessage = ex.ToString();
             
                return null;
            }
            finally
            {
            }
        }

        //private void TDAApiService_OnStatusesChanged()
        //{
        //    throw new NotImplementedException();
        //}

        public static async Task<TDAStockQuote> GetQuote(string sSymbol)
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

                response = await GetQuoteResponseAsync(sSymbol);
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
                    await GetAuthenticationAsync();
                }
                return quote;
            }
        }

        public  async Task<TDAStockQuote> GetStaticQuote(string sSymbol)
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
                    await GetAuthenticationAsync();
                }
                return quote;
            }
        }

        private static async Task<HttpResponseMessage> GetQuoteResponseAsync(string sSymbol)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://api.tdameritrade.com/v1/marketdata/{sSymbol}/quotes?apikey={TDAConstants._apiKey}");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {TDATokens._TDAAccessToken}");
            return await httpClient.SendAsync(request);
        }
        ///  TODO: Consider showing green and red for increased ans decreased numbers, 
        ///  TODO: See if there is a grid event that will enabled this

        private async Task<HttpResponseMessage> GetCandlesResponseAsync(string sSymbol)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod("GET"), $"https://api.tdameritrade.com/v1/marketdata/{sSymbol}/pricehistory?apikey={TDAConstants._apiKey}&periodType=month&period=1&frequencyType=daily&frequency=1");
            //request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {TDATokens._TDAAccessToken}");
            return await httpClient.SendAsync(request);
        }
        public async Task<object> GetAuthorization()
        {
            try
            {
                var response = await _httpClient.GetAsync(TDAConstants._TDAAuthorizationUrl);
                response.EnsureSuccessStatusCode();
                using System.IO.Stream responseContent = await response.Content.ReadAsStreamAsync();
                byte[] buf = new byte[responseContent.Length];
                responseContent.Read(buf, 0, (int)responseContent.Length);
                string s = System.Text.Encoding.UTF8.GetString(buf, 0, buf.Length);
                return s;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                return null;
            }
        }

        public static async Task<TDAAuthentication> GetAuthentication(string sAuthToken, bool isRefreshToken = false)
        {
            var authObject = new TDAAuthentication();
            try
            {
                string url = "https://api.tdameritrade.com/v1/oauth2/token";
                var client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", @"application/x-www-form-urlencoded");
                client.DefaultRequestHeaders.Add("access_token", TDAConstants._apiKey);

                var urlData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("refresh_token",""),
                new KeyValuePair<string, string>("access_type", "offline"),
                new KeyValuePair<string, string>("code", sAuthToken),
                new KeyValuePair<string, string>("client_id", TDAConstants._apiKey),
                new KeyValuePair<string, string>("redirect_uri", "http://localhost")
            };
                if (isRefreshToken)
                    urlData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token",sAuthToken),
                        new KeyValuePair<string, string>("client_id", TDAConstants._apiKey),
                    };

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(urlData) };
                var response = await client.SendAsync(request);
                TDANotifications.TDAAuthStatus = response.StatusCode.ToString();
                response.EnsureSuccessStatusCode();

                // Get the Json payload as a string
                string content = await response.Content?.ReadAsStringAsync();
                authObject = JsonSerializer.Deserialize<TDAAuthentication>(content);
                if (isRefreshToken)
                {
                    // Update access token if refresh 
                    string contentBefore = System.IO.File.ReadAllText("TDAAuth.json");
                    var authObjectBefore = JsonSerializer.Deserialize<TDAAuthentication>(contentBefore);
                    authObject.refresh_token = authObjectBefore.refresh_token;
                    authObject.refresh_token_expires_in = authObjectBefore.refresh_token_expires_in;
                    content = JsonSerializer.Serialize<TDAAuthentication>(authObject);
                }
                System.IO.File.WriteAllText("TDAAuth.json", content);
                // Return
                TDATokens._TDAAccessToken = authObject.access_token;
                return authObject;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return authObject;
            }
        }
        /// <summary>
        /// Use the TDAAuth.json file to get the access token 
        /// </summary>
        /// <returns></returns>
        public static async Task<TDAAuthentication> GetAuthenticationAsync()
        {
            try
            {
                // Read contents of file
                string content = await System.IO.File.ReadAllTextAsync("TDAAuth.json");
                // Use contents to hydrate a TDAAuthentication object
                var authObject = JsonSerializer.Deserialize<TDAAuthentication>(content);
                // Set the global access and refresh tokens
                TDATokens._TDAAccessToken = authObject.access_token;
                TDATokens._TDARefreshToken = authObject.refresh_token;
                // Use the refresh token to obtain a new access token
                // (and store the access token in the file)
                await GetAuthentication(TDATokens._TDARefreshToken, true);

                // reset the Auth Status lamp by testing the quote service
                var response = await GetQuoteResponseAsync("SPY");
                TDANotifications.TDAAuthStatus = response.StatusCode.ToString();

                return authObject;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new TDAAuthentication();
            }
        }

        public async Task<TDACandleList> GetCandles()
        {
            var candles = new TDACandleList();
            await Task.FromResult(true);
            return candles;
        }

        public void captureUserPrincipalInfo(string _TDAUserPrincipalJson)
        {
            System.IO.File.WriteAllText("TDAUserPrincipalAuth.json", _TDAUserPrincipalJson);
        }

    }
}


