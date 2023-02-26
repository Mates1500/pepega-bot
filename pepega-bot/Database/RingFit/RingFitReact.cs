using System;
using Microsoft.EntityFrameworkCore;

namespace pepega_bot.Database.RingFit
{
    [Index(nameof(MessageTime))]
    [Index(nameof(UserId))]
    public class RingFitReact
    {
        public int Id { get; set; }
        public string EmoteId { get; set; }
        public ulong UserId { get; set; }
        public ulong MessageId { get; set; }
        public uint MinuteValue { get; set; }
        public bool IsApproximateValue { get; set; }
        public DateTime MessageTime { get; set; }

        public override string ToString()
        {
            return $"UserId - {UserId}, {MinuteValue} minutes, IsApproximate {IsApproximateValue}";
        }
    }
}