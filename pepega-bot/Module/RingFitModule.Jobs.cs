using System.Threading.Tasks;
using Quartz;

namespace pepega_bot.Module
{
    internal class DailyPostJob : IJob
    {
        private readonly RingFitModule _rfm;

        public DailyPostJob(RingFitModule rfm)
        {
            _rfm = rfm;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _rfm.PostDailyMessage();
        }
    }

    internal class WeeklySummaryJob : IJob
    {
        private readonly RingFitModule _rfm;

        public WeeklySummaryJob(RingFitModule rfm)
        {
            _rfm = rfm;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _rfm.PostWeeklyStats();
        }
    }
}