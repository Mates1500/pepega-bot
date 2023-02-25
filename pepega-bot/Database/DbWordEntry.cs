using Microsoft.EntityFrameworkCore;

namespace pepega_bot.Database
{
    [Index(nameof(Value))]
    internal class DbWordEntry
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int Count { get; set; }
    }
}