using tdaStreamHub.Data;
using tapeStream.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Timers;
using System.Text.Json;
using static tapeStream.Shared.CONSTANTS;

namespace tdaStreamHub.Components
{
    public partial class TDAStreamer
    {
        [Inject]
        BrowserService Service { get; set; }
        [Inject]
        BlazorTimer Timer { get; set; }

        [Inject]
        IJSRuntime TDAStreamerJs { get; set; }

        #region Variables
        [Parameter]
        public string symbol { get; set; }

        //public ObservableCollection<SparkData> DataSource { get; set; }
        DateTime svcDateTime = DateTime.Now;
        private string admCode = "-1";
        private string admStatus = "Not started";
        static string clockFormat = "h:mm:ss MMM d, yyyy";
        static string timerFormat = "h'h 'm'm 's's'";
        static string dateFormat = "h:mm:ss tt dddd MMM d, yyyy";
        string clock = DateTime.Now.ToString(clockFormat);
        string serviceRequestText = "";
        string serviceSelection = "Service Request";
        string logText = "";
        string logTopics = "";

        static string status = "None";
        string statusClass = "primary";

        StringBuilder logTextsb = new StringBuilder();  /// Called from Javascript
        StringBuilder logTopicsb = new StringBuilder();  /// Called from Javascript
        //string feedFile, chartFile, quoteFile = "";

        double strike = 0;

        IEnumerable<int> values = new int[] { 1, 2, 4, 5 };
        readonly string[] valuesName = CONSTANTS.valuesName;
        int[] valuesCounts = new int[] { 999, 0, 0, 0, 0, 0, 0, 0, 0 };

        Dictionary<string, int> dictTopicCounts = new Dictionary<string, int>();

        DateTime optionExpDate = DateTime.Now.AddDays(1);

        private HubConnection hubConnection;

        private string dateTimeNow = DateTime.Now.ToString(dateFormat);

        bool? logStreamer = false;
        bool logHub = false;
        string sinceLastData = "0";
        string elapsed = "0";
        DateTime startedTime = DateTime.Now;
        Timer timer = new Timer(1000);

        public int Height { get; set; }
        public int Width { get; set; }

        double value = 0;

        #endregion


        #region Page Event Handlers   
        protected override async Task OnInitializedAsync()
        {
            dictTopicCounts = new Dictionary<string, int>();
            foreach (var x in CONSTANTS.valuesName)
                dictTopicCounts.Add(x, 0);

            /// Connect to the web socket, passing it a ref to this page, so it can call methods from javascript
            var dotNetReference = DotNetObjectReference.Create(this);
            var dud = await TDAStreamerJs.InvokeAsync<string>("Initialize", dotNetReference);
            var dud2 = await TDAStreamerJs.InvokeAsync<string>("Connect");
            //feedFile = FilesManager.GetFileNameForToday("FEED");
            //quoteFile = FilesManager.GetFileNameForToday(@$"{symbol} QUOTES");
            //chartFile = FilesManager.GetFileNameForToday(@$"{symbol} CANDLES");
            await TDAApiService.GetAuthenticationAsync();


            await Task.CompletedTask;
            /// Get the current QQQ price so can determine options to track


            List<DayOfWeek> optionDaysOfWeek = new List<DayOfWeek>(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });
            while (!optionDaysOfWeek.Contains(optionExpDate.DayOfWeek))
            {
                optionExpDate = optionExpDate.AddDays(1);
            }
            TDAStreamerData.expiryDate = optionExpDate;

            timer.Elapsed += async (sender, e) => await Timer_ElapsedAsync();
            timer.Start();

            await Init();

            TDAStreamerData.OnTimeSalesStatusChanged += sendTimeSalesData;


        }

        async Task GetDimensions()
        {
            var dimension = await Service.GetDimensions();
            Height = dimension.Height;
            Width = dimension.Width;
        }


        private async Task StateChangedAsync()
        {
            await InvokeAsync(() => StateHasChanged());
        }


        protected void serviceRequestChanged(RadzenSplitButtonItem item)
        {
            if (item == null) return;

            serviceSelection = item.Value;
            TDAStreamerData.chain = "102120C337,102120P337";
            serviceRequestText = TDAStreamerData.getServiceRequestOld(serviceSelection);
            //LogText(serviceRequestText);
        }

        void Change(IEnumerable<int> value, string name)
        {
            //var str = string.Join(", ", value);
            //events.Add(DateTime.Now, $"{name} value changed to {str}");
        }

        #endregion

        #region Button Event Handlers
        protected async Task Login()
        {
            string request = TDAStreamerData.getLoginRequest();

            var dud = await TDAStreamerJs.InvokeAsync<string>("tdaSendRequest", request);

            StateHasChanged();
        }

