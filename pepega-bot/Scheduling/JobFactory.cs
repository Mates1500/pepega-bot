using System.ComponentModel.Design;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace pepega_bot.Scheduling
{
    public class JobFactory: IJobFactory
    {
        private readonly IServiceContainer _jobContainer;

        public JobFactory(IServiceContainer jobContainer)
        {
            _jobContainer = jobContainer;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _jobContainer.GetRequiredService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            
        }
    }
}