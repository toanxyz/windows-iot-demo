using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace BackgroundApplicationDemo
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //var a = 5;
            //_deferral = taskInstance.GetDeferral();

            //taskInstance.Canceled += TaskInstanceOnCanceled;

            //Task.WaitAll(AzureIoTHub.SendDeviceToCloudMessageAsync());
        }

        private void TaskInstanceOnCanceled(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason reason)
        {
            //_deferral.Complete();
        }
    }
}