        protected async Task Sends()
        {


            //var servicesSelected = string.Join(',', values.Select(i => valuesName[i]));
            serviceRequestText = await TDAStreamerData.getServiceRequest(values);
            await TDAStreamerJs.InvokeAsync<string>("tdaSendRequest", serviceRequestText);
        }

        protected void Ping()
        {
            StateHasChanged();
        }

        protected async Task Logout()
        {
            string request = TDAStreamerData.getLoginRequest(isLogout: true);

            var dud = await TDAStreamerJs.InvokeAsync<string>("tdaSendRequest", request);

            StateHasChanged();
        }

        protected async Task Requested()
        {
            await Logout();
            await Login();
            await Login();
            await Sends();
        }
        void logStreamerChange(bool? arg, string comment)
        {
            if (arg == false)
            {
                logText = "";
                StateHasChanged();
            }
        }
        void logHubChange(bool? arg, string comment)
        {
            if (arg == false)
            {
                logTopics = "";
                StateHasChanged();
            }
        }
        #endregion

        #region External Event Handlers

        private void sendTimeSalesData()
        {
            // Send("TimeAndSales", JsonSerializer.Serialize<TimeSales_Content>(TDAStreamerData.timeAndSales));
            sendPrintsData();
        }

        Dictionary<int, DataItem[]> dictPies = new Dictionary<int, DataItem[]>();
        void sendPrintsData()
        {
            //if (moduloPrints ==0 || TDAStreamerData.timeSales[symbol].Count() % moduloPrints != 0) return;

            // Only summarize once every new 2 time and sales
            if (TDAStreamerData.timeSales[symbol].Count % 2 != 0) return;

            value = TDAPrints.GetPrintsGaugeScore(symbol, ref dictPies);
            KeyValuePair<DateTime, double> pair = new KeyValuePair<DateTime, double>(DateTime.Now, value);

            Send("GaugeScore", JsonSerializer.Serialize<KeyValuePair<DateTime, double>>(pair));
            Send("PrintsPies", JsonSerializer.Serialize<Dictionary<int, DataItem[]>>(dictPies));


            //TDAStreamerData.gaugeValues.Add(DateTime.Now, value);
            //TDAStreamerData.gaugeValues.RemoveAll((key, val) => key < DateTime.Now.AddSeconds(-printSeconds.Max()));

            StateHasChanged();
        }



        private async Task Timer_ElapsedAsync()
        {
            dateTimeNow = DateTime.Now.ToString(dateFormat);
            sinceLastData = DateTime.Now.Subtract(svcDateTime.AddMilliseconds(-1600)).ToString(timerFormat);
            elapsed = DateTime.Now.Subtract(startedTime).ToString(timerFormat);
            await StateChangedAsync();
        }

        #endregion

        #region Utility
        void LogText(string text)
        {
            if (logStreamer == false) return;

            logTextsb.Insert(0, "\n" + text);
            logText = string.Join('\n', logTextsb.ToString().Split('\n').Take(10));

            StateHasChanged();
        }

        void LogTopic(string topic, string content)
        {
            // Show the topic text (last 1000 lines)
            logTopicsb.Insert(0, "\n" + topic + ":" + content.Replace("\r", "").Replace("\n", ""));
            logTopics = string.Join('\n', logTopicsb.ToString().Split('\n').Take(10));

            // Update topic's Stats count
            dictTopicCounts[topic] += 1;
            StateHasChanged();
        }
        #endregion

        #region WebSocket Responses
        // Called from javascript
        [JSInvokable("TDAStreamerStatus")]
        public void TDAStreamerStatus(string it)
        {
            switch (it)
            {
                case "0": status = "CONNECTING"; break;
                case "1": status = "OPEN"; statusClass = "badge-success"; break;
                case "2": status = "CLOSING"; statusClass = "badge-warning"; break;
                case "3": status = "CLOSED"; statusClass = "badge-danger"; break;
            }
            LogText($"STATUS:{status}");
            StateHasChanged();
        }

        List<string> lstJson = new List<string>();
        private string loginStatus;
        private string adminMsg;

        [JSInvokable("TDAStreamerOnMessage")]
        public async Task TDAStreamerOnMessage(string jsonResponse)
        {
            await Task.Yield();

            LogText("RECEIVED: " + jsonResponse);

            var fieldedResponse = jsonResponse;
            if (jsonResponse.Contains("\"data\":"))
            {
                await TDA_Process_Data(jsonResponse);
            }
            else if (jsonResponse.Contains("\"notify\":"))
            {
                TDA_Process_Heartbeat(jsonResponse);
            }
            else
            {
                TDA_Process_Admin(jsonResponse);
            }
            StateHasChanged();
        }

