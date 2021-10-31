using System.Threading.Tasks;
using Quartz;

namespace pepega_bot.Module
{
    internal class DailyCleanJob : IJob
    {
        private readonly TobikExposerModule _tem;

        public DailyCleanJob(TobikExposerModule tem)
        {
            _tem = tem;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _tem.Clean();
        }
    }
}