using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pepega_bot.Database;
using pepega_bot.Database.RingFit;

namespace pepega_bot.Module
{
    internal class DatabaseService
    {
        private readonly ResultDatabaseContext _dbContext;

        public DatabaseService(ResultDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task InsertOrAddWordCountByOne(string wordValue)
        {
            var currentResult = _dbContext.WordEntries.AsQueryable().Where(x => x.Value == wordValue).ToList();
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

        public async Task InsertOrUpdateRingFitReact(RingFitReact r)
        {
            try
            {
                var currentResult = _dbContext.RingFitReacts.Single(x => x.UserId == r.UserId
                                                                         && x.MessageId == r.MessageId);
                // already exists
                currentResult.MinuteValue = r.MinuteValue;
                currentResult.IsApproximateValue = r.IsApproximateValue;
                _dbContext.RingFitReacts.Update(currentResult);
            }
            catch (InvalidOperationException)
            {
                _dbContext.RingFitReacts.Add(r);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> RemoveRingFitReact(ulong reactUserId, ulong reactedMessageId)
        {
            var results =
                _dbContext.RingFitReacts.AsQueryable().Where(x => x.MessageId == reactedMessageId &&
                                                                  x.UserId == reactUserId)
                    .ToList();

            if (results.Count == 0)
                return false;

            _dbContext.Remove(results[0]);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task RemoveRingFitReactsFor(ulong messageId)
        {
            var messagesToRemove = _dbContext.RingFitReacts.AsQueryable().Where(x => x.MessageId == messageId);

            _dbContext.RingFitReacts.RemoveRange(messagesToRemove);

            await _dbContext.SaveChangesAsync();
        }

        private int GoBackDaysToStartOfTheWeek(DateTime dt)
        {
            if (dt.DayOfWeek != 0) // retarded murican failsafe because their week starts with Sunday
                return (int)dt.DayOfWeek - 1;
            return 6;
        }

        public IEnumerable<RingFitReact> GetReactsForWeekIn(DateTime dt)
        {
            var goBackDays = GoBackDaysToStartOfTheWeek(dt);

            var weekStart = dt.AddDays(-goBackDays).Date;
            var followingWeekStart = weekStart.AddDays(7).Date;

            return _dbContext.RingFitReacts.AsQueryable().Where(x =>
                x.MessageTime >= weekStart && x.MessageTime < followingWeekStart);
        }

        public IEnumerable<RingFitReact> GetReactsForDay(DateTime dt)
        {
            var currentDayStart = dt.Date;
            var nextDayStart = dt.Date.AddDays(1);
            return _dbContext.RingFitReacts.AsQueryable().Where(x => x.MessageTime >= currentDayStart
                                                                     && x.MessageTime < nextDayStart);
        }

        public RingFitMessage GetDailyMessageFor(DateTime dt)
        {
            var currentDayStart = dt.Date;
            var nextDayStart = dt.Date.AddDays(1);

            return _dbContext.RingFitMessages.AsQueryable()
                .Where(x =>
                    x.MessageTime >= currentDayStart
                    && x.MessageTime < nextDayStart 
                    && x.MessageType == RingFitMessageType.Daily)
                .OrderByDescending(x => x.MessageTime)
                .First();
        }

        public async Task AddOrUpdateDailyMessage(RingFitMessage message)
        // basically just for message id tracking
        {
            if (message.MessageType != RingFitMessageType.Daily)
                throw new ArgumentException("Provided message is not of daily type");

            try
            {
                var currentMessage = GetDailyMessageFor(message.MessageTime);
                // message already exists
                currentMessage.MessageId = message.MessageId;
                _dbContext.RingFitMessages.Update(currentMessage);
            }
            catch (InvalidOperationException)
            {
                // message does not exist yet
                _dbContext.RingFitMessages.Add(message);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task InsertOrUpdateEmoteStatMatch(EmoteStatMatch esm)
        {
            var existingEsm = _dbContext.EmoteStatMatches.AsQueryable().Where(x => x.MessageId == esm.MessageId);
            if (existingEsm.Any())
            {
                var updatedEsm = existingEsm.First();
                updatedEsm.UpdateFrom(esm);

                _dbContext.EmoteStatMatches.Update(updatedEsm);
            }
            else
            {
                _dbContext.EmoteStatMatches.Add(esm);
            }
            await _dbContext.SaveChangesAsync();
        }

        public IEnumerable<EmoteStatMatch> GetEmoteStatMatchesForUserAndWeekIn(ulong userId, DateTime dt)
        {
            var goBackDays = GoBackDaysToStartOfTheWeek(dt);

            var weekStart = dt.AddDays(-goBackDays).Date;
            var followingWeekStart = weekStart.AddDays(7).Date;

            return _dbContext.EmoteStatMatches.AsQueryable().Where(x =>
                x.TimestampUtc >= weekStart.ToUniversalTime() &&
                x.TimestampUtc < followingWeekStart.ToUniversalTime() 
                && x.UserId == userId);
        }
    }
}