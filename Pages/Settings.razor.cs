using BlazorTrader.Data;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorTrader.Pages
{
    public partial class Settings
    {
        [Inject]
        public TDAOptionsTableManager mgr { get; set; }
        /// Page Local Variablee
        /// For the Data Tab
        List<TDAOptionQuote> lstCallOptions = new List<TDAOptionQuote>();

        /// Page Local Variablee
        string dataToSend, socketLog, errorMessage = "", dataReceived;
        WebsocketService wsClient = new WebsocketService(TDAParameters.webSocketUrl);

        DateTime optionExpDate = DateTime.Now.AddDays(1);

        void OnThemeColorChanged(string value)
        {
            if (Theme?.ColorOptions != null)
                Theme.ColorOptions.Primary = value;

            if (Theme?.BackgroundOptions != null)
                Theme.BackgroundOptions.Primary = value;

            Theme.ThemeHasChanged();
        }

        [CascadingParameter] Blazorise.Theme Theme { get; set; }

        /// Page Init
        protected override async Task OnInitializedAsync()
        {
            //if (mgr.lstOptions != null)
            //    foreach (TDAOptionQuote[] qtArray in mgr.lstOptions)
            //    {
            //        var qt = qtArray[0];
            //        lstCallOptions.Add(qt);
            //    }

            StateHasChanged();

            /// Listen for WebSocket Events
            WebsocketService.OnMessage += MessageReceived;
            WebsocketService.OnError += ErrorMessageReceived;
            WebsocketService.OnFirstSend += SendCompleted;

            /// To make this method become async
            await Task.CompletedTask;
            TDAStreamerData.OnStatusesChanged += getQuoteData;




            List<DayOfWeek> optionDaysOfWeek = new List<DayOfWeek>(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });
            while (!optionDaysOfWeek.Contains(optionExpDate.DayOfWeek))
            {
                optionExpDate = optionExpDate.AddDays(1);
            }

        }

        private void getQuoteData()
        {
            StateHasChanged();
        }

        #region Page Event Handlers
        protected async void Connect()
        {
            wsClient = new WebsocketService(TDAParameters.webSocketUrl);
            await wsClient.Connecting();

            errorMessage = "";
            StateHasChanged();
        }
        protected async Task Send()
        {
            await wsClient.Sending(dataToSend);
            dataToSend = "";
            //SendCompleted();
        }
        protected void Ping()
        {
            StateHasChanged();
        }
        protected async Task Disconnect()
        {
            await wsClient.Stopping();
            StateHasChanged();
        }

        protected void socketServerChanged(RadzenSplitButtonItem item)
        {
            TDAParameters.webSocketUrl = item.Value;
            TDAParameters.webSocketName = item.Text;
        }
        #endregion

        #region Websocket Event Handlers
        void MessageReceived(string responseText)
        {
            dataReceived = responseText;
            logReceivedText(responseText);
            StateHasChanged();
        }
        void ErrorMessageReceived(Exception ex)
        {
            errorMessage = ex.ToString();
            StateHasChanged();
        }
        void SendCompleted()
        {
            //dataToSend = "";
            //try { StateHasChanged(); } catch { }
            //await Task.CompletedTask;

        }
        #endregion


        #region Utilities
        void logSentText(string text)
        {
            socketLog += "Sent:" + text + "\t";
            StateHasChanged();
        }
        public void logReceivedText(string text)
        {
            socketLog += "Received:" + text + "\n";
            StateHasChanged();
        }
        #endregion 
    }
}
