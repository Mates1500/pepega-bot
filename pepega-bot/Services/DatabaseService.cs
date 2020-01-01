using System;
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
    }
}