        private async Task TDA_Process_Data(string jsonResponse)
        {
            var dataJsonSvcArray = JObject.Parse(jsonResponse)["data"];
            foreach (var svcJsonObject in dataJsonSvcArray)
            {
                var svcName = svcJsonObject["service"].ToString();
                svcDateTime = GetServiceTime(svcJsonObject);

                var svcJson = svcJsonObject.ToString();
                var svcFieldedJson = svcJson;

                /// Decode
                /// Get field names corresponding to field numbers 
                List<string> svcFields = TDAConstants.TDAResponseFields[svcName];
                /// Replace field numbers with field names  
                for (int i = 1; i < svcFields.Count; i++)
                {
                    string sIndex = $"\"{i}\":";
                    svcFieldedJson = svcFieldedJson.Replace(sIndex, $" \"{svcFields[i]}\":");
                }

                LogText("DECODED: " + svcFieldedJson);

                var svcJsonObjectDecoded = JObject.Parse(svcFieldedJson);
                var contents = svcJsonObjectDecoded["content"];

                /// Send to connected hub
                /// 
                await TDAStreamerData.captureTdaServiceData(svcFieldedJson);
                await Send(svcName, svcFieldedJson);

            }
        }

        private void TDA_Process_Heartbeat(string jsonResponse)
        {
            var dataJsonSvcArray = JObject.Parse(jsonResponse)["notify"];
            var svcJsonObject = dataJsonSvcArray[0];
            if (svcJsonObject["heartbeat"] != null)
            {
                svcDateTime = GetServiceTime(svcJsonObject, "heartbeat");
                LogText("DECODED: " + jsonResponse + " " + svcDateTime.TimeOfDay);
            }
        }

        private void TDA_Process_Admin(string jsonResponse)
        {
            var dataJsonSvcArray = JObject.Parse(jsonResponse)["response"];
            var svcJsonObject = dataJsonSvcArray[0];
            if (svcJsonObject["service"] != null)
            {
                var svcName = ((Newtonsoft.Json.Linq.JValue)svcJsonObject["service"]).Value;
                if (svcName.ToString() == "ADMIN")
                {
                    svcDateTime = GetServiceTime(svcJsonObject);
                    var content = (Newtonsoft.Json.Linq.JObject)svcJsonObject["content"];
                    var code = ((Newtonsoft.Json.Linq.JValue)content["code"]).Value.ToString();
                    var cmd = ((Newtonsoft.Json.Linq.JValue)svcJsonObject["command"]).Value.ToString();
                    admCode = ((Newtonsoft.Json.Linq.JValue)content["msg"]).Value.ToString();
                    admStatus = $"{cmd} at {svcDateTime} : Code {code}";
                    LogText("DECODED: " + jsonResponse + " " + svcDateTime.TimeOfDay);
                }
            }
        }


        private DateTime GetServiceTime(JToken svcJsonObject, string timeField = "timestamp")
        {
            var svcEpochTime = Convert.ToInt64(svcJsonObject[timeField]);
            var svcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0)
                .AddMilliseconds(svcEpochTime).ToLocalTime();
            clock = svcDateTime.ToString(clockFormat);
            return svcDateTime;
        }
        #endregion

        #region SignalR Client

        private async Task Init()
        {
            hubConnection = new HubConnectionBuilder().WithUrl("https://localhost:44367/tdahub").Build();

            hubConnection.On("ReceiveMessage", (Action<string, string>)((user, message) =>
            {
                Receive(user, message);
            }));

            hubConnection.On("NASDAQ_BOOK", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("TIMESALE_EQUITY", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("CHART_EQUITY", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("OPTION", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("QUOTE", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("ACTIVES_NYSE", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("ACTIVES_NASDAQ", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("ACTIVES_OPTIONS", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("TimeAndSales", (Action<string, string>)((topic, message) => { Receive(topic, message); }));
            hubConnection.On("GaugeScore", (Action<string, string>)((topic, message) => { Receive(topic, message); }));


            /// Start a Connection to the hub
            /// 
            await hubConnection.StartAsync();


            var color = IsConnected ? "green" : "red";
            TDAStreamerData.hubStatus = $"./images/{color}.gif";
            StateHasChanged();

        }
        private void Receive(string user, string message)
        {
            LogTopic(user, message);
            StateHasChanged();
        }

        /// <summary>
        /// Method for this client to Send a message to the hub
        /// </summary>
        /// <returns></returns>
        /// 
        Task Send(string userInput, string messageInput) =>
            hubConnection.SendAsync("SendTopic", userInput, messageInput);

        /// <summary>
        /// Method to test if hub connection is alive
        /// </summary>
        /// 
        public bool IsConnected => hubConnection != null && hubConnection.State == HubConnectionState.Connected;

        /// <summary>
        /// Method that will shut down hub if this page is closed
        /// </summary>
        /// 
        public void Dispose()
        {
            _ = hubConnection.DisposeAsync();
        }

        #endregion
    }
}
