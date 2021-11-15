using System.Threading.Tasks;
using Quartz;

namespace pepega_bot.Module
{
    internal class WeeklyStatisticsJob : IJob
    {
        private readonly YukiiModule _ym;

        public WeeklyStatisticsJob(YukiiModule ym)
        {
            _ym = ym;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _ym.PostWeeklyStats();
        }
    }
}