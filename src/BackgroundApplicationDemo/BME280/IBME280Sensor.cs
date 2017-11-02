using System.Threading.Tasks;

namespace BackgroundApplicationDemo.BME280
{
    internal interface IBME280Sensor
    {
        Task Initialize();

        Task<float> ReadTemperature();
    }
}