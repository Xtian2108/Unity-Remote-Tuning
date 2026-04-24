using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RemoteTuning.Host.Server
{
    /// <summary>
    /// WebSocket behavior that handles each connected client.
    /// Implements WebSocketBehavior from websocket-sharp.
    /// </summary>
    public class RemoteTuningBehavior : WebSocketBehavior
    {
        public event Action<string, string> OnMessageReceived;
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        
        public string ClientId { get; private set; }

        protected override void OnOpen()
        {
            ClientId = ID;
            Debug.Log($"[RemoteTuningBehavior] Client connected: {ClientId}");
            OnClientConnected?.Invoke(ClientId);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log($"[RemoteTuningBehavior] Client disconnected: {ClientId}");
            OnClientDisconnected?.Invoke(ClientId);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsText)
            {
                OnMessageReceived?.Invoke(ClientId, e.Data);
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.LogError($"[RemoteTuningBehavior] Error from {ClientId}: {e.Message}");
            if (e.Exception != null)
                Debug.LogError($"[RemoteTuningBehavior] Exception: {e.Exception}");
        }

        public void SendMessage(string message)
        {
            try
            {
                Send(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteTuningBehavior] Error sending to {ClientId}: {ex.Message}");
            }
        }
    }
}
