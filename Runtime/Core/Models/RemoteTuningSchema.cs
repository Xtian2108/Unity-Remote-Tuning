using System;
using UnityEngine;

namespace RemoteTuning.Core.Models
{
    /// <summary>
    /// Full game schema, compatible with JsonUtility.
    /// Sent to the client so it can build the dynamic UI.
    /// </summary>
    [Serializable]
    public class RemoteTuningSchema
    {
        // Game metadata
        public string gameId;
        public string gameName;
        public string version;
        
        // Control list (array instead of List for JsonUtility)
        public ControlDefinition[] controls;
        
        // Generation timestamp
        public long timestamp;

        public RemoteTuningSchema()
        {
            version = "1.0";
            timestamp = DateTime.UtcNow.Ticks;
        }
        
        public string ToJson()
        {
            return JsonUtility.ToJson(this, prettyPrint: false);
        }
        
        public static RemoteTuningSchema FromJson(string json)
        {
            return JsonUtility.FromJson<RemoteTuningSchema>(json);
        }
    }
}
