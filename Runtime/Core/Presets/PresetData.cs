using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTuning.Core.Presets
{
    /// <summary>
    /// Data for a saved preset.
    /// </summary>
    [Serializable]
    public class PresetData
    {
        public string presetName;
        public string description;
        public long timestamp; // DateTime.Ticks
        public Dictionary<string, object> values = new Dictionary<string, object>();
        
        public PresetData()
        {
            timestamp = DateTime.Now.Ticks;
        }

        public PresetData(string name, string desc = "")
        {
            presetName = name;
            description = desc;
            timestamp = DateTime.Now.Ticks;
        }

        public DateTime GetDateTime()
        {
            return new DateTime(timestamp);
        }

        public string GetFormattedDate()
        {
            return GetDateTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}

