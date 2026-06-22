using System;

namespace ModbusMonitor.Models
{
    public enum DeviceStatus
    {
        Offline,
        Online
    }

    public enum WorkState
    {
        Unknown,
        Stopped,
        Running
    }

    public class DeviceData
    {
        public int DeviceId { get; set; }
        public DeviceStatus Status { get; set; }
        public WorkState WorkState { get; set; }
        public decimal InputEnergy { get; set; }
        public decimal OutputEnergy { get; set; }
        public int FailCount { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public void ResetFailCount()
        {
            FailCount = 0;
        }

        public void IncrementFailCount()
        {
            FailCount++;
            if (FailCount >= 5)
            {
                Status = DeviceStatus.Offline;
            }
        }

        public void UpdateData(byte workStateValue, ushort inputEnergyHigh, ushort inputEnergyLow, ushort outputEnergyHigh, ushort outputEnergyLow)
        {
            WorkState = workStateValue switch
            {
                0 => WorkState.Stopped,
                1 => WorkState.Running,
                _ => WorkState.Unknown
            };

            InputEnergy = (decimal)(inputEnergyHigh << 16 | inputEnergyLow) / 100;
            OutputEnergy = (decimal)(outputEnergyHigh << 16 | outputEnergyLow) / 100;

            Status = DeviceStatus.Online;
            FailCount = 0;
            LastUpdateTime = DateTime.Now;
        }
    }
}