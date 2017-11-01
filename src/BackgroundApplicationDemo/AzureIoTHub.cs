using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Devices.Tpm;

namespace BackgroundApplicationDemo
{
    internal class AzureIoTHub
    {
        private const string DeviceConnectionString = "HostName=toannguyen-iot-hub.azure-devices.net;DeviceId=minwinpc;SharedAccessKey=RKlg9S84DQxCYJXOODlE7ayDEtvNpjbEzPqfsKKz8rU=";

        public static async Task SendDeviceToCloudMessageAsync()
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);

            var str = "Hello, Cloud from a C# app!";

            var message = new Message(Encoding.ASCII.GetBytes(str));

            await deviceClient.SendEventAsync(message);
        }

        public static async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
            // TODO: Issue tracking https://github.com/Azure/azure-iot-hub-vs-cs/issues/9
            //try
            //{
            //    var myDevice = new TpmDevice(0);
            //    string hubUri = myDevice.GetHostName();
            //    string deviceId = myDevice.GetDeviceId();
            //    string sasToken = myDevice.GetSASToken();

            //    var deviceClient1 = DeviceClient.Create(
            //        hubUri,
            //        AuthenticationMethodFactory.
            //            CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Mqtt);
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e);
            //}


            var deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);

            while (true)
            {
                var receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    await deviceClient.CompleteAsync(receivedMessage);

                    Debug.WriteLine($"Hello message from the cloud {messageData}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
