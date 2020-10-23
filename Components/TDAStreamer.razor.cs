using BlazorTrader.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTrader.Components
{
    public partial class TDAStreamer
    {

        [Inject]
        IJSRuntime TDAStreamerJs { get; set; }

        [Parameter]
        public string symbol { get; set; }

        //public ObservableCollection<SparkData> DataSource { get; set; }

        string clock = DateTime.Now.ToString("HH:mm:ss.fff");
        string serviceRequestText = "";
        string serviceSelection = "Service Request";
        string logText = "";

        static string status = "None";
        string statusClass = "badge-primary";

        StringBuilder logTextsb = new StringBuilder();  /// Called from Javascript
        string feedFile, chartFile, quoteFile = "";

        double strike = 0;

        IEnumerable<int> values = new int[] { 1 ,2,4,5};
        readonly string[] valuesName = new string[] {"ALL" ,"NASDAQ_BOOK" ,"TIMESALE_EQUITY" ,"CHART_EQUITY" , "OPTION" ,"QUOTE" };


        DateTime optionExpDate = DateTime.Now.AddDays(1);

        void Change(IEnumerable<int> value, string name)
        {
            //var str = string.Join(", ", value);
            //events.Add(DateTime.Now, $"{name} value changed to {str}");
        }

        // Page Event Handlers
        protected override async Task OnInitializedAsync()
        {

            /// Connect to the web socket, passing it a ref to this page, so it can call methods from javascript
            var dotNetReference = DotNetObjectReference.Create(this);
            var dud = await TDAStreamerJs.InvokeAsync<string>("Initialize", dotNetReference);
            var dud2 = await TDAStreamerJs.InvokeAsync<string>("Connect");
            feedFile = FilesManager.GetFileNameForToday("FEED");
            quoteFile = FilesManager.GetFileNameForToday(@$"{symbol} QUOTES");
            chartFile = FilesManager.GetFileNameForToday(@$"{symbol} CANDLES");
            await TDAApiService.GetAuthenticationAsync();


            await Task.CompletedTask;
            /// Get the current QQQ price so can determine options to track


            List<DayOfWeek> optionDaysOfWeek = new List<DayOfWeek>(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });
            while (!optionDaysOfWeek.Contains(optionExpDate.DayOfWeek))
            {
                optionExpDate = optionExpDate.AddDays(1);
            }
            TDAStreamerData.expiryDate = optionExpDate;

    
        }

        protected async void Login()
        {
            string request = TDAStreamerData.getLoginRequest();

            var dud = await TDAStreamerJs.InvokeAsync<string>("tdaSendRequest", request);

            StateHasChanged();
        }

        protected async Task Send()
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

        protected void serviceRequestChanged(RadzenSplitButtonItem item)
        {
            if (item == null) return;

            serviceSelection = item.Value;
            TDAStreamerData.chain = "102120C337,102120P337";
            serviceRequestText = TDAStreamerData.getServiceRequestOld(serviceSelection);
            //LogText(serviceRequestText);
        }

        protected async Task startFeed()
        { /// get json from the FEED file and call the same procs as if from javascript

          /// loop thru the file and pause one sec between reads, unless it's not data
            int counter = 0;
            string line;
            TDAStreamerData.isRealTime = false;
            // Read the file and display it line by line.
            System.IO.StreamReader file =
                new System.IO.StreamReader(feedFile.Replace(".json", "copy.json"));
            while ((line = file.ReadLine()) != null)
            {
                if (line.Contains("data"))
                {
                    /// Send the line to js and have it send it back to TDAStreamerOnMessage(line);
                    var dud = await TDAStreamerJs.InvokeAsync<string>("Echo", line);

                    System.Threading.Thread.Sleep(750);
                }
                counter++;
            }


        }
        /// Utility
        void LogText(string text)
        {
            logTextsb.Insert(0, "\n" + text);
            logText = string.Join('\n', logTextsb.ToString().Split('\n').Take(100));

            StateHasChanged();
        }



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

        [JSInvokable("TDAStreamerOnMessage")]
        public void TDAStreamerOnMessage(string jsonResponse)
        {


            LogText("RECEIVED: " + jsonResponse);
            var fieldedResponse = jsonResponse;
            if (jsonResponse.Contains("\"data\":"))
            {
                var dataJsonSvcArray = JObject.Parse(jsonResponse)["data"];
                foreach (var svcJsonObject in dataJsonSvcArray)
                {
                    var svcName = svcJsonObject["service"].ToString();
                    var svcEpochTime = Convert.ToInt64(svcJsonObject["timestamp"]);
                    var svcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(svcEpochTime).ToLocalTime();

                    clock = svcDateTime.ToString("HH:mm:ss.fff");

                    var svcJson = svcJsonObject.ToString();
                    var svcFieldedJson = svcJson;
                    List<string> svcFields = TDAConstants.TDAResponseFields[svcName];
                    for (int i = 1; i < svcFields.Count; i++)
                    {
                        string sIndex = $"\"{i}\":";
                        svcFieldedJson = svcFieldedJson.Replace(sIndex, $" \"{svcFields[i]}\":");
                    }
                    LogText("DECODED: " + svcFieldedJson);
                    TDAStreamerData.captureTdaServiceData(svcFieldedJson).Wait();

                }
            }
            else if (jsonResponse.Contains("\"notify\":"))
            {
                var it = JObject.Parse(jsonResponse)["notify"][0]["heartbeat"];
                if (it != null)
                {
                    var timeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(Convert.ToDouble(((Newtonsoft.Json.Linq.JValue)it).Value)).ToLocalTime();
                    LogText("DECODED: " + jsonResponse + " " + timeStamp.TimeOfDay);
                }

            }
            if (TDAStreamerData.isRealTime && feedFile != null && jsonResponse != null)
                System.IO.File.AppendAllText(feedFile, jsonResponse.Replace("\r\n", "") + "\n");

            //lstJson.Add(jsonResponse);
            //if (lstJson.Count == 10)
            //{
            //    System.IO.File.AppendAllText(feedFile, string.Join('\n',lstJson));
            //    lstJson.Clear();
            //}

            StateHasChanged();
        }

        //public string symbol { get; set; }

    }
}
