﻿//------------------------------------------------------------------------------
// <copyright file="WebSocketBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
 
namespace System.Net.WebSockets
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
 
    internal abstract class WebSocketBase : WebSocket, IDisposable
    {
        private static volatile bool s_LoggingEnabled;
 
#if DEBUG
        private volatile string m_CloseStack;
#endif
        private readonly OutstandingOperationHelper m_CloseOutstandingOperationHelper;
        private readonly OutstandingOperationHelper m_CloseOutputOutstandingOperationHelper;
        private readonly OutstandingOperationHelper m_ReceiveOutstandingOperationHelper;
        private readonly OutstandingOperationHelper m_SendOutstandingOperationHelper;
        private readonly Stream m_InnerStream;
        private readonly IWebSocketStream m_InnerStreamAsWebSocketStream;
        private readonly string m_SubProtocol;
 
        // We are not calling Dispose method on this object in Cleanup method to avoid a race condition while one thread is calling disposing on 
        // this object and another one is still using WaitAsync. According to Dev11 358715, this should be fine as long as we are not accessing the
        // AvailableWaitHandle on this SemaphoreSlim object.
        private readonly SemaphoreSlim m_SendFrameThrottle;
        // locking m_ThisLock protects access to
        // - State
        // - m_CloseStack
        // - m_CloseAsyncStartedReceive
        // - m_CloseReceivedTaskCompletionSource
        // - m_CloseNetworkConnectionTask
        private readonly object m_ThisLock;
        private readonly WebSocketBuffer m_InternalBuffer;
        private readonly KeepAliveTracker m_KeepAliveTracker;
        private volatile bool m_CleanedUp;
        private volatile TaskCompletionSource<object> m_CloseReceivedTaskCompletionSource;
        private volatile Task m_CloseOutputTask;
        private volatile bool m_IsDisposed;
        private volatile Task m_CloseNetworkConnectionTask;
        private volatile bool m_CloseAsyncStartedReceive;
        private volatile WebSocketState m_State;
        private volatile Task m_KeepAliveTask;
        private volatile WebSocketOperation.ReceiveOperation m_ReceiveOperation;
        private volatile WebSocketOperation.SendOperation m_SendOperation;
        private volatile WebSocketOperation.SendOperation m_KeepAliveOperation;
        private volatile WebSocketOperation.CloseOutputOperation m_CloseOutputOperation;
        private Nullable<WebSocketCloseStatus> m_CloseStatus;
        private string m_CloseStatusDescription;
        private int m_ReceiveState;
        private Exception m_PendingException;
 
        protected WebSocketBase(Stream innerStream,
            string subProtocol,
            TimeSpan keepAliveInterval,
            WebSocketBuffer internalBuffer)
        {
            Contract.Assert(internalBuffer != null, "'internalBuffer' MUST NOT be NULL.");
            WebSocketHelpers.ValidateInnerStream(innerStream);
            WebSocketHelpers.ValidateOptions(subProtocol, internalBuffer.ReceiveBufferSize,
                internalBuffer.SendBufferSize, keepAliveInterval);
 
            s_LoggingEnabled = Logging.On && Logging.WebSockets.Switch.ShouldTrace(TraceEventType.Critical);
            string parameters = string.Empty;
 
            if (s_LoggingEnabled)
            {
                parameters = string.Format(CultureInfo.InvariantCulture,
                    "ReceiveBufferSize: {0}, SendBufferSize: {1},  Protocols: {2}, KeepAliveInterval: {3}, innerStream: {4}, internalBuffer: {5}",
                    internalBuffer.ReceiveBufferSize,
                    internalBuffer.SendBufferSize,
                    subProtocol,
                    keepAliveInterval,
                    Logging.GetObjectLogHash(innerStream),
                    Logging.GetObjectLogHash(internalBuffer));
 
                Logging.Enter(Logging.WebSockets, this, Methods.Initialize, parameters);
            }
 
            m_ThisLock = new object();
 
            try
            {
                m_InnerStream = innerStream;
                m_InternalBuffer = internalBuffer;
                if (s_LoggingEnabled)
                {
                    Logging.Associate(Logging.WebSockets, this, m_InnerStream);
                    Logging.Associate(Logging.WebSockets, this, m_InternalBuffer);
                }
 
                m_CloseOutstandingOperationHelper = new OutstandingOperationHelper();
                m_CloseOutputOutstandingOperationHelper = new OutstandingOperationHelper();
                m_ReceiveOutstandingOperationHelper = new OutstandingOperationHelper();
                m_SendOutstandingOperationHelper = new OutstandingOperationHelper();
                m_State = WebSocketState.Open;
                m_SubProtocol = subProtocol;
                m_SendFrameThrottle = new SemaphoreSlim(1, 1);
                m_CloseStatus = null;
                m_CloseStatusDescription = null;
                m_InnerStreamAsWebSocketStream = innerStream as IWebSocketStream;
                if (m_InnerStreamAsWebSocketStream != null)
                {
                    m_InnerStreamAsWebSocketStream.SwitchToOpaqueMode(this);
                }
                m_KeepAliveTracker = KeepAliveTracker.Create(keepAliveInterval);
            }
            finally
            {
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.Initialize, parameters);
                }
            }
        }
 
        internal static bool LoggingEnabled
        {
            get
            {
                return s_LoggingEnabled;
            }
        }
 
        public override WebSocketState State
        {
            get
            {
                Contract.Assert(m_State != WebSocketState.None, "'m_State' MUST NOT be 'WebSocketState.None'.");
                return m_State;
            }
        }
 
        public override string SubProtocol
        {
            get
            {
                return m_SubProtocol;
            }
        }
 
        public override Nullable<WebSocketCloseStatus> CloseStatus
        {
            get
            {
                return m_CloseStatus;
            }
        }
 
        public override string CloseStatusDescription
        {
            get
            {
                return m_CloseStatusDescription;
            }
        }
 
        internal WebSocketBuffer InternalBuffer
        {
            get
            {
                Contract.Assert(m_InternalBuffer != null, "'m_InternalBuffer' MUST NOT be NULL.");
                return m_InternalBuffer;
            }
        }
 
        protected void StartKeepAliveTimer()
        {
            m_KeepAliveTracker.StartTimer(this);
        }
 
        // locking SessionHandle protects access to
        // - WSPC (WebSocketProtocolComponent)
        // - m_KeepAliveTask
        // - m_CloseOutputTask
        // - m_LastSendActivity
        internal abstract SafeHandle SessionHandle { get; }
 
        // MultiThreading: ThreadSafe; At most one outstanding call to ReceiveAsync is allowed
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            WebSocketHelpers.ValidateArraySegment<byte>(buffer, "buffer");
            return ReceiveAsyncCore(buffer, cancellationToken);
        }
 
        private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            Contract.Assert(buffer != null);
 
            if (s_LoggingEnabled)
            {
                Logging.Enter(Logging.WebSockets, this, Methods.ReceiveAsync, string.Empty);
            }
 
            WebSocketReceiveResult receiveResult;
            try
            {
                ThrowIfPendingException();
                ThrowIfDisposed();
                ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseSent);
 
                bool ownsCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
                try
                {
                    ownsCancellationTokenSource = m_ReceiveOutstandingOperationHelper.TryStartOperation(cancellationToken,
                        out linkedCancellationToken);
                    if (!ownsCancellationTokenSource)
                    {
                        lock (m_ThisLock)
                        {
                            if (m_CloseAsyncStartedReceive)
                            {
                                throw new InvalidOperationException(
                                    SR.GetString(SR.net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync, Methods.CloseAsync, Methods.CloseOutputAsync));
                            }
 
                            throw new InvalidOperationException(
                                SR.GetString(SR.net_Websockets_AlreadyOneOutstandingOperation, Methods.ReceiveAsync));
                        }
                    }
 
                    EnsureReceiveOperation();
                    receiveResult = await m_ReceiveOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
 
                    if (s_LoggingEnabled && receiveResult.Count > 0)
                    {
                        Logging.Dump(Logging.WebSockets,
                            this,
                            Methods.ReceiveAsync,
                            buffer.Array,
                            buffer.Offset,
                            receiveResult.Count);
                    }
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.ReceiveAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    m_ReceiveOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                }
            }
            finally
            {
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.ReceiveAsync, string.Empty);
                }
            }
 
            return receiveResult;
        }
 
        // MultiThreading: ThreadSafe; At most one outstanding call to SendAsync is allowed
        public override Task SendAsync(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            if (messageType != WebSocketMessageType.Binary &&
                    messageType != WebSocketMessageType.Text)
            {
                throw new ArgumentException(SR.GetString(SR.net_WebSockets_Argument_InvalidMessageType,
                    messageType,
                    Methods.SendAsync,
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text,
                    Methods.CloseOutputAsync),
                    "messageType");
            }
 
            WebSocketHelpers.ValidateArraySegment<byte>(buffer, "buffer");
 
            return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
        }
 
        private async Task SendAsyncCore(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            Contract.Assert(messageType == WebSocketMessageType.Binary || messageType == WebSocketMessageType.Text,
                "'messageType' MUST be either 'WebSocketMessageType.Binary' or 'WebSocketMessageType.Text'.");
            Contract.Assert(buffer != null);
 
            string inputParameter = string.Empty;
            if (s_LoggingEnabled)
            {
                inputParameter = string.Format(CultureInfo.InvariantCulture,
                    "messageType: {0}, endOfMessage: {1}",
                    messageType,
                    endOfMessage);
                Logging.Enter(Logging.WebSockets, this, Methods.SendAsync, inputParameter);
            }
 
            try
            {
                ThrowIfPendingException();
                ThrowIfDisposed();
                ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
                bool ownsCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
 
                try
                {
                    while (!(ownsCancellationTokenSource = m_SendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken)))
                    {
                        Task keepAliveTask;
 
                        lock (SessionHandle)
                        {
                            keepAliveTask = m_KeepAliveTask;
 
                            if (keepAliveTask == null)
                            {
                                // Check whether there is still another outstanding send operation
                                // Potentially the keepAlive operation has completed before this thread
                                // was able to enter the SessionHandle-lock. 
                                m_SendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                                if (ownsCancellationTokenSource = m_SendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken))
                                {
                                    break;
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        SR.GetString(SR.net_Websockets_AlreadyOneOutstandingOperation, Methods.SendAsync));
                                }
                            }
                        }
 
                        await keepAliveTask.SuppressContextFlow();
                        ThrowIfPendingException();
 
                        m_SendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                    }
 
                    if (s_LoggingEnabled && buffer.Count > 0)
                    {
                        Logging.Dump(Logging.WebSockets,
                            this,
                            Methods.SendAsync,
                            buffer.Array,
                            buffer.Offset,
                            buffer.Count);
                    }
 
                    int position = buffer.Offset;
 
                    EnsureSendOperation();
                    m_SendOperation.BufferType = GetBufferType(messageType, endOfMessage);
                    await m_SendOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.SendAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    m_SendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                }
            }
            finally
            {
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.SendAsync, inputParameter);
                }
            }
        }
 
        private async Task SendFrameAsync(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
        {
            bool sendFrameLockTaken = false;
            try
            {
                await m_SendFrameThrottle.WaitAsync(cancellationToken).SuppressContextFlow();
                sendFrameLockTaken = true;
 
                if (sendBuffers.Count > 1 &&
                    m_InnerStreamAsWebSocketStream != null &&
                    m_InnerStreamAsWebSocketStream.SupportsMultipleWrite)
                {
                    await m_InnerStreamAsWebSocketStream.MultipleWriteAsync(sendBuffers,
                        cancellationToken).SuppressContextFlow();
                }
                else
                {
                    foreach (ArraySegment<byte> buffer in sendBuffers)
                    {
                        await m_InnerStream.WriteAsync(buffer.Array,
                            buffer.Offset,
                            buffer.Count,
                            cancellationToken).SuppressContextFlow();
                    }
                }
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, objectDisposedException);
            }
            catch (NotSupportedException notSupportedException)
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, notSupportedException);
            }
            finally
            {
                if (sendFrameLockTaken)
                {
                    m_SendFrameThrottle.Release();
                }
            }
        }
 
        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override void Abort()
        {
            if (s_LoggingEnabled)
            {
                Logging.Enter(Logging.WebSockets, this, Methods.Abort, string.Empty);
            }
 
            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;
            try
            {
                if (IsStateTerminal(State))
                {
                    return;
                }
 
                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                if (IsStateTerminal(State))
                {
                    return;
                }
 
                m_State = WebSocketState.Aborted;
 
#if DEBUG
                string stackTrace = new StackTrace().ToString();
                if (m_CloseStack == null)
                {
                    m_CloseStack = stackTrace;
                }
 
                if (s_LoggingEnabled)
                {
                    string message = string.Format(CultureInfo.InvariantCulture, "Stack: {0}", stackTrace);
                    Logging.PrintWarning(Logging.WebSockets, this, Methods.Abort, message);
                }
#endif
 
                // Abort any outstanding IO operations.
                if (SessionHandle != null && !SessionHandle.IsClosed && !SessionHandle.IsInvalid)
                {
                    WebSocketProtocolComponent.WebSocketAbortHandle(SessionHandle);
                }
 
                m_ReceiveOutstandingOperationHelper.CancelIO();
                m_SendOutstandingOperationHelper.CancelIO();
                m_CloseOutputOutstandingOperationHelper.CancelIO();
                m_CloseOutstandingOperationHelper.CancelIO();
                if (m_InnerStreamAsWebSocketStream != null)
                {
                    m_InnerStreamAsWebSocketStream.Abort();
                }
                CleanUp();
            }
            finally
            {
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.Abort, string.Empty);
                }
            }
        }
 
        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketHelpers.ValidateCloseStatus(closeStatus, statusDescription);
 
            return CloseOutputAsyncCore(closeStatus, statusDescription, cancellationToken);
        }
 
        private async Task CloseOutputAsyncCore(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            string inputParameter = string.Empty;
            if (s_LoggingEnabled)
            {
                inputParameter = string.Format(CultureInfo.InvariantCulture,
                    "closeStatus: {0}, statusDescription: {1}",
                    closeStatus,
                    statusDescription);
                Logging.Enter(Logging.WebSockets, this, Methods.CloseOutputAsync, inputParameter);
            }
 
            try
            {
                ThrowIfPendingException();
                if (IsStateTerminal(State))
                {
                    return;
                }
                ThrowIfDisposed();
 
                bool thisLockTaken = false;
                bool sessionHandleLockTaken = false;
                bool needToCompleteSendOperation = false;
                bool ownsCloseOutputCancellationTokenSource = false;
                bool ownsSendCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
                try
                {
                    TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    ThrowIfPendingException();
                    ThrowIfDisposed();
 
                    if (IsStateTerminal(State))
                    {
                        return;
                    }
 
                    ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
                    ownsCloseOutputCancellationTokenSource = m_CloseOutputOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                    if (!ownsCloseOutputCancellationTokenSource)
                    {
                        Task closeOutputTask = m_CloseOutputTask;
 
                        if (closeOutputTask != null)
                        {
                            ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                            await closeOutputTask.SuppressContextFlow();
                            TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                        }
                    }
                    else
                    {
                        needToCompleteSendOperation = true;
                        while (!(ownsSendCancellationTokenSource =
                            m_SendOutstandingOperationHelper.TryStartOperation(cancellationToken,
                                out linkedCancellationToken)))
                        {
                            if (m_KeepAliveTask != null)
                            {
                                Task keepAliveTask = m_KeepAliveTask;
 
                                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                                await keepAliveTask.SuppressContextFlow();
                                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
 
                                ThrowIfPendingException();
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    SR.GetString(SR.net_Websockets_AlreadyOneOutstandingOperation, Methods.SendAsync));
                            }
 
                            m_SendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
                        }
 
                        EnsureCloseOutputOperation();
                        m_CloseOutputOperation.CloseStatus = closeStatus;
                        m_CloseOutputOperation.CloseReason = statusDescription;
                        m_CloseOutputTask = m_CloseOutputOperation.Process(null, linkedCancellationToken);
 
                        ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                        await m_CloseOutputTask.SuppressContextFlow();
                        TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
 
                        if (OnCloseOutputCompleted())
                        {
                            bool callCompleteOnCloseCompleted = false;
 
                            try
                            {
                                callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                    thisLockTaken, sessionHandleLockTaken, linkedCancellationToken).SuppressContextFlow();
                            }
                            catch (Exception)
                            {
                                // If an exception is thrown we know that the locks have been released,
                                // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                                throw;
                            }
 
                            if (callCompleteOnCloseCompleted)
                            {
                                ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                                FinishOnCloseCompleted();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.CloseOutputAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    m_CloseOutputOutstandingOperationHelper.CompleteOperation(ownsCloseOutputCancellationTokenSource);
 
                    if (needToCompleteSendOperation)
                    {
                        m_SendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
                    }
 
                    m_CloseOutputTask = null;
                    ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                }
            }
            finally
            {
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.CloseOutputAsync, inputParameter);
                }
            }
        }
 
        // returns TRUE if the caller should also call StartOnCloseCompleted
        private bool OnCloseOutputCompleted()
        {
            if (IsStateTerminal(State))
            {
                return false;
            }
 
            switch (State)
            {
                case WebSocketState.Open:
                    m_State = WebSocketState.CloseSent;
                    return false;
                case WebSocketState.CloseReceived:
                    return true;
                default:
                    return false;
            }
        }
 
        // MultiThreading: This method has to be called under a m_ThisLock-lock
        // ReturnValue: This method returns true only if CompleteOnCloseCompleted needs to be called
        // If this method returns true all locks were released before starting the IO operation 
        // and they have to be retaken by the caller before calling CompleteOnCloseCompleted
        // Exception handling: If an exception is thrown from await StartOnCloseCompleted
        // it always means the locks have been released already - so the caller has to retake the
        // locks in the catch-block. 
        // This is ensured by enforcing a Task.Yield for IWebSocketStream.CloseNetowrkConnectionAsync
        private async Task<bool> StartOnCloseCompleted(bool thisLockTakenSnapshot,
            bool sessionHandleLockTakenSnapshot,
            CancellationToken cancellationToken)
        {
            Contract.Assert(thisLockTakenSnapshot, "'thisLockTakenSnapshot' MUST be 'true' at this point.");
 
            if (IsStateTerminal(m_State))
            {
                return false;
            }
 
            m_State = WebSocketState.Closed;
 
#if DEBUG
            if (m_CloseStack == null)
            {
                m_CloseStack = new StackTrace().ToString();
            }
#endif
 
            if (m_InnerStreamAsWebSocketStream != null)
            {
                bool thisLockTaken = thisLockTakenSnapshot;
                bool sessionHandleLockTaken = sessionHandleLockTakenSnapshot;
 
                try
                {
                    if (m_CloseNetworkConnectionTask == null)
                    {
                        m_CloseNetworkConnectionTask =
                            m_InnerStreamAsWebSocketStream.CloseNetworkConnectionAsync(cancellationToken);
                    }
 
                    if (thisLockTaken && sessionHandleLockTaken)
                    {
                        ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
                    }
                    else if (thisLockTaken)
                    {
                        ReleaseLock(m_ThisLock, ref thisLockTaken);
                    }
 
                    await m_CloseNetworkConnectionTask.SuppressContextFlow();
                }
                catch (Exception closeNetworkConnectionTaskException)
                {
                    if (!CanHandleExceptionDuringClose(closeNetworkConnectionTaskException))
                    {
                        ThrowIfConvertibleException(Methods.StartOnCloseCompleted,
                            closeNetworkConnectionTaskException,
                            cancellationToken,
                            cancellationToken.IsCancellationRequested);
                        throw;
                    }
                }
            }
 
            return true;
        }
 
        // MultiThreading: This method has to be called under a thisLock-lock
        private void FinishOnCloseCompleted()
        {
            CleanUp();
        }
 
        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        public override Task CloseAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            WebSocketHelpers.ValidateCloseStatus(closeStatus, statusDescription);
            return CloseAsyncCore(closeStatus, statusDescription, cancellationToken);
        }
 
        private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            string inputParameter = string.Empty;
            if (s_LoggingEnabled)
            {
                inputParameter = string.Format(CultureInfo.InvariantCulture,
                    "closeStatus: {0}, statusDescription: {1}",
                    closeStatus,
                    statusDescription);
                Logging.Enter(Logging.WebSockets, this, Methods.CloseAsync, inputParameter);
            }
 
            try
            {
                ThrowIfPendingException();
                if (IsStateTerminal(State))
                {
                    return;
                }
                ThrowIfDisposed();
 
                bool lockTaken = false;
                Monitor.Enter(m_ThisLock, ref lockTaken);
                bool ownsCloseCancellationTokenSource = false;
                CancellationToken linkedCancellationToken = CancellationToken.None;
                try
                {
                    ThrowIfPendingException();
                    if (IsStateTerminal(State))
                    {
                        return;
                    }
                    ThrowIfDisposed();
                    ThrowOnInvalidState(State,
                        WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent);
 
                    Task closeOutputTask;
                    ownsCloseCancellationTokenSource = m_CloseOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                    if (ownsCloseCancellationTokenSource)
                    {
                        closeOutputTask = m_CloseOutputTask;
                        if (closeOutputTask == null && State != WebSocketState.CloseSent)
                        {
                            if (m_CloseReceivedTaskCompletionSource == null)
                            {
                                m_CloseReceivedTaskCompletionSource = new TaskCompletionSource<object>();
                            }
                            ReleaseLock(m_ThisLock, ref lockTaken);
                            closeOutputTask = CloseOutputAsync(closeStatus,
                                statusDescription,
                                linkedCancellationToken);
                        }
                    }
                    else
                    {
                        Contract.Assert(m_CloseReceivedTaskCompletionSource != null,
                            "'m_CloseReceivedTaskCompletionSource' MUST NOT be NULL.");
                        closeOutputTask = m_CloseReceivedTaskCompletionSource.Task;
                    }
 
                    if (closeOutputTask != null)
                    {
                        ReleaseLock(m_ThisLock, ref lockTaken);
                        try
                        {
                            await closeOutputTask.SuppressContextFlow();
                        }
                        catch (Exception closeOutputError)
                        {
                            Monitor.Enter(m_ThisLock, ref lockTaken);
 
                            if (!CanHandleExceptionDuringClose(closeOutputError))
                            {
                                ThrowIfConvertibleException(Methods.CloseOutputAsync,
                                    closeOutputError,
                                    cancellationToken,
                                    linkedCancellationToken.IsCancellationRequested);
                                throw;
                            }
                        }
 
                        // When closeOutputTask != null  and an exception thrown from await closeOutputTask is handled, 
                        // the lock will be taken in the catch-block. So the logic here avoids taking the lock twice. 
                        if (!lockTaken)
                        {
                            Monitor.Enter(m_ThisLock, ref lockTaken);
                        }
                    }
 
                    if (OnCloseOutputCompleted())
                    {
                        bool callCompleteOnCloseCompleted = false;
                        
                        try
                        {
                            // linkedCancellationToken can be CancellationToken.None if ownsCloseCancellationTokenSource==false
                            // This is still ok because OnCloseOutputCompleted won't start any IO operation in this case
                            callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                lockTaken, false, linkedCancellationToken).SuppressContextFlow();
                        }
                        catch (Exception)
                        {
                            // If an exception is thrown we know that the locks have been released,
                            // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                            ResetFlagAndTakeLock(m_ThisLock, ref lockTaken);
                            throw;
                        }
 
                        if (callCompleteOnCloseCompleted)
                        {
                            ResetFlagAndTakeLock(m_ThisLock, ref lockTaken);
                            FinishOnCloseCompleted();
                        }
                    }
 
                    if (IsStateTerminal(State))
                    {
                        return;
                    }
 
                    linkedCancellationToken = CancellationToken.None;
 
                    bool ownsReceiveCancellationTokenSource = m_ReceiveOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                    if (ownsReceiveCancellationTokenSource)
                    {
                        m_CloseAsyncStartedReceive = true;
                        ArraySegment<byte> closeMessageBuffer =
                            new ArraySegment<byte>(new byte[WebSocketBuffer.MinReceiveBufferSize]);
                        EnsureReceiveOperation();
                        Task<WebSocketReceiveResult> receiveAsyncTask = m_ReceiveOperation.Process(closeMessageBuffer,
                            linkedCancellationToken);
                        ReleaseLock(m_ThisLock, ref lockTaken);
 
                        WebSocketReceiveResult receiveResult = null;
                        try
                        {
                            receiveResult = await receiveAsyncTask.SuppressContextFlow();
                        }
                        catch (Exception receiveException)
                        {
                            Monitor.Enter(m_ThisLock, ref lockTaken);
 
                            if (!CanHandleExceptionDuringClose(receiveException))
                            {
                                ThrowIfConvertibleException(Methods.CloseAsync,
                                    receiveException,
                                    cancellationToken,
                                    linkedCancellationToken.IsCancellationRequested);
                                throw;
                            }
                        }
 
                        // receiveResult is NEVER NULL if WebSocketBase.ReceiveOperation.Process completes successfully 
                        // - but in the close code path we handle some exception if another thread was able to tranistion 
                        // the state into Closed successfully. In this case receiveResult can be NULL and it is safe to 
                        // skip the statements in the if-block.
                        if (receiveResult != null)
                        {
                            if (s_LoggingEnabled && receiveResult.Count > 0)
                            {
                                Logging.Dump(Logging.WebSockets,
                                    this,
                                    Methods.ReceiveAsync,
                                    closeMessageBuffer.Array,
                                    closeMessageBuffer.Offset,
                                    receiveResult.Count);
                            }
 
                            if (receiveResult.MessageType != WebSocketMessageType.Close)
                            {
                                throw new WebSocketException(WebSocketError.InvalidMessageType,
                                    SR.GetString(SR.net_WebSockets_InvalidMessageType,
                                        typeof(WebSocket).Name + "." + Methods.CloseAsync,
                                        typeof(WebSocket).Name + "." + Methods.CloseOutputAsync,
                                        receiveResult.MessageType));
                            }
                        }
                    }
                    else
                    {
                        m_ReceiveOutstandingOperationHelper.CompleteOperation(ownsReceiveCancellationTokenSource);
                        ReleaseLock(m_ThisLock, ref lockTaken);
                        await m_CloseReceivedTaskCompletionSource.Task.SuppressContextFlow();
                    }
 
                    // When ownsReceiveCancellationTokenSource is true and an exception is thrown, the lock will be taken.
                    // So this logic here is to avoid taking the lock twice. 
                    if (!lockTaken)
                    {
                        Monitor.Enter(m_ThisLock, ref lockTaken);
                    }
 
                    if (!IsStateTerminal(State))
                    {
                        bool ownsSendCancellationSource = false;
                        try
                        {
                            // We know that the CloseFrame has been sent at this point. So no Send-operation is allowed anymore and we
                            // can hijack the m_SendOutstandingOperationHelper to create a linkedCancellationToken
                            ownsSendCancellationSource = m_SendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
                            Contract.Assert(ownsSendCancellationSource, "'ownsSendCancellationSource' MUST be 'true' at this point.");
 
                            bool callCompleteOnCloseCompleted = false;
 
                            try
                            {
                                // linkedCancellationToken can be CancellationToken.None if ownsCloseCancellationTokenSource==false
                                // This is still ok because OnCloseOutputCompleted won't start any IO operation in this case
                                callCompleteOnCloseCompleted = await StartOnCloseCompleted(
                                    lockTaken, false, linkedCancellationToken).SuppressContextFlow();
                            }
                            catch (Exception)
                            {
                                // If an exception is thrown we know that the locks have been released,
                                // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                ResetFlagAndTakeLock(m_ThisLock, ref lockTaken);
                                throw;
                            }
 
                            if (callCompleteOnCloseCompleted)
                            {
                                ResetFlagAndTakeLock(m_ThisLock, ref lockTaken);
                                FinishOnCloseCompleted();
                            }
                        }
                        finally
                        {
                            m_SendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationSource);
                        }
                    }
                }
                catch (Exception exception)
                {
                    bool aborted = linkedCancellationToken.IsCancellationRequested;
                    Abort();
                    ThrowIfConvertibleException(Methods.CloseAsync, exception, cancellationToken, aborted);
                    throw;
                }
                finally
                {
                    m_CloseOutstandingOperationHelper.CompleteOperation(ownsCloseCancellationTokenSource);
                    ReleaseLock(m_ThisLock, ref lockTaken);
                }
            }
            finally
            {
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, this, Methods.CloseAsync, inputParameter);
                }
            }
        }
 
        // MultiThreading: ThreadSafe; No-op if already in a terminal state
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "m_SendFrameThrottle",
            Justification = "SemaphoreSlim.Dispose is not threadsafe and can cause NullRef exceptions on other threads." +
            "Also according to the CLR Dev11#358715) there is no need to dispose SemaphoreSlim if the ManualResetEvent " +
            "is not used.")]
        public override void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }
 
            bool thisLockTaken = false;
            bool sessionHandleLockTaken = false;
 
            try
            {
                TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
 
                if (m_IsDisposed)
                {
                    return;
                }
 
                if (!IsStateTerminal(State))
                {
                    Abort();
                }
                else
                {
                    CleanUp();
                }
 
                m_IsDisposed = true;
            }
            finally
            {
                ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
            }
        }
 
        private void ResetFlagAndTakeLock(object lockObject, ref bool thisLockTaken)
        {
            Contract.Assert(lockObject != null, "'lockObject' MUST NOT be NULL.");
            thisLockTaken = false;
            Monitor.Enter(lockObject, ref thisLockTaken);
        }
 
        private void ResetFlagsAndTakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            thisLockTaken = false;
            sessionHandleLockTaken = false;
            TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
        }
 
        private void TakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            Contract.Assert(m_ThisLock != null, "'m_ThisLock' MUST NOT be NULL.");
            Contract.Assert(SessionHandle != null, "'SessionHandle' MUST NOT be NULL.");
 
            Monitor.Enter(SessionHandle, ref sessionHandleLockTaken);
            Monitor.Enter(m_ThisLock, ref thisLockTaken);
        }
 
        private void ReleaseLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
        {
            Contract.Assert(m_ThisLock != null, "'m_ThisLock' MUST NOT be NULL.");
            Contract.Assert(SessionHandle != null, "'SessionHandle' MUST NOT be NULL.");
 
            if (thisLockTaken || sessionHandleLockTaken)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    if (thisLockTaken)
                    {
                        Monitor.Exit(m_ThisLock);
                        thisLockTaken = false;
                    }
 
                    if (sessionHandleLockTaken)
                    {
                        Monitor.Exit(SessionHandle);
                        sessionHandleLockTaken = false;
                    }
                }
            }
        }
 
        private void EnsureReceiveOperation()
        {
            if (m_ReceiveOperation == null)
            {
                lock (m_ThisLock)
                {
                    if (m_ReceiveOperation == null)
                    {
                        m_ReceiveOperation = new WebSocketOperation.ReceiveOperation(this);
                    }
                }
            }
        }
 
        private void EnsureSendOperation()
        {
            if (m_SendOperation == null)
            {
                lock (m_ThisLock)
                {
                    if (m_SendOperation == null)
                    {
                        m_SendOperation = new WebSocketOperation.SendOperation(this);
                    }
                }
            }
        }
 
        private void EnsureKeepAliveOperation()
        {
            if (m_KeepAliveOperation == null)
            {
                lock (m_ThisLock)
                {
                    if (m_KeepAliveOperation == null)
                    {
                        WebSocketOperation.SendOperation keepAliveOperation = new WebSocketOperation.SendOperation(this);
                        keepAliveOperation.BufferType = WebSocketProtocolComponent.BufferType.UnsolicitedPong;
                        m_KeepAliveOperation = keepAliveOperation;
                    }
                }
            }
        }
 
        private void EnsureCloseOutputOperation()
        {
            if (m_CloseOutputOperation == null)
            {
                lock (m_ThisLock)
                {
                    if (m_CloseOutputOperation == null)
                    {
                        m_CloseOutputOperation = new WebSocketOperation.CloseOutputOperation(this);
                    }
                }
            }
        }
 
        private static void ReleaseLock(object lockObject, ref bool lockTaken)
        {
            Contract.Assert(lockObject != null, "'lockObject' MUST NOT be NULL.");
            if (lockTaken)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    Monitor.Exit(lockObject);
                    lockTaken = false;
                }
            }
        }
 
        private static WebSocketProtocolComponent.BufferType GetBufferType(WebSocketMessageType messageType,
            bool endOfMessage)
        {
            Contract.Assert(messageType == WebSocketMessageType.Binary || messageType == WebSocketMessageType.Text,
                string.Format(CultureInfo.InvariantCulture,
                    "The value of 'messageType' ({0}) is invalid. Valid message types: '{1}, {2}'",
                    messageType,
                    WebSocketMessageType.Binary,
                    WebSocketMessageType.Text));
 
            if (messageType == WebSocketMessageType.Text)
            {
                if (endOfMessage)
                {
                    return WebSocketProtocolComponent.BufferType.UTF8Message;
                }
 
                return WebSocketProtocolComponent.BufferType.UTF8Fragment;
            }
            else
            {
                if (endOfMessage)
                {
                    return WebSocketProtocolComponent.BufferType.BinaryMessage;
                }
 
                return WebSocketProtocolComponent.BufferType.BinaryFragment;
            }
        }
 
        private static WebSocketMessageType GetMessageType(WebSocketProtocolComponent.BufferType bufferType)
        {
            switch (bufferType)
            {
                case WebSocketProtocolComponent.BufferType.Close:
                    return WebSocketMessageType.Close;
                case WebSocketProtocolComponent.BufferType.BinaryFragment:
                case WebSocketProtocolComponent.BufferType.BinaryMessage:
                    return WebSocketMessageType.Binary;
                case WebSocketProtocolComponent.BufferType.UTF8Fragment:
                case WebSocketProtocolComponent.BufferType.UTF8Message:
                    return WebSocketMessageType.Text;
                default:
                    // This indicates a contract violation of the websocket protocol component,
                    // because we currently don't support any WebSocket extensions and would
                    // not accept a Websocket handshake requesting extensions
                    Contract.Assert(false,
                    string.Format(CultureInfo.InvariantCulture,
                        "The value of 'bufferType' ({0}) is invalid. Valid buffer types: {1}, {2}, {3}, {4}, {5}.",
                        bufferType,
                        WebSocketProtocolComponent.BufferType.Close,
                        WebSocketProtocolComponent.BufferType.BinaryFragment,
                        WebSocketProtocolComponent.BufferType.BinaryMessage,
                        WebSocketProtocolComponent.BufferType.UTF8Fragment,
                        WebSocketProtocolComponent.BufferType.UTF8Message));
 
                    throw new WebSocketException(WebSocketError.NativeError,
                        SR.GetString(SR.net_WebSockets_InvalidBufferType,
                            bufferType,
                            WebSocketProtocolComponent.BufferType.Close,
                            WebSocketProtocolComponent.BufferType.BinaryFragment,
                            WebSocketProtocolComponent.BufferType.BinaryMessage,
                            WebSocketProtocolComponent.BufferType.UTF8Fragment,
                            WebSocketProtocolComponent.BufferType.UTF8Message));
            }
        }
 
        internal void ValidateNativeBuffers(WebSocketProtocolComponent.Action action,
            WebSocketProtocolComponent.BufferType bufferType,
            WebSocketProtocolComponent.Buffer[] dataBuffers,
            uint dataBufferCount)
        {
            m_InternalBuffer.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);
        }
 
        internal void ThrowIfClosedOrAborted()
        {
            if (State == WebSocketState.Closed || State == WebSocketState.Aborted)
            {
                throw new WebSocketException(WebSocketError.InvalidState,
                    SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, GetType().FullName, State));
            }
        }
 
        private void ThrowIfAborted(bool aborted, Exception innerException)
        {
            if (aborted)
            {
                throw new WebSocketException(WebSocketError.InvalidState,
                    SR.GetString(SR.net_WebSockets_InvalidState_ClosedOrAborted, GetType().FullName, WebSocketState.Aborted),
                    innerException);
            }
        }
 
        private bool CanHandleExceptionDuringClose(Exception error)
        {
            Contract.Assert(error != null, "'error' MUST NOT be NULL.");
 
            if (State != WebSocketState.Closed)
            {
                return false;
            }
 
            return error is OperationCanceledException ||
                error is WebSocketException ||
                error is SocketException ||
                error is HttpListenerException ||
                error is IOException;
        }
 
        // We only want to throw an OperationCanceledException if the CancellationToken passed
        // down from the caller is canceled - not when Abort is called on another thread and
        // the linkedCancellationToken is canceled.
        private void ThrowIfConvertibleException(string methodName,
            Exception exception,
            CancellationToken cancellationToken,
            bool aborted)
        {
            Contract.Assert(exception != null, "'exception' MUST NOT be NULL.");
 
            if (s_LoggingEnabled && !string.IsNullOrEmpty(methodName))
            {
                Logging.Exception(Logging.WebSockets, this, methodName, exception);
            }
 
            OperationCanceledException operationCanceledException = exception as OperationCanceledException;
            if (operationCanceledException != null)
            {
                if (cancellationToken.IsCancellationRequested ||
                    !aborted)
                {
                    return;
                }
                ThrowIfAborted(aborted, exception);
            }
 
            WebSocketException convertedException = exception as WebSocketException;
            if (convertedException != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfAborted(aborted, convertedException);
                return;
            }
 
            SocketException socketException = exception as SocketException;
            if (socketException != null)
            {
                convertedException = new WebSocketException(socketException.NativeErrorCode, socketException);
            }
 
            HttpListenerException httpListenerException = exception as HttpListenerException;
            if (httpListenerException != null)
            {
                convertedException = new WebSocketException(httpListenerException.ErrorCode, httpListenerException);
            }
 
            IOException ioException = exception as IOException;
            if (ioException != null)
            {
                socketException = exception.InnerException as SocketException;
                if (socketException != null)
                {
                    convertedException = new WebSocketException(socketException.NativeErrorCode, ioException);
                }
            }
 
            if (convertedException != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfAborted(aborted, convertedException);
                throw convertedException;
            }
 
            AggregateException aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                // Collapse possibly nested graph into a flat list.
                // Empty inner exception list is unlikely but possible via public api.
                ReadOnlyCollection<Exception> unwrappedExceptions = aggregateException.Flatten().InnerExceptions;
                if (unwrappedExceptions.Count == 0)
                {
                    return;
                }
 
                foreach (Exception unwrappedException in unwrappedExceptions)
                {
                    ThrowIfConvertibleException(null, unwrappedException, cancellationToken, aborted);
                }
            }
        }
 
        private void CleanUp()
        {
            // Multithreading: This method is always called under the m_ThisLock lock
            if (m_CleanedUp)
            {
                return;
            }
 
            m_CleanedUp = true;
 
            if (SessionHandle != null)
            {
                SessionHandle.Dispose();
            }
 
            if (m_InternalBuffer != null)
            {
                m_InternalBuffer.Dispose(this.State);
            }
 
            if (m_ReceiveOutstandingOperationHelper != null)
            {
                m_ReceiveOutstandingOperationHelper.Dispose();
            }
 
            if (m_SendOutstandingOperationHelper != null)
            {
                m_SendOutstandingOperationHelper.Dispose();
            }
 
            if (m_CloseOutputOutstandingOperationHelper != null)
            {
                m_CloseOutputOutstandingOperationHelper.Dispose();
            }
 
            if (m_CloseOutstandingOperationHelper != null)
            {
                m_CloseOutstandingOperationHelper.Dispose();
            }
 
            if (m_InnerStream != null)
            {
                try
                {
                    m_InnerStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (IOException)
                {
                }
                catch (SocketException)
                {
                }
                catch (HttpListenerException)
                {
                }
            }
 
            m_KeepAliveTracker.Dispose();
        }
 
        private void OnBackgroundTaskException(Exception exception)
        {
            if (Interlocked.CompareExchange<Exception>(ref m_PendingException, exception, null) == null)
            {
                if (s_LoggingEnabled)
                {
                    Logging.Exception(Logging.WebSockets, this, Methods.Fault, exception);
                }
                Abort();
            }
        }
 
        private void ThrowIfPendingException()
        {
            Exception pendingException = Interlocked.Exchange<Exception>(ref m_PendingException, null);
            if (pendingException != null)
            {
                throw new WebSocketException(WebSocketError.Faulted, pendingException);
            }
        }
 
        private void ThrowIfDisposed()
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
 
        private void UpdateReceiveState(int newReceiveState, int expectedReceiveState)
        {
            int receiveState;
            if ((receiveState = Interlocked.Exchange(ref m_ReceiveState, newReceiveState)) != expectedReceiveState)
            {
                Contract.Assert(false,
                    string.Format(CultureInfo.InvariantCulture,
                        "'m_ReceiveState' had an invalid value '{0}'. The expected value was '{1}'.",
                        receiveState,
                        expectedReceiveState));
            }
        }
 
        private bool StartOnCloseReceived(ref bool thisLockTaken)
        {
            ThrowIfDisposed();
 
            if (IsStateTerminal(State) || State == WebSocketState.CloseReceived)
            {
                return false;
            }
 
            Monitor.Enter(m_ThisLock, ref thisLockTaken);
            if (IsStateTerminal(State) || State == WebSocketState.CloseReceived)
            {
                return false;
            }
 
            if (State == WebSocketState.Open)
            {
                m_State = WebSocketState.CloseReceived;
 
                if (m_CloseReceivedTaskCompletionSource == null)
                {
                    m_CloseReceivedTaskCompletionSource = new TaskCompletionSource<object>();
                }
 
                return false;
            }
 
            return true;
        }
 
        private void FinishOnCloseReceived(WebSocketCloseStatus closeStatus,
            string closeStatusDescription)
        {
            if (m_CloseReceivedTaskCompletionSource != null)
            {
                m_CloseReceivedTaskCompletionSource.TrySetResult(null);
            }
 
            m_CloseStatus = closeStatus;
            m_CloseStatusDescription = closeStatusDescription;
 
            if (s_LoggingEnabled)
            {
                string parameters = string.Format(CultureInfo.InvariantCulture,
                    "closeStatus: {0}, closeStatusDescription: {1}, m_State: {2}",
                    closeStatus, closeStatusDescription, m_State);
 
                Logging.PrintInfo(Logging.WebSockets, this, Methods.FinishOnCloseReceived, parameters);
            }
        }
 
        private async static void OnKeepAlive(object sender)
        {
            Contract.Assert(sender != null, "'sender' MUST NOT be NULL.");
            Contract.Assert((sender as WebSocketBase) != null, "'sender as WebSocketBase' MUST NOT be NULL.");
 
            WebSocketBase thisPtr = sender as WebSocketBase;
            bool lockTaken = false;
 
            if (s_LoggingEnabled)
            {
                Logging.Enter(Logging.WebSockets, thisPtr, Methods.OnKeepAlive, string.Empty);
            }
 
            CancellationToken linkedCancellationToken = CancellationToken.None;
            try
            {
                Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);
 
                if (thisPtr.m_IsDisposed ||
                    thisPtr.m_State != WebSocketState.Open ||
                    thisPtr.m_CloseOutputTask != null)
                {
                    return;
                }
 
                if (thisPtr.m_KeepAliveTracker.ShouldSendKeepAlive())
                {
                    bool ownsCancellationTokenSource = false;
                    try
                    {
                        ownsCancellationTokenSource = thisPtr.m_SendOutstandingOperationHelper.TryStartOperation(CancellationToken.None, out linkedCancellationToken);
                        if (ownsCancellationTokenSource)
                        {
                            thisPtr.EnsureKeepAliveOperation();
                            thisPtr.m_KeepAliveTask = thisPtr.m_KeepAliveOperation.Process(null, linkedCancellationToken);
                            ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
                            await thisPtr.m_KeepAliveTask.SuppressContextFlow();
                        }
                    }
                    finally
                    {
                        if (!lockTaken)
                        {
                            Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);
                        }
                        thisPtr.m_SendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
                        thisPtr.m_KeepAliveTask = null;
                    }
 
                    thisPtr.m_KeepAliveTracker.ResetTimer();
                }
            }
            catch (Exception exception)
            {
                try
                {
                    thisPtr.ThrowIfConvertibleException(Methods.OnKeepAlive,
                        exception,
                        CancellationToken.None,
                        linkedCancellationToken.IsCancellationRequested);
                    throw;
                }
                catch (Exception backgroundException)
                {
                    thisPtr.OnBackgroundTaskException(backgroundException);
                }
            }
            finally
            {
                ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
 
                if (s_LoggingEnabled)
                {
                    Logging.Exit(Logging.WebSockets, thisPtr, Methods.OnKeepAlive, string.Empty);
                }
            }
        }
 
        private abstract class WebSocketOperation
        {
            protected bool AsyncOperationCompleted { get; set; }
            private readonly WebSocketBase m_WebSocket;
 
            internal WebSocketOperation(WebSocketBase webSocket)
            {
                Contract.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
                m_WebSocket = webSocket;
                AsyncOperationCompleted = false;
            }
 
            public WebSocketReceiveResult ReceiveResult { get; protected set; }
            protected abstract int BufferCount { get; }
            protected abstract WebSocketProtocolComponent.ActionQueue ActionQueue { get; }
            protected abstract void Initialize(Nullable<ArraySegment<byte>> buffer, CancellationToken cancellationToken);
            protected abstract bool ShouldContinue(CancellationToken cancellationToken);
 
            // Multi-Threading: This method has to be called under a SessionHandle-lock. It returns true if a 
            // close frame was received. Handling the received close frame might involve IO - to make the locking
            // strategy easier and reduce one level in the await-hierarchy the IO is kicked off by the caller.
            protected abstract bool ProcessAction_NoAction();
            
            protected virtual void ProcessAction_IndicateReceiveComplete(
                Nullable<ArraySegment<byte>> buffer,
                WebSocketProtocolComponent.BufferType bufferType,
                WebSocketProtocolComponent.Action action,
                WebSocketProtocolComponent.Buffer[] dataBuffers,
                uint dataBufferCount,
                IntPtr actionContext)
            {
                throw new NotImplementedException();
            }
 
            protected abstract void Cleanup();
 
            internal async Task<WebSocketReceiveResult> Process(Nullable<ArraySegment<byte>> buffer,
                CancellationToken cancellationToken)
            {
                Contract.Assert(BufferCount >= 1 && BufferCount <= 2, "'bufferCount' MUST ONLY BE '1' or '2'.");
 
                bool sessionHandleLockTaken = false;
                AsyncOperationCompleted = false;
                ReceiveResult = null;
                try
                {
                    Monitor.Enter(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                    m_WebSocket.ThrowIfPendingException();
                    Initialize(buffer, cancellationToken);
 
                    while (ShouldContinue(cancellationToken))
                    {
                        WebSocketProtocolComponent.Action action;
                        WebSocketProtocolComponent.BufferType bufferType;
 
                        bool completed = false;
 
                        while (!completed)
                        {
                            WebSocketProtocolComponent.Buffer[] dataBuffers =
                                new WebSocketProtocolComponent.Buffer[BufferCount];
                            uint dataBufferCount = (uint)BufferCount;
                            IntPtr actionContext;
 
                            m_WebSocket.ThrowIfDisposed();
                            WebSocketProtocolComponent.WebSocketGetAction(m_WebSocket,
                                ActionQueue,
                                dataBuffers,
                                ref dataBufferCount,
                                out action,
                                out bufferType,
                                out actionContext);
 
                            switch (action)
                            {
                                case WebSocketProtocolComponent.Action.NoAction:
                                    if (ProcessAction_NoAction())
                                    {
                                        // A close frame was received
 
                                        Contract.Assert(ReceiveResult.Count == 0, "'receiveResult.Count' MUST be 0.");
                                        Contract.Assert(ReceiveResult.CloseStatus != null, "'receiveResult.CloseStatus' MUST NOT be NULL for message type 'Close'.");
                                        bool thisLockTaken = false;
                                        try
                                        {
                                            if (m_WebSocket.StartOnCloseReceived(ref thisLockTaken))
                                            {
                                                // If StartOnCloseReceived returns true the WebSocket close handshake has been completed
                                                // so there is no need to retake the SessionHandle-lock.
                                                // m_ThisLock lock is guaranteed to be taken by StartOnCloseReceived when returning true
                                                ReleaseLock(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                                bool callCompleteOnCloseCompleted = false;
 
                                                try
                                                {
                                                    callCompleteOnCloseCompleted = await m_WebSocket.StartOnCloseCompleted(
                                                        thisLockTaken, sessionHandleLockTaken, cancellationToken).SuppressContextFlow();
                                                }
                                                catch (Exception)
                                                {
                                                    // If an exception is thrown we know that the locks have been released,
                                                    // because we enforce IWebSocketStream.CloseNetworkConnectionAsync to yield
                                                    m_WebSocket.ResetFlagAndTakeLock(m_WebSocket.m_ThisLock, ref thisLockTaken);
                                                    throw;
                                                }
 
                                                if (callCompleteOnCloseCompleted)
                                                {
                                                    m_WebSocket.ResetFlagAndTakeLock(m_WebSocket.m_ThisLock, ref thisLockTaken);
                                                    m_WebSocket.FinishOnCloseCompleted();
                                                }
                                            }
                                            m_WebSocket.FinishOnCloseReceived(ReceiveResult.CloseStatus.Value, ReceiveResult.CloseStatusDescription);
                                        }
                                        finally
                                        {
                                            if (thisLockTaken)
                                            {
                                                ReleaseLock(m_WebSocket.m_ThisLock, ref thisLockTaken);
                                            }
                                        }
                                    }
                                    completed = true;
                                    break;
                                case WebSocketProtocolComponent.Action.IndicateReceiveComplete:
                                    ProcessAction_IndicateReceiveComplete(buffer,
                                        bufferType,
                                        action,
                                        dataBuffers,
                                        dataBufferCount,
                                        actionContext);
                                    break;
                                case WebSocketProtocolComponent.Action.ReceiveFromNetwork:
                                    int count = 0;
                                    try
                                    {
                                        ArraySegment<byte> payload = m_WebSocket.m_InternalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);
 
                                        ReleaseLock(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                        WebSocketHelpers.ThrowIfConnectionAborted(m_WebSocket.m_InnerStream, true);
                                        try
                                        {
                                            Task<int> readTask = m_WebSocket.m_InnerStream.ReadAsync(payload.Array,
                                                payload.Offset,
                                                payload.Count,
                                                cancellationToken);
                                            count = await readTask.SuppressContextFlow();
                                            m_WebSocket.m_KeepAliveTracker.OnDataReceived();
                                        }
                                        catch (ObjectDisposedException objectDisposedException)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, objectDisposedException);
                                        }
                                        catch (NotSupportedException notSupportedException)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, notSupportedException);
                                        }
                                        Monitor.Enter(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                        m_WebSocket.ThrowIfPendingException();
                                        // If the client unexpectedly closed the socket we throw an exception as we didn't get any close message
                                        if (count == 0)
                                        {
                                            throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                                        }
                                    }
                                    finally
                                    {
                                        WebSocketProtocolComponent.WebSocketCompleteAction(m_WebSocket,
                                            actionContext,
                                            count);
                                    }
                                    break;
                                case WebSocketProtocolComponent.Action.IndicateSendComplete:
                                    WebSocketProtocolComponent.WebSocketCompleteAction(m_WebSocket, actionContext, 0);
                                    AsyncOperationCompleted = true;
                                    ReleaseLock(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                    await m_WebSocket.m_InnerStream.FlushAsync().SuppressContextFlow();
                                    Monitor.Enter(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                    break;
                                case WebSocketProtocolComponent.Action.SendToNetwork:
                                    int bytesSent = 0;
                                    try
                                    {
                                        if (m_WebSocket.State != WebSocketState.CloseSent ||
                                            (bufferType != WebSocketProtocolComponent.BufferType.PingPong &&
                                            bufferType != WebSocketProtocolComponent.BufferType.UnsolicitedPong))
                                        {
                                            if (dataBufferCount == 0)
                                            {
                                                break;
                                            }
 
                                            List<ArraySegment<byte>> sendBuffers = new List<ArraySegment<byte>>((int)dataBufferCount);
                                            int sendBufferSize = 0;
                                            ArraySegment<byte> framingBuffer = m_WebSocket.m_InternalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);
                                            sendBuffers.Add(framingBuffer);
                                            sendBufferSize += framingBuffer.Count;
 
                                            // There can be at most 2 dataBuffers
                                            // - one for the framing header and one for the payload
                                            if (dataBufferCount == 2)
                                            {
                                                ArraySegment<byte> payload;
 
                                                // The second buffer might be from the pinned send payload buffer (1) or from the
                                                // internal native buffer (2).  In the case of a PONG response being generated, the buffer
                                                // would be from (2).  Even if the payload is from a WebSocketSend operation, the buffer
                                                // might be (1) only if no buffer copies were needed (in the case of no masking, for example).
                                                // Or it might be (2).  So, we need to check.
                                                if (m_WebSocket.m_InternalBuffer.IsPinnedSendPayloadBuffer(dataBuffers[1], bufferType))
                                                {
                                                    payload = m_WebSocket.m_InternalBuffer.ConvertPinnedSendPayloadFromNative(dataBuffers[1], bufferType);
                                                }
                                                else
                                                {
                                                    payload = m_WebSocket.m_InternalBuffer.ConvertNativeBuffer(action, dataBuffers[1], bufferType);
                                                }
 
                                                sendBuffers.Add(payload);
                                                sendBufferSize += payload.Count;
                                            }
 
                                            ReleaseLock(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                            WebSocketHelpers.ThrowIfConnectionAborted(m_WebSocket.m_InnerStream, false);
                                            await m_WebSocket.SendFrameAsync(sendBuffers, cancellationToken).SuppressContextFlow();
                                            Monitor.Enter(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                                            m_WebSocket.ThrowIfPendingException();
                                            bytesSent += sendBufferSize;
                                            m_WebSocket.m_KeepAliveTracker.OnDataSent();
                                        }
                                    }
                                    finally
                                    {
                                        WebSocketProtocolComponent.WebSocketCompleteAction(m_WebSocket,
                                            actionContext,
                                            bytesSent);
                                    }
 
                                    break;
                                default:
                                    string assertMessage = string.Format(CultureInfo.InvariantCulture,
                                        "Invalid action '{0}' returned from WebSocketGetAction.",
                                        action);
                                    Contract.Assert(false, assertMessage);
                                    throw new InvalidOperationException();
                            }
                        }
 
                        // WebSocketGetAction has returned NO_ACTION. In general, WebSocketGetAction will return
                        // NO_ACTION if there is no work item available to process at the current moment. But
                        // there could be work items on the queue still.  Those work items can't be returned back
                        // until the current work item (being done by another thread) is complete.
                        //
                        // It's possible that another thread might be finishing up an async operation and needs
                        // to call WebSocketCompleteAction. Once that happens, calling WebSocketGetAction on this
                        // thread might return something else to do. This happens, for example, if the RECEIVE thread
                        // ends up having to begin sending out a PONG response (due to it receiving a PING) and the
                        // current SEND thread has posted a WebSocketSend but it can't be processed yet until the
                        // RECEIVE thread has finished sending out the PONG response.
                        // 
                        // So, we need to release the lock briefly to give the other thread a chance to finish
                        // processing.  We won't actually exit this outter loop and return from this async method
                        // until the caller's async operation has been fully completed.
                        ReleaseLock(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                        Monitor.Enter(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                    }
                }
                finally
                {
                    Cleanup();
                    ReleaseLock(m_WebSocket.SessionHandle, ref sessionHandleLockTaken);
                }
 
                return ReceiveResult;
            }
 
            public class ReceiveOperation : WebSocketOperation
            {
                int m_ReceiveState;
                bool m_PongReceived;
                bool m_ReceiveCompleted;
 
                public ReceiveOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                }
 
                protected override WebSocketProtocolComponent.ActionQueue ActionQueue
                {
                    get { return WebSocketProtocolComponent.ActionQueue.Receive; }
                }
 
                protected override int BufferCount
                {
                    get { return 1; }
                }
 
                protected override void Initialize(Nullable<ArraySegment<byte>> buffer, CancellationToken cancellationToken)
                {
                    Contract.Assert(buffer != null, "'buffer' MUST NOT be NULL.");
                    m_PongReceived = false;
                    m_ReceiveCompleted = false;
                    m_WebSocket.ThrowIfDisposed();
 
                    int originalReceiveState = Interlocked.CompareExchange(ref m_WebSocket.m_ReceiveState,
                        ReceiveState.Application, ReceiveState.Idle);
 
                    switch (originalReceiveState)
                    {
                        case ReceiveState.Idle:
                            m_ReceiveState = ReceiveState.Application;
                            break;
                        case ReceiveState.Application:
                            Contract.Assert(false, "'originalReceiveState' MUST NEVER be ReceiveState.Application at this point.");
                            break;
                        case ReceiveState.PayloadAvailable:
                            WebSocketReceiveResult receiveResult;
                            if (!m_WebSocket.m_InternalBuffer.ReceiveFromBufferedPayload(buffer.Value, out receiveResult))
                            {
                                m_WebSocket.UpdateReceiveState(ReceiveState.Idle, ReceiveState.PayloadAvailable);
                            }
                            ReceiveResult = receiveResult;
                            m_ReceiveCompleted = true;
                            break;
                        default:
                            Contract.Assert(false,
                                string.Format(CultureInfo.InvariantCulture, "Invalid ReceiveState '{0}'.", originalReceiveState));
                            break;
                    }
                }
 
                protected override void Cleanup()
                {
                }
 
                protected override bool ShouldContinue(CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (m_ReceiveCompleted)
                    {
                        return false;
                    }
 
                    m_WebSocket.ThrowIfDisposed();
                    m_WebSocket.ThrowIfPendingException();
                    WebSocketProtocolComponent.WebSocketReceive(m_WebSocket);
 
                    return true;
                }
 
                protected override bool ProcessAction_NoAction()
                {
                    if (m_PongReceived)
                    {
                        m_ReceiveCompleted = false;
                        m_PongReceived = false;
                        return false;
                    }
 
                    Contract.Assert(ReceiveResult != null,
                        "'ReceiveResult' MUST NOT be NULL.");
                    m_ReceiveCompleted = true;
 
                    if (ReceiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return true;
                    }
 
                    return false;
                }
 
                protected override void ProcessAction_IndicateReceiveComplete(
                    Nullable<ArraySegment<byte>> buffer,
                    WebSocketProtocolComponent.BufferType bufferType,
                    WebSocketProtocolComponent.Action action,
                    WebSocketProtocolComponent.Buffer[] dataBuffers,
                    uint dataBufferCount,
                    IntPtr actionContext)
                {
                    Contract.Assert(buffer != null, "'buffer MUST NOT be NULL.");
 
                    int bytesTransferred = 0;
                    m_PongReceived = false;
 
                    if (bufferType == WebSocketProtocolComponent.BufferType.PingPong)
                    {
                        // ignoring received pong frame 
                        m_PongReceived = true;
                        WebSocketProtocolComponent.WebSocketCompleteAction(m_WebSocket,
                            actionContext,
                            bytesTransferred);
                        return;
                    }
 
                    WebSocketReceiveResult receiveResult;
                    try
                    {
                        ArraySegment<byte> payload;
                        WebSocketMessageType messageType = GetMessageType(bufferType);
                        int newReceiveState = ReceiveState.Idle;
 
                        if (bufferType == WebSocketProtocolComponent.BufferType.Close)
                        {
                            payload = WebSocketHelpers.EmptyPayload;
                            string reason;
                            WebSocketCloseStatus closeStatus;
                            m_WebSocket.m_InternalBuffer.ConvertCloseBuffer(action, dataBuffers[0], out closeStatus, out reason);
 
                            receiveResult = new WebSocketReceiveResult(bytesTransferred,
                                messageType, true, closeStatus, reason);
                        }
                        else
                        {
                            payload = m_WebSocket.m_InternalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);
 
                            bool endOfMessage = bufferType ==
                                WebSocketProtocolComponent.BufferType.BinaryMessage ||
                                bufferType == WebSocketProtocolComponent.BufferType.UTF8Message ||
                                bufferType == WebSocketProtocolComponent.BufferType.Close;
 
                            if (payload.Count > buffer.Value.Count)
                            {
                                m_WebSocket.m_InternalBuffer.BufferPayload(payload, buffer.Value.Count, messageType, endOfMessage);
                                newReceiveState = ReceiveState.PayloadAvailable;
                                endOfMessage = false;
                            }
 
                            bytesTransferred = Math.Min(payload.Count, (int)buffer.Value.Count);
                            if (bytesTransferred > 0)
                            {
                                Buffer.BlockCopy(payload.Array,
                                    payload.Offset,
                                    buffer.Value.Array,
                                    buffer.Value.Offset,
                                    bytesTransferred);
                            }
 
                            receiveResult = new WebSocketReceiveResult(bytesTransferred, messageType, endOfMessage);
                        }
 
                        m_WebSocket.UpdateReceiveState(newReceiveState, m_ReceiveState);
                    }
                    finally
                    {
                        WebSocketProtocolComponent.WebSocketCompleteAction(m_WebSocket,
                            actionContext,
                            bytesTransferred);
                    }
 
                    ReceiveResult = receiveResult;
                }
            }
 
            public class SendOperation : WebSocketOperation
            {
                protected bool m_BufferHasBeenPinned;
 
                public SendOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                }
 
                protected override WebSocketProtocolComponent.ActionQueue ActionQueue
                {
                    get { return WebSocketProtocolComponent.ActionQueue.Send; }
                }
 
                protected override int BufferCount
                {
                    get { return 2; }
                }
 
                protected virtual Nullable<WebSocketProtocolComponent.Buffer> CreateBuffer(Nullable<ArraySegment<byte>> buffer)
                {
                    if (buffer == null)
                    {
                        return null;
                    }
 
                    WebSocketProtocolComponent.Buffer payloadBuffer;
                    payloadBuffer = new WebSocketProtocolComponent.Buffer();
                    m_WebSocket.m_InternalBuffer.PinSendBuffer(buffer.Value, out m_BufferHasBeenPinned);
                    payloadBuffer.Data.BufferData = m_WebSocket.m_InternalBuffer.ConvertPinnedSendPayloadToNative(buffer.Value);
                    payloadBuffer.Data.BufferLength = (uint)buffer.Value.Count;
                    return payloadBuffer;
                }
 
                protected override bool ProcessAction_NoAction()
                {
                    return false;
                }
 
                protected override void Cleanup()
                {
                    if (m_BufferHasBeenPinned)
                    {
                        m_BufferHasBeenPinned = false;
                        m_WebSocket.m_InternalBuffer.ReleasePinnedSendBuffer();
                    }
                }
 
                internal WebSocketProtocolComponent.BufferType BufferType { get; set; }
 
                protected override void Initialize(Nullable<ArraySegment<byte>> buffer,
                    CancellationToken cancellationToken)
                {
                    Contract.Assert(!m_BufferHasBeenPinned, "'m_BufferHasBeenPinned' MUST NOT be pinned at this point.");
                    m_WebSocket.ThrowIfDisposed();
                    m_WebSocket.ThrowIfPendingException();
 
                    Nullable<WebSocketProtocolComponent.Buffer> payloadBuffer = CreateBuffer(buffer);
                    if (payloadBuffer != null)
                    {
                        WebSocketProtocolComponent.WebSocketSend(m_WebSocket, BufferType, payloadBuffer.Value);
                    }
                    else
                    {
                        WebSocketProtocolComponent.WebSocketSendWithoutBody(m_WebSocket, BufferType);
                    }
                }
 
                protected override bool ShouldContinue(CancellationToken cancellationToken)
                {
                    Contract.Assert(ReceiveResult == null, "'ReceiveResult' MUST be NULL.");
                    if (AsyncOperationCompleted)
                    {
                        return false;
                    }
 
                    cancellationToken.ThrowIfCancellationRequested();
                    return true;
                }
            }
 
            public class CloseOutputOperation : SendOperation
            {
                public CloseOutputOperation(WebSocketBase webSocket)
                    : base(webSocket)
                {
                    BufferType = WebSocketProtocolComponent.BufferType.Close;
                }
 
                internal WebSocketCloseStatus CloseStatus { get; set; }
                internal string CloseReason { get; set; }
 
                protected override Nullable<WebSocketProtocolComponent.Buffer> CreateBuffer(Nullable<ArraySegment<byte>> buffer)
                {
                    Contract.Assert(buffer == null, "'buffer' MUST BE NULL.");
                    m_WebSocket.ThrowIfDisposed();
                    m_WebSocket.ThrowIfPendingException();
 
                    if (CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        return null;
                    }
 
                    WebSocketProtocolComponent.Buffer payloadBuffer = new WebSocketProtocolComponent.Buffer();
                    if (CloseReason != null)
                    {
                        byte[] blob = UTF8Encoding.UTF8.GetBytes(CloseReason);
                        Contract.Assert(blob.Length <= WebSocketHelpers.MaxControlFramePayloadLength,
                            "The close reason is too long.");
                        ArraySegment<byte> closeBuffer = new ArraySegment<byte>(blob, 0, Math.Min(WebSocketHelpers.MaxControlFramePayloadLength, blob.Length));
                        m_WebSocket.m_InternalBuffer.PinSendBuffer(closeBuffer, out m_BufferHasBeenPinned);
                        payloadBuffer.CloseStatus.ReasonData = m_WebSocket.m_InternalBuffer.ConvertPinnedSendPayloadToNative(closeBuffer);
                        payloadBuffer.CloseStatus.ReasonLength = (uint)closeBuffer.Count;
                    }
 
                    payloadBuffer.CloseStatus.CloseStatus = (ushort)CloseStatus;
                    return payloadBuffer;
                }
            }
        }
 
        private abstract class KeepAliveTracker : IDisposable
        {
            // Multi-Threading: only one thread at a time is allowed to call OnDataReceived or OnDataSent 
            // - but both methods can be called from different threads at the same time.
            public abstract void OnDataReceived();
            public abstract void OnDataSent();
            public abstract void Dispose();
            public abstract void StartTimer(WebSocketBase webSocket);
            public abstract void ResetTimer();
            public abstract bool ShouldSendKeepAlive();
            
            public static KeepAliveTracker Create(TimeSpan keepAliveInterval)
            {
                if ((int)keepAliveInterval.TotalMilliseconds > 0)
                {
                    return new DefaultKeepAliveTracker(keepAliveInterval);
                }
 
                return new DisabledKeepAliveTracker();
            }
 
            private class DisabledKeepAliveTracker : KeepAliveTracker
            {
                public override void OnDataReceived() 
                {
                }
 
                public override void OnDataSent()
                {
                }
 
                public override void ResetTimer()
                {
                }
 
                public override void StartTimer(WebSocketBase webSocket)
                {
                }
 
                public override bool ShouldSendKeepAlive()
                {
                    return false;
                }
 
                public override void Dispose()
                {
                }
            }
 
            private class DefaultKeepAliveTracker : KeepAliveTracker
            {
                private static readonly TimerCallback s_KeepAliveTimerElapsedCallback = new TimerCallback(OnKeepAlive);
                private readonly TimeSpan m_KeepAliveInterval;
                private readonly Stopwatch m_LastSendActivity;
                private readonly Stopwatch m_LastReceiveActivity;
                private Timer m_KeepAliveTimer;
 
                public DefaultKeepAliveTracker(TimeSpan keepAliveInterval)
                {
                    m_KeepAliveInterval = keepAliveInterval;
                    m_LastSendActivity = new Stopwatch();
                    m_LastReceiveActivity = new Stopwatch();
                }
 
                public override void OnDataReceived()
                {
                    m_LastReceiveActivity.Restart();
                }
 
                public override void OnDataSent()
                {
                    m_LastSendActivity.Restart();
                }
 
                public override void ResetTimer()
                {
                    ResetTimer((int)m_KeepAliveInterval.TotalMilliseconds);
                }
 
                public override void StartTimer(WebSocketBase webSocket)
                {
                    Contract.Assert(webSocket != null, "'webSocket' MUST NOT be NULL.");
                    Contract.Assert(webSocket.m_KeepAliveTracker != null, 
                        "'webSocket.m_KeepAliveTracker' MUST NOT be NULL at this point.");
                    int keepAliveIntervalMilliseconds = (int)m_KeepAliveInterval.TotalMilliseconds;
                    Contract.Assert(keepAliveIntervalMilliseconds > 0, "'keepAliveIntervalMilliseconds' MUST be POSITIVE.");
                    
                    // The correct pattern is to first initialize the Timer object, assign it to the member variable
                    // and only afterwards enable the Timer. This is required because the constructor, together with 
                    // the assignment are not guaranteed to be an atomic operation, which creates a ---- between the 
                    // assignment and the Timer callback.
                    if (ExecutionContext.IsFlowSuppressed())
                    {
                        m_KeepAliveTimer = new Timer(s_KeepAliveTimerElapsedCallback, webSocket, Timeout.Infinite, 
                            Timeout.Infinite);
 
                        m_KeepAliveTimer.Change(keepAliveIntervalMilliseconds, Timeout.Infinite);
                    }
                    else
                    {
                        using (ExecutionContext.SuppressFlow())
                        {
                            m_KeepAliveTimer = new Timer(s_KeepAliveTimerElapsedCallback, webSocket, Timeout.Infinite,
                                Timeout.Infinite);
 
                            m_KeepAliveTimer.Change(keepAliveIntervalMilliseconds, Timeout.Infinite);
                        }
                    }
                }
 
                public override bool ShouldSendKeepAlive()
                {
                    TimeSpan idleTime = GetIdleTime();
                    if (idleTime >= m_KeepAliveInterval)
                    {
                        return true;
                    }
 
                    ResetTimer((int)(m_KeepAliveInterval - idleTime).TotalMilliseconds);
                    return false;
                }
 
                public override void Dispose()
                {
                    m_KeepAliveTimer.Dispose();
                }
 
                private void ResetTimer(int dueInMilliseconds)
                {
                    m_KeepAliveTimer.Change(dueInMilliseconds, Timeout.Infinite);
                }
 
                private TimeSpan GetIdleTime()
                {
                    TimeSpan sinceLastSendActivity = GetTimeElapsed(m_LastSendActivity);
                    TimeSpan sinceLastReceiveActivity = GetTimeElapsed(m_LastReceiveActivity);
 
                    if (sinceLastReceiveActivity < sinceLastSendActivity)
                    {
                        return sinceLastReceiveActivity;
                    }
 
                    return sinceLastSendActivity;
                }
 
                private TimeSpan GetTimeElapsed(Stopwatch watch)
                {
                    if (watch.IsRunning)
                    {
                        return watch.Elapsed;
                    }
 
                    return m_KeepAliveInterval;
                }
            }
        }
 
        private class OutstandingOperationHelper : IDisposable
        {
            private volatile int m_OperationsOutstanding;
            private volatile CancellationTokenSource m_CancellationTokenSource;
            private volatile bool m_IsDisposed;
            private readonly object m_ThisLock = new object();
 
            public bool TryStartOperation(CancellationToken userCancellationToken, out CancellationToken linkedCancellationToken)
            {
                linkedCancellationToken = CancellationToken.None;
                ThrowIfDisposed();
 
                lock (m_ThisLock)
                {
                    int operationsOutstanding = ++m_OperationsOutstanding;
 
                    if (operationsOutstanding == 1)
                    {
                        linkedCancellationToken = CreateLinkedCancellationToken(userCancellationToken);
                        return true;
                    }
 
                    Contract.Assert(operationsOutstanding >= 1, "'operationsOutstanding' must never be smaller than 1.");
                    return false;
                }
            }
 
            public void CompleteOperation(bool ownsCancellationTokenSource)
            {
                if (m_IsDisposed)
                {
                    // no-op if the WebSocket is already aborted
                    return;
                }
 
                CancellationTokenSource snapshot = null;
 
                lock (m_ThisLock)
                {
                    --m_OperationsOutstanding;
                    Contract.Assert(m_OperationsOutstanding >= 0, "'m_OperationsOutstanding' must never be smaller than 0.");
 
                    if (ownsCancellationTokenSource)
                    {
                        snapshot = m_CancellationTokenSource;
                        m_CancellationTokenSource = null;
                    }
                }
 
                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }
 
            // Has to be called under m_ThisLock lock
            private CancellationToken CreateLinkedCancellationToken(CancellationToken cancellationToken)
            {
                CancellationTokenSource linkedCancellationTokenSource;
 
                if (cancellationToken == CancellationToken.None)
                {
                    linkedCancellationTokenSource = new CancellationTokenSource();
                }
                else
                {
                    linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                        new CancellationTokenSource().Token);
                }
 
                Contract.Assert(m_CancellationTokenSource == null, "'m_CancellationTokenSource' MUST be NULL.");
                m_CancellationTokenSource = linkedCancellationTokenSource;
 
                return linkedCancellationTokenSource.Token;
            }
 
            public void CancelIO()
            {
                CancellationTokenSource cancellationTokenSourceSnapshot = null;
 
                lock (m_ThisLock)
                {
                    if (m_OperationsOutstanding == 0)
                    {
                        return;
                    }
 
                    cancellationTokenSourceSnapshot = m_CancellationTokenSource;
                }
 
                if (cancellationTokenSourceSnapshot != null)
                {
                    try
                    {
                        cancellationTokenSourceSnapshot.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Simply ignore this exception - There is apparently a rare race condition
                        // where the cancellationTokensource is disposed before the Cancel method call completed.
                    }
                }
            }
 
            public void Dispose()
            {
                if (m_IsDisposed)
                {
                    return;
                }
 
                CancellationTokenSource snapshot = null;
                lock (m_ThisLock)
                {
                    if (m_IsDisposed)
                    {
                        return;
                    }
 
                    m_IsDisposed = true;
                    snapshot = m_CancellationTokenSource;
                    m_CancellationTokenSource = null;
                }
 
                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }
 
            private void ThrowIfDisposed()
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }
        }
 
        internal interface IWebSocketStream
        {
            // Switching to opaque mode will change the behavior to use the knowledge that the WebSocketBase class
            // is pinning all payloads already and that we will have at most one outstanding send and receive at any
            // given time. This allows us to avoid creation of OverlappedData and pinning for each operation.
 
            void SwitchToOpaqueMode(WebSocketBase webSocket);
            void Abort();
            bool SupportsMultipleWrite { get; }
            Task MultipleWriteAsync(IList<ArraySegment<byte>> buffers, CancellationToken cancellationToken);
 
            // Any implementation has to guarantee that no exception is thrown synchronously
            // for example by enforcing a Task.Yield at the beginning of the method
            // This is necessary to enforce an API contract (for WebSocketBase.StartOnCloseCompleted) that ensures 
            // that all locks have been released whenever an exception is thrown from it.
            Task CloseNetworkConnectionAsync(CancellationToken cancellationToken);
        }
 
        private static class ReceiveState
        {
            internal const int SendOperation = -1;
            internal const int Idle = 0;
            internal const int Application = 1;
            internal const int PayloadAvailable = 2;
        }
 
        internal static class Methods
        {
            internal const string ReceiveAsync = "ReceiveAsync";
            internal const string SendAsync = "SendAsync";
            internal const string CloseAsync = "CloseAsync";
            internal const string CloseOutputAsync = "CloseOutputAsync";
            internal const string Abort = "Abort";
            internal const string Initialize = "Initialize";
            internal const string Fault = "Fault";
            internal const string StartOnCloseCompleted = "StartOnCloseCompleted";
            internal const string FinishOnCloseReceived = "FinishOnCloseReceived";
            internal const string OnKeepAlive = "OnKeepAlive";
        }
    }
}