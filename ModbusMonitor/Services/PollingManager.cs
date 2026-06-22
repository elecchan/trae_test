using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModbusMonitor.Models;

namespace ModbusMonitor.Services
{
    public delegate void DataUpdatedHandler(DeviceData data);

    public class PollingManager
    {
        private readonly ModbusService _modbusService;
        private readonly List<DeviceData> _devices = new List<DeviceData>();
        private Task _pollingTask;
        private CancellationTokenSource _cts;
        private int _deviceCount;
        private bool _isRunning;

        public event DataUpdatedHandler DataUpdated;

        public PollingManager(ModbusService modbusService)
        {
            _modbusService = modbusService;
        }

        public void Initialize(int deviceCount)
        {
            _deviceCount = deviceCount;
            _devices.Clear();
            for (int i = 1; i <= deviceCount; i++)
            {
                _devices.Add(new DeviceData
                {
                    DeviceId = i,
                    Status = DeviceStatus.Offline,
                    WorkState = WorkState.Unknown,
                    InputEnergy = 0,
                    OutputEnergy = 0,
                    FailCount = 0
                });
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _isRunning = true;

            _pollingTask = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    await PollAllDevices();
                    
                    int interval = _deviceCount switch
                    {
                        <= 10 => 200,
                        <= 50 => 500,
                        _ => 1000
                    };

                    await Task.Delay(interval, _cts.Token);
                }
            }, _cts.Token);
        }

        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async Task PollAllDevices()
        {
            foreach (var device in _devices)
            {
                if (_cts.IsCancellationRequested) break;

                DeviceData data;
                bool success = _modbusService.ReadDeviceData(device.DeviceId, out data);

                if (success && data != null)
                {
                    device.WorkState = data.WorkState;
                    device.InputEnergy = data.InputEnergy;
                    device.OutputEnergy = data.OutputEnergy;
                    device.Status = DeviceStatus.Online;
                    device.FailCount = 0;
                    device.LastUpdateTime = DateTime.Now;

                    DataUpdated?.Invoke(device);
                }
                else
                {
                    device.IncrementFailCount();
                    DataUpdated?.Invoke(device);
                }

                await Task.Delay(5, _cts.Token);
            }
        }

        public bool SendControlAll(bool startAging)
        {
            bool allSuccess = true;
            foreach (var device in _devices)
            {
                bool success = _modbusService.WriteControl(device.DeviceId, startAging);
                if (!success)
                {
                    allSuccess = false;
                }
                Thread.Sleep(10);
            }
            return allSuccess;
        }

        public DeviceData GetDeviceData(int deviceId)
        {
            return _devices.FirstOrDefault(d => d.DeviceId == deviceId);
        }

        public List<DeviceData> GetAllDevices()
        {
            return new List<DeviceData>(_devices);
        }

        public int OnlineCount => _devices.Count(d => d.Status == DeviceStatus.Online);
        public int OfflineCount => _devices.Count(d => d.Status == DeviceStatus.Offline);
    }
}