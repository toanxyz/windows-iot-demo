namespace BackgroundApplicationDemo.BME280
{
    internal class CalibrationData
    {
        /// <summary>
        /// unsigned short ~ ushort ~ dig_T1
        /// </summary>
        public ushort CompensationT1 { get; set; }

        /// <summary>
        /// signed short ~ Int16 ~ short ~ dig_T2
        /// </summary>
        public short CompensationT2 { get; set; }

        /// <summary>
        /// signed short ~ Int16 ~ short ~ CompensationT3
        /// </summary>
        public short CompensationT3 { get; set; }
    }
}