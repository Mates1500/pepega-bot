using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    internal class DatabaseService
    {
        private readonly ResultDatabaseContext _dbContext;

        public DatabaseService(IConfigurationService config)
        {
            _dbContext = new ResultDatabaseContext(config);
        }

        public async Task InsertOrAddCountByOne(string wordValue)
        {
            var currentResult = _dbContext.WordEntries.Where(x => x.Value == wordValue).ToList();
            if (currentResult.Count < 1)
            {
                _dbContext.WordEntries.Add(new DbWordEntry {Value = wordValue, Count = 1});
            }
            else
            {
                var currentWord = currentResult[0];
                currentWord.Count += 1;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task InsertRingFitReact(RingFitReact r)
        {
            _dbContext.RingFitReacts.Add(r);

            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRingFitReact(ulong reactUserId, ulong reactedMessageId)
        {
            var results =
                _dbContext.RingFitReacts.Where(x => x.MessageId == reactedMessageId && x.UserId == reactUserId).ToList();

            if (results.Count == 0)
                return;

            _dbContext.Remove(results[0]);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRingFitReactsFor(ulong messageId)
        {
            var messagesToRemove = _dbContext.RingFitReacts.Where(x => x.MessageId == messageId);

            _dbContext.RingFitReacts.RemoveRange(messagesToRemove);

            await _dbContext.SaveChangesAsync();
        }

        public IEnumerable<RingFitReact> GetReactsForWeekIn(DateTime dt)
        {
            var goBackDays = 0;
            if (dt.DayOfWeek != 0) // retarded murican failsafe because their week starts with Sunday
                goBackDays = (int) dt.DayOfWeek - 1;
            else
                goBackDays = 6;

            var weekStart = dt.AddDays(-goBackDays);
            var followingWeekStart = weekStart.AddDays(7);

            return _dbContext.RingFitReacts.Where(x =>
                x.MessageTime >= weekStart && x.MessageTime < followingWeekStart);
        }
    }
}