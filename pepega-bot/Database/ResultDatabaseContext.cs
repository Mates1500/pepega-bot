using Microsoft.EntityFrameworkCore;
using pepega_bot.Database;
using pepega_bot.Database.RingFit;
using pepega_bot.Services;
using System.IO;
using System;

namespace pepega_bot.Module
{
    internal class ResultDatabaseContext : DbContext
    {
        public DbSet<DbWordEntry> WordEntries { get; set; }

        public DbSet<RingFitReact> RingFitReacts { get; set; }

        public DbSet<RingFitMessage> RingFitMessages { get; set; }

        public DbSet<EmoteStatMatch> EmoteStatMatches { get; set; }

        private static bool _initialized = false;

        public ResultDatabaseContext(DbContextOptions options): base(options)
        {

        }


        public void InitializeDatabase(IConfigurationService config, bool runtime = true)
        {
            if (_initialized)
                return;

            if (config == null)
                throw new ArgumentNullException("Config can't be null while executing this method");

            if (runtime)
            {
                EnsureDatabaseExists(config);
            }
            Database.Migrate();
            _initialized = true;
        }

        private void EnsureDatabaseExists(IConfigurationService config)
        {
            // sort of a hack, for some reason using migrations without an empty DB existing first initializes the DB with the tables up without migrations and then errors out on migrations desync...
            var file = new FileInfo(config.Configuration["SqliteDbLocation"]);
            file.Directory?.Create();
            if (!File.Exists(file.FullName))
                using (File.Create(file.FullName))
                {
                }
        }
    }
}