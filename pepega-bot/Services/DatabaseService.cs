using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using pepega_bot.Database;
using pepega_bot.Database.RingFit;

namespace pepega_bot.Services
{
    internal class DatabaseService
    {
        private readonly PooledDbContextFactory<ResultDatabaseContext> _contextFactory;

        public DatabaseService(IConfigurationService config)
        {
            var options = new DbContextOptionsBuilder<ResultDatabaseContext>()
                .UseSqlite(config.SqliteDbConnectionString)
                .Options;

            using (var dbContext = new ResultDatabaseContext(options))
            {
                // just initialize it with config to ensure migrations run
                dbContext.InitializeDatabase(config);
            }

            _contextFactory = new PooledDbContextFactory<ResultDatabaseContext>(options, poolSize: 16);
        }

        public async Task InsertOrAddWordCountByOne(string wordValue)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            var currentResult = dbContext.WordEntries.AsQueryable().Where(x => x.Value == wordValue).ToList();
            if (currentResult.Count < 1)
            {
                dbContext.WordEntries.Add(new DbWordEntry {Value = wordValue, Count = 1});
            }
            else
            {
                var currentWord = currentResult[0];
                currentWord.Count += 1;
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task InsertOrUpdateRingFitReact(RingFitReact r)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            try
            {
                var currentResult = dbContext.RingFitReacts.Single(x => x.UserId == r.UserId
                                                                         && x.MessageId == r.MessageId);
                // already exists
                currentResult.MinuteValue = r.MinuteValue;
                currentResult.IsApproximateValue = r.IsApproximateValue;
                dbContext.RingFitReacts.Update(currentResult);
            }
            catch (InvalidOperationException)
            {
                dbContext.RingFitReacts.Add(r);
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<bool> RemoveRingFitReact(ulong reactUserId, ulong reactedMessageId)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            var results =
                dbContext.RingFitReacts.AsQueryable().Where(x => x.MessageId == reactedMessageId &&
                                                                  x.UserId == reactUserId)
                    .ToList();

            if (results.Count == 0)
                return false;

            dbContext.Remove(results[0]);
            await dbContext.SaveChangesAsync();
            return true;
        }

        private static int GoBackDaysToStartOfTheWeek(DateTime dt)
        {
            if (dt.DayOfWeek != 0) // retarded murican failsafe because their week starts with Sunday
                return (int)dt.DayOfWeek - 1;
            return 6;
        }

        public async Task<List<RingFitReact>> GetReactsForWeekIn(DateTime dt)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            var goBackDays = GoBackDaysToStartOfTheWeek(dt);

            var weekStart = dt.AddDays(-goBackDays).Date;
            var followingWeekStart = weekStart.AddDays(7).Date;

            return await dbContext.RingFitReacts.AsQueryable().Where(x =>
                x.MessageTime >= weekStart && x.MessageTime < followingWeekStart)
                .ToListAsync();
        }

        public async Task<List<RingFitReact>> GetReactsForDay(DateTime dt)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            var currentDayStart = dt.Date;
            var nextDayStart = dt.Date.AddDays(1);
            return await dbContext.RingFitReacts.AsQueryable().Where(x => x.MessageTime >= currentDayStart
                                                                          && x.MessageTime < nextDayStart)
                .ToListAsync();
        }

        public RingFitMessage GetDailyMessageFor(DateTime dt)
        {
            using var dbContext = _contextFactory.CreateDbContext();

            var currentDayStart = dt.Date;
            var nextDayStart = dt.Date.AddDays(1);

            return dbContext.RingFitMessages.AsQueryable()
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

            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            try
            {
                var currentMessage = GetDailyMessageFor(message.MessageTime);
                // message already exists
                currentMessage.MessageId = message.MessageId;
                dbContext.RingFitMessages.Update(currentMessage);
            }
            catch (InvalidOperationException)
            {
                // message does not exist yet
                dbContext.RingFitMessages.Add(message);
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task InsertOrUpdateEmoteStatMatch(EmoteStatMatch esm)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            var existingEsm = dbContext.EmoteStatMatches.AsQueryable().Where(x => x.MessageId == esm.MessageId);
            if (existingEsm.Any())
            {
                var updatedEsm = existingEsm.First();
                updatedEsm.UpdateFrom(esm);

                dbContext.EmoteStatMatches.Update(updatedEsm);
            }
            else
            {
                dbContext.EmoteStatMatches.Add(esm);
            }
            await dbContext.SaveChangesAsync();
        }

        public async Task<List<EmoteStatMatch>> GetEmoteStatMatchesForUserAndWeekIn(ulong userId, DateTime dt)
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();

            var goBackDays = GoBackDaysToStartOfTheWeek(dt);

            var weekStart = dt.AddDays(-goBackDays).Date;
            var followingWeekStart = weekStart.AddDays(7).Date;

            return await dbContext.EmoteStatMatches.AsQueryable().Where(x =>
                x.TimestampUtc >= weekStart.ToUniversalTime() &&
                x.TimestampUtc < followingWeekStart.ToUniversalTime() 
                && x.UserId == userId)
                .ToListAsync();
        }
    }
}