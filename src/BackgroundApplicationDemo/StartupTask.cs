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

            // Create a new object for our sensor class
            BME280Sensor BME280;

            // Create a new object for our sensor class
            BME280 = new BME280Sensor();

            //Initialize the sensor
            await BME280.Initialize();

            //Initialize them to 0.
            float temp = 0;
            float pressure = 0;
            float altitude = 0;
            float humidity = 0;
            //Create a constant for pressure at sea level. 
            //This is based on your local sea level pressure (Unit: Hectopascal)
            const float seaLevelPressure = 1022.00f;
            //Read 10 samples of the data
            for (int i = 0; i < 10; i++)
            {
                temp = await BME280.ReadTemperature();
                //pressure = await BME280.ReadPreasure();
                //altitude = await BME280.ReadAltitude(seaLevelPressure);
                //humidity = await BME280.ReadHumidity();
                //Write the values to your debug console
                Debug.WriteLine("Temperature: " + temp.ToString() + " deg C");
                //Debug.WriteLine("Humidity: " + humidity.ToString() + " %");
                //Debug.WriteLine("Pressure: " + pressure.ToString() + " Pa");
                //Debug.WriteLine("Altitude: " + altitude.ToString() + " m");
                Debug.WriteLine("");
            }

            taskInstance.Canceled += TaskInstanceOnCanceled;

            _deferral.Complete();
            //await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
        }

        private void TaskInstanceOnCanceled(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason reason)
        {
            _deferral.Complete();
        }
    }
}
