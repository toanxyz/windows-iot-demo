using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Devices.Tpm;

namespace BackgroundApplicationDemo
{
    internal class AzureIoTHub
    {
        private static DeviceClient _deviceClient;

        private static void CreateClient()
        {
            if (_deviceClient != null)
            {
                return;
            }

            var myDevice = new TpmDevice(0); // Use logical device 0 on the TPM
            var hubUri = myDevice.GetHostName();
            var deviceId = myDevice.GetDeviceId();
            var sasToken = myDevice.GetSASToken();

            _deviceClient = DeviceClient.Create(
                hubUri,
                AuthenticationMethodFactory.
                    CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Mqtt);
        }

        public static async Task SendDeviceToCloudMessageAsync()
        {
            CreateClient();

            var str = "Hello, Cloud from a C# app!";

            var message = new Message(Encoding.ASCII.GetBytes(str));

            await _deviceClient.SendEventAsync(message);
        }

        public static async Task<string> ReceiveCloudToDeviceMessageAsync()
        {
            CreateClient();

            while (true)
            {
                var receivedMessage = await _deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    await _deviceClient.CompleteAsync(receivedMessage);
                    return messageData;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
