using System.IO;
using Microsoft.EntityFrameworkCore;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    internal class ResultDatabaseContext : DbContext
    {
        private readonly IConfigurationService _config;

        public ResultDatabaseContext(IConfigurationService config, bool runtime=true)
        {
            _config = config;

            Initialize(runtime);
        }

        private void Initialize(bool runtime)
        {
            if (runtime)
            {
                EnsureDatabaseExists();
            }
            Database.Migrate();
        }

        private void EnsureDatabaseExists()
        {
            // sort of a hack, for some reason using migrations without an empty DB existing first initializes the DB with the tables up without migrations and then errors out on migrations desync...
            var file = new FileInfo(_config.Configuration["SqliteDbLocation"]);
            file.Directory?.Create();
            if (!File.Exists(file.FullName))
                using (File.Create(file.FullName))
                {
                }
        }

        public DbSet<DbWordEntry> WordEntries { get; set; }
        public DbSet<RingFitReact> RingFitReacts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite(_config.SqliteDbConnectionString);
        }
    }
}