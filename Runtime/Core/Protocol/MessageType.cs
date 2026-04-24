namespace RemoteTuning.Core.Protocol
{
    public enum MessageType
    {
        Hello,
        Schema,
        Set,
        RequestValues,
        Values,
        Ping,
        Pong,
        Error
    }
}
