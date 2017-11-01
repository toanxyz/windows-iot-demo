using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            taskInstance.Canceled += TaskInstanceOnCanceled;

            await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
        }

        private void TaskInstanceOnCanceled(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason reason)
        {
            _deferral.Complete();
        }
    }
}
