using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tdaStreamHub.Data
{

    /// <summary>
    /// WebsocketService
    /// 
    /// 1. Create new instance
    ///         wsClient = new WebsocketService(WebsocketUrl);
    /// 
    /// 2. Listen for WebsocketService events
    ///         WebsocketService.OnMessage += MessageReceived;      // Message text returned to this event
    ///         WebsocketService.OnError += ErrorMessageReceived;   
    ///         WebsocketService.OnFirstSend += SendCompleted;
    /// 
    /// 2. Call Connecting
    ///         await wsClient.Connecting();                        // NOTE: Always create new instance each time before connecting
    /// 
    /// 3. Call Sending 
    ///         await wsClient.Sending(dataToSend);
    /// 
    /// 5. Call Stopping
    ///         await wsClient.Stopping();
    ///         
    /// </summary>
    public class WebsocketService : BackgroundService
    {
        public ClientWebSocket socket = new ClientWebSocket();

        //Events
        static public event Action<string> OnMessage; private void SendMessageToClient(string txt) => OnMessage?.Invoke(txt);
        static public event Action<Exception> OnError; private void SendErrorMessageToClient(Exception ex) => OnError?.Invoke(ex);
        /// <summary>
        /// Fires on FIRST Sending call --
        /// Because we call the async Receive (which waits) after the first Send, 
        /// the first call to the Send method does not return.
        /// 
        /// This event allows the caller to do any processing needed after the first send, a pseudo returm.
        /// </summary>
        static public event Action OnFirstSend; private void ReturnAfterFirstSend() => OnFirstSend?.Invoke();

        // Private Stores
        CancellationToken stoppingToken;
        static string Connection = TDAParameters.webSocketUrl;
        bool IsReading = false;

        /// Constructor
        public WebsocketService(string webSocketUrl)
        {
            Connection = webSocketUrl;
        }

        /// User-called methods
        public async Task Connecting()
        {
            stoppingToken = new System.Threading.CancellationToken();
            await ExecuteAsync(stoppingToken);
        }
        public async Task Stopping()
        {
            await StopAsync(System.Threading.CancellationToken.None);
        }
        public async Task Sending(string textToSend)
        {
            if (socket.State == WebSocketState.Open)
            {
                await Send(socket, textToSend, stoppingToken);
                if (!IsReading)
                {
                    //ReturnAfterFirstSend();
                    await Receive(socket, stoppingToken);
                }
            }
        }

        /// Object methods
        protected override async Task ExecuteAsync(CancellationToken stopToken)
        {
            stoppingToken = stopToken;
            try
            {
                await socket.ConnectAsync(new Uri(Connection), stoppingToken);

            }
            catch (Exception ex)
            {
                SendErrorMessageToClient(ex);
            }
        }
        public override async Task StopAsync(CancellationToken stopToken)
        {
            try
            {
                if (socket.State != WebSocketState.Closed)
                {
                    stoppingToken = new System.Threading.CancellationToken(true);
                    var status = new WebSocketCloseStatus();
                    status = WebSocketCloseStatus.NormalClosure;
                    await socket.CloseAsync(status, socket.CloseStatusDescription, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                SendErrorMessageToClient(ex);
            }
        }
        private async Task Send(ClientWebSocket socket, string data, CancellationToken stoppingToken) =>
            await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, stoppingToken);
        private async Task Receive(ClientWebSocket socket, CancellationToken stoppingToken)
        {
            try
            {
                IsReading = true;
            KeepReading:
                var buffer = new ArraySegment<byte>(new byte[2048]);
                while (!stoppingToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            result = await socket.ReceiveAsync(buffer, stoppingToken);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // if the token is cancelled while ReceiveAsync is blocking, the socket state changes to aborted and it can't be used
                            if (!stoppingToken.IsCancellationRequested && result.MessageType == WebSocketMessageType.Close)
                            {
                                // the client is notifying us that the connection will close; send acknowledgement
                                if (socket.State == WebSocketState.CloseReceived && result.MessageType == WebSocketMessageType.Close)
                                {
                                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                                    // the socket state changes to closed at this point
                                }
                            }
                            SendErrorMessageToClient(new Exception(result.CloseStatus.ToString()));
                            break;
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            var dataReceived = await reader.ReadToEndAsync(); // Here's where received data lands, how to get it back? 
                            SendMessageToClient(dataReceived);
                        }
                    }

                };

                //var receiveResult = await client.Socket.ReceiveAsync(buffer, loopToken);


                if (socket.State == WebSocketState.Closed)
                {
                    //await Stopping();
                    //stoppingToken = new CancellationToken(false);
                    socket = new ClientWebSocket();
                    await socket.ConnectAsync(new Uri(Connection), stoppingToken);
                }
                goto KeepReading;
            }
            catch (Exception ex)
            {
                SendErrorMessageToClient(ex);
            }
        }

    }
}
