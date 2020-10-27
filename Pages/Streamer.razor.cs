using tdaStreamHub.Data;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace tdaStreamHub.Pages
{
    public partial class Streamer
    {

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        string dataToSend, socketLog, errorMessage = "", dataReceived;

        WebsocketService wsClient = new WebsocketService(TDAParameters.webSocketUrl);

        DateTime optionExpDate = DateTime.Now.AddDays(1);

        protected override async Task OnInitializedAsync()
        {

            /// Listen for WebSocket Events
            WebsocketService.OnMessage += MessageReceived;
            WebsocketService.OnError += ErrorMessageReceived;
            WebsocketService.OnFirstSend += SendCompleted;

            /// To make this method become async
            await Task.CompletedTask;

            List<DayOfWeek> optionDaysOfWeek = new List<DayOfWeek>(new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });
            while (!optionDaysOfWeek.Contains(optionExpDate.DayOfWeek))
            {
                optionExpDate = optionExpDate.AddDays(1);
            }

            StateHasChanged();

        }

        #region WebSocket Event Handlers
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
        #endregion

        #region Page Event Handlers
        protected void socketServerChanged(RadzenSplitButtonItem item)
        {
            TDAParameters.webSocketUrl = item.Value;
            TDAParameters.webSocketName = item.Text;
        }
        #endregion

        #region Websocket Response Handlers
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
