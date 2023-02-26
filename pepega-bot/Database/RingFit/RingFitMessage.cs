using System;

namespace pepega_bot.Database.RingFit
{
    public enum RingFitMessageType
    {
        Daily = 1
    }
    public class RingFitMessage
    {
        public int Id { get; set; }
        public ulong MessageId { get; set; }
        public DateTime MessageTime { get; set; }
        public RingFitMessageType MessageType { get; set; }
    }
}