using System;

namespace pepega_bot.Database
{
    internal class EmoteStatMatch
    {
        public uint Id { get; set; }
        public ulong UserId { get; set; }
        public int MessageLength { get; set; }
        public int MatchesCount { get; set; }
        public decimal CharactersPerEmote => MatchesCount == 0 ? 0 :(decimal) MessageLength / MatchesCount;
        public DateTime TimestampUtc { get; set; }
        public ulong MessageId { get; set; }

        public void UpdateFrom(EmoteStatMatch esm)
        {
            UserId = esm.UserId;
            MessageLength = esm.MessageLength;
            MatchesCount = esm.MatchesCount;
            TimestampUtc = esm.TimestampUtc;
            MessageId = esm.MessageId;
        }
    }
}