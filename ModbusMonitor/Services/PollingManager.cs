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

            var cts = new CancellationTokenSource();
            _cts = cts;
            _isRunning = true;

            _pollingTask = Task.Run(async () =>
            {
                var token = cts.Token;
                while (!token.IsCancellationRequested)
                {
                    await PollAllDevices(token);
                    
                    int interval = _deviceCount switch
                    {
                        <= 10 => 200,
                        <= 50 => 500,
                        _ => 1000
                    };

                    try
                    {
                        await Task.Delay(interval, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, cts.Token);
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            try
            {
                _cts?.Cancel();
                _pollingTask?.Wait(500);
            }
            catch { }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _pollingTask = null;
            }
        }

        private async Task PollAllDevices(CancellationToken token)
        {
            foreach (var device in _devices)
            {
                if (token.IsCancellationRequested) break;

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

                if (token.IsCancellationRequested) break;
                try
                {
                    await Task.Delay(5, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
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