using System;
using UnityEngine;
using RemoteTuning.Core.Models;
namespace RemoteTuning.Core.Protocol
{
    [Serializable]
    public class RTMessage
    {
        public string type;
        public long timestamp;
        public RTMessage()
        {
            timestamp = DateTime.UtcNow.Ticks;
        }
        public RTMessage(string messageType)
        {
            type = messageType;
            timestamp = DateTime.UtcNow.Ticks;
        }
        public virtual string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
    [Serializable]
    public class HelloMessage : RTMessage
    {
        public string clientId;
        public string clientName;
        public HelloMessage() : base("hello") { }
    }
    [Serializable]
    public class SchemaMessage : RTMessage
    {
        public string gameId;
        public string gameName;
        public string version;
        public ControlDefinition[] controls;
        public SchemaMessage() : base("schema") { }
    }
    [Serializable]
    public class SetMessage : RTMessage
    {
        public string id;
        public string valueType;
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public string stringValue;
        public SetMessage() : base("set") { }
        public object GetValue()
        {
            switch (valueType)
            {
                case "Float":
                    return floatValue;
                case "Int":
                    return intValue;
                case "Bool":
                    return boolValue;
                case "String":
                case "Enum":
                    return stringValue;
                default:
                    return null;
            }
        }
    }
    [Serializable]
    public class RequestValuesMessage : RTMessage
    {
        public RequestValuesMessage() : base("requestValues") { }
    }
    [Serializable]
    public class ValueData
    {
        public string id;
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public string stringValue;
    }
    [Serializable]
    public class ValuesMessage : RTMessage
    {
        public ValueData[] values;
        public ValuesMessage() : base("values") { }
    }
    [Serializable]
    public class PingMessage : RTMessage
    {
        public PingMessage() : base("ping") { }
    }
    [Serializable]
    public class PongMessage : RTMessage
    {
        public PongMessage() : base("pong") { }
    }
    [Serializable]
    public class ErrorMessage : RTMessage
    {
        public string errorCode;
        public string errorMessage;
        public ErrorMessage() : base("error") { }
    }
}
