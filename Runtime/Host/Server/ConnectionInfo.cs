using System;
using UnityEngine;
namespace RemoteTuning.Host.Server
{
    /// <summary>
    /// Connection info shared with the client (via QR code).
    /// Compatible with JsonUtility.
    /// </summary>
    [Serializable]
    public class ConnectionInfo
    {
        public string host;        // Host IP
        public int port;           // WebSocket port
        public string gameId;      // Unique game identifier
        public string gameName;    // Human-readable game name
        public long timestamp;     // Generation timestamp
        public ConnectionInfo()
        {
            timestamp = DateTime.UtcNow.Ticks;
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
        public static ConnectionInfo FromJson(string json)
        {
            return JsonUtility.FromJson<ConnectionInfo>(json);
        }
        public string GetWebSocketUrl()
        {
            return $"ws://{host}:{port}";
        }
    }
}
