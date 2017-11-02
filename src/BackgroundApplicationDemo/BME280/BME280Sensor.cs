using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace BackgroundApplicationDemo.BME280
{
    internal class BME280Sensor : IBME280Sensor
    {
        /// <summary>
        /// Memory address of each register
        /// </summary>
        private enum RegisterMemoryAddress : byte
        {
            /// <summary>
            /// Compensation data T1 (dig_T1) stored at memory address 0x88 
            /// </summary>
            CompensationT1 = 0x88,

            /// <summary>
            /// Compensation data T2 (dig_T2) stored at memory address 0x8A 
            /// </summary>
            CompensationT2 = 0x8A,

            /// <summary>
            /// Compensation data T3 (dig_T3) stored at memory address 0x8A 
            /// </summary>
            CompensationT3 = 0x8C,

            /// <summary>
            /// Chip Id stored at memory address 0xD0 
            /// </summary>
            RegisterChipId = 0xD0,

            /// <summary>
            /// The "ctrl_meas" register sets the pressure and temperature data acquisition options of the
            /// device. The register needs to be written after changing “ctrl_hum” for the changes to become effective.
            /// </summary>
            RegisterControl = 0xF4,

            /// <summary>
            /// Raw temperature data MSB (temp_msb) stored at memory address 0xFA
            /// </summary>
            TemperatureDataMSB = 0xFA,

            /// <summary>
            /// Raw temperature data LSB (temp_lsb) stored at memory address 0xFB 
            /// </summary>
            TemperatureDataLSB = 0xFB,

            /// <summary>
            /// Raw temperature data XLSB (temp_xlsb) stored at memory address 0xFC
            /// </summary>
            TemperatureDataXLSB = 0xFC
        }

        // The BME280 register addresses according the the datasheet: https://cdn.sparkfun.com/assets/learn_tutorials/4/1/9/BST-BME280_DS001-10.pdf
        private const byte DeviceAddress = 0x77;

        // The BME280 chip identification number created by manufacturer is 0x60
        private const byte ChipIdentificationNumber = 0x60;

        // String for the friendly name of the I2C bus
        private const string I2CControllerName = "I2C1";

        // Create an I2C device
        private I2cDevice _bme280Device;

        // Create new calibration data for the sensor
        private CalibrationData _calibrationData;

        // Variable to check if device is initialized
        private bool _isInitialized;

        // Method to initialize the BME280 sensor
        public async Task Initialize()
        {
            Debug.WriteLine("BME280::Initialize");

            try
            {
                // Instantiate the I2CConnectionSettings using the device address of the BME280
                var settings = new I2cConnectionSettings(DeviceAddress)
                {
                    //Set the I2C bus speed of connection to fast mode 400 kHz
                    BusSpeed = I2cBusSpeed.FastMode
                };

                // Use the I2CBus device selector to create an advanced query syntax string
                var aqs = I2cDevice.GetDeviceSelector(I2CControllerName);

                // Use the Windows.Devices.Enumeration.DeviceInformation class to create a collection using the advanced query syntax string
                var deviceInformation = await DeviceInformation.FindAllAsync(aqs);

                // Instantiate the the BME280 I2C device using the device id of the I2CBus and the I2CConnectionSettings
                _bme280Device = await I2cDevice.FromIdAsync(deviceInformation[0].Id, settings);

                // Check if device was found
                if (_bme280Device == null)
                {
                    Debug.WriteLine("Device not found");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Read temperature data from sensor
        /// </summary>
        /// <returns></returns>
        public async Task<float> ReadTemperature()
        {
            // Make sure the I2C device is initialized
            if (!_isInitialized)
            {
                await Begin();
            }

            /*
             * Int 32 bit
             * 0000 0000 0000 0000 0000 0000 0000 0000
             * 
             * Temperature: 20 bit
             *0000 0000 0000 1111 1111 1111 1111 1111

             * t_msb
             * 0000 0000 0000 0000 0000 0000 1111 1111
             * << 12
             * 0000 0000 0000 1111 1111 0000 0000 0000

             * t_lsb
             * 0000 0000 0000 0000 0000 0000 1111 1111
             * << 4
             * 0000 0000 0000 0000 0000 1111 1111 0000
             *
             * x_lsb
             * 0000 0000 0000 0000 0000 0000 1111 0000
             * >> 4
             * 0000 0000 0000 0000 0000 0000 0000 1111
            */

            // Read the MSB, LSB and bits 7:4 (XLSB) of the temperature from the BME280 registers
            byte temperatureDataMSB = Read8BitValueFromAssignedMemory((byte)RegisterMemoryAddress.TemperatureDataMSB);
            byte temperatureDataLSB = Read8BitValueFromAssignedMemory((byte)RegisterMemoryAddress.TemperatureDataLSB);
            byte temperatureDataXLSB = Read8BitValueFromAssignedMemory((byte)RegisterMemoryAddress.TemperatureDataXLSB);

            // Combine the values into a 32-bit integer
            int rawTemperature = (temperatureDataMSB << 12) + (temperatureDataLSB << 4) + (temperatureDataXLSB >> 4);

            // Convert the raw value to the temperature in degC
            double degreeC = ConvertRawTemperatureToDegreeC(rawTemperature);

            // Return the temperature as a float value
            return (float)degreeC;
        }

        private async Task Begin()
        {
            Debug.WriteLine("BME280::Begin");

            // A buffer that contains the data that you want to write to the I2 C device. 
            // This data should not include the bus address.
            byte[] writeBuffer = { (byte)RegisterMemoryAddress.RegisterChipId };

            // The buffer to which you want to read the data from the I2 C bus. 
            // The length of the buffer determines how much data to request from the device.
            byte[] readBuffer = { 0xFF };

            // Read the device signature
            _bme280Device.WriteRead(writeBuffer, readBuffer);
            Debug.WriteLine("BME280 Signature: " + readBuffer[0]);

            // Verify this is the BME280 which has chip number 0x60
            if (readBuffer[0] != ChipIdentificationNumber)
            {
                Debug.WriteLine("BME280::Begin Signature Mismatch.");
                return;
            }

            // Set the initialize variable to true
            _isInitialized = true;

            // Read the coefficients table
            _calibrationData = await ReadCalibrationData();

            // Write control register
            await WriteControlRegister();

            // Write humidity control register
            // await WriteControlRegisterHumidity();
        }

        //Method to write 0x03 to the humidity control register
        //private async Task WriteControlRegisterHumidity()
        //{
        //    byte[] WriteBuffer = new byte[] { (byte)RegisterMemoryAddress.BME280_REGISTER_CONTROLHUMID, 0x03 };
        //    _bme280Device.Write(WriteBuffer);
        //    await Task.Delay(1);
        //    return;
        //}

        /// <summary>
        /// To write 0x3F to the control register
        /// </summary>
        /// <returns></returns>
        private async Task WriteControlRegister()
        {
            byte[] writeBuffer = { (byte)RegisterMemoryAddress.RegisterControl, 0x3F };
            _bme280Device.Write(writeBuffer);

            await Task.Delay(1);
        }

        /// <summary>
        /// To read a 16-bit value from a memory address and return it in little endian format
        /// </summary>
        /// <param name="memoryAddress">The memory address</param>
        /// <returns>Data in little endian format</returns>
        private ushort Read16BitValueFromAssignedMemory(byte memoryAddress)
        {
            ushort value = 0;
            byte[] writeBuffer = { 0x00 };
            byte[] readBuffer = { 0x00, 0x00 };

            writeBuffer[0] = memoryAddress;

            _bme280Device.WriteRead(writeBuffer, readBuffer);
            int h = readBuffer[1] << 8;
            int l = readBuffer[0];
            value = (ushort)(h + l);

            return value;
        }

        /// <summary>
        /// To read an 8-bit value from a memory address
        /// </summary>
        /// <param name="memoryAddress"></param>
        /// <returns></returns>
        private byte Read8BitValueFromAssignedMemory(byte memoryAddress)
        {
            byte value = 0;
            byte[] writeBuffer = { 0x00 };
            byte[] readBuffer = { 0x00 };

            writeBuffer[0] = memoryAddress;

            _bme280Device.WriteRead(writeBuffer, readBuffer);
            value = readBuffer[0];

            return value;
        }

        /// <summary>
        /// To read the calibration data from the registers
        /// </summary>
        /// <returns></returns>
        private async Task<CalibrationData> ReadCalibrationData()
        {
            // 16 bit calibration data is stored as Little Endian, the helper method will do the byte swap.
            _calibrationData = new CalibrationData();

            // Read temperature calibration data
            _calibrationData.CompensationT1 = (ushort)Read16BitValueFromAssignedMemory((byte)RegisterMemoryAddress.CompensationT1);
            _calibrationData.CompensationT2 = (short)Read16BitValueFromAssignedMemory((byte)RegisterMemoryAddress.CompensationT2);
            _calibrationData.CompensationT3 = (short)Read16BitValueFromAssignedMemory((byte)RegisterMemoryAddress.CompensationT3);

            await Task.Delay(1);

            return _calibrationData;
        }

        /// <summary>
        /// To return the temperature in DegC. Resolution is 0.01 DegC. Output value of “5123” equals 51.23 DegC.
        /// </summary>
        /// <param name="rawTemperature"></param>
        /// <returns></returns>
        private double ConvertRawTemperatureToDegreeC(int rawTemperature)
        {
            //The temperature is calculated using the compensation formula in the BME280 datasheet
            //var1 = (((double)rawTemperature) / 16384.0 - ((double)_calibrationData.CompensationT1) / 1024.0) *((double)_calibrationData.CompensationT2);
            //var2 = ((((double)rawTemperature) / 131072.0 - ((double)_calibrationData.CompensationT1) / 8192.0) * (((double)rawTemperature) / 131072.0 - ((double)_calibrationData.CompensationT1) / 8192.0)) *((double)_calibrationData.CompensationT3);

            //The temperature is calculated using the compensation formula in the BME280 datasheet
            double var1 = ((rawTemperature / 16384.0) - (_calibrationData.CompensationT1 / 1024.0)) * _calibrationData.CompensationT2;
            double var2 = ((rawTemperature / 131072.0) - (_calibrationData.CompensationT1 / 8192.0)) * _calibrationData.CompensationT3;

            return (var1 + var2) / 5120.0;
        }
    }
}
