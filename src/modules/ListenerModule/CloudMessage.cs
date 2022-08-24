namespace ListenerModule
{
    using System;

    public class CloudMessage
    {
        public CloudMessage(string deviceId, DateTime timestamp, string message)
        {
            this.deviceId = deviceId;
            this.timestamp = timestamp;
            this.message = message;
        }

        public string deviceId { get; private set; }
        public DateTime timestamp { get; private set; }
        public string message { get; private set; }
    }
}
