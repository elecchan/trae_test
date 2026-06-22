using System;
using System.IO.Ports;
using System.Threading;
using ModbusMonitor.Models;
using ModbusMonitor.Utils;

namespace ModbusMonitor.Services
{
    public class ModbusService
    {
        private SerialPort _serialPort;
        private bool _isOpen;
        private readonly object _lockObj = new object();

        public bool IsOpen => _isOpen;

        public bool Open(string portName, int baudRate)
        {
            lock (_lockObj)
            {
                if (_isOpen)
                {
                    Close();
                }

                try
                {
                    _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 100,
                        WriteTimeout = 100,
                        DtrEnable = true,
                        RtsEnable = true
                    };
                    _serialPort.Open();
                    _isOpen = true;
                    return true;
                }
                catch
                {
                    _isOpen = false;
                    return false;
                }
            }
        }

        public void Close()
        {
            lock (_lockObj)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort = null;
                _isOpen = false;
            }
        }

        public bool ReadDeviceData(int deviceId, out DeviceData data)
        {
            data = null;
            if (!_isOpen || _serialPort == null) return false;

            try
            {
                byte[] request = BuildReadRequest(deviceId);
                
                lock (_lockObj)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(request, 0, request.Length);
                    
                    Thread.Sleep(10);
                    
                    int bytesToRead = 15;
                    byte[] response = new byte[bytesToRead];
                    int totalRead = 0;
                    int timeout = 100;
                    DateTime startTime = DateTime.Now;

                    while (totalRead < bytesToRead && (DateTime.Now - startTime).TotalMilliseconds < timeout)
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            int read = _serialPort.Read(response, totalRead, bytesToRead - totalRead);
                            totalRead += read;
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }

                    if (totalRead < 8) return false;

                    if (!CRC16.Verify(response)) return false;

                    if (response[1] != 0x03) return false;

                    if (response[2] != 10) return false;

                    data = new DeviceData
                    {
                        DeviceId = deviceId,
                        WorkState = (WorkState)response[3],
                        InputEnergy = (decimal)((response[5] << 8 | response[4]) << 16 | (response[7] << 8 | response[6])) / 100,
                        OutputEnergy = (decimal)((response[9] << 8 | response[8]) << 16 | (response[11] << 8 | response[10])) / 100,
                        Status = DeviceStatus.Online,
                        FailCount = 0,
                        LastUpdateTime = DateTime.Now
                    };

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool WriteControl(int deviceId, bool startAging)
        {
            if (!_isOpen || _serialPort == null) return false;

            try
            {
                byte[] request = BuildWriteRequest(deviceId, startAging);
                
                lock (_lockObj)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(request, 0, request.Length);
                    
                    Thread.Sleep(10);
                    
                    int bytesToRead = 12;
                    byte[] response = new byte[bytesToRead];
                    int totalRead = 0;
                    int timeout = 100;
                    DateTime startTime = DateTime.Now;

                    while (totalRead < bytesToRead && (DateTime.Now - startTime).TotalMilliseconds < timeout)
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            int read = _serialPort.Read(response, totalRead, bytesToRead - totalRead);
                            totalRead += read;
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }

                    if (totalRead < 8) return false;

                    if (!CRC16.Verify(response)) return false;

                    return response[1] == 0x10;
                }
            }
            catch
            {
                return false;
            }
        }

        private byte[] BuildReadRequest(int deviceId)
        {
            byte[] request = new byte[8];
            request[0] = (byte)deviceId;
            request[1] = 0x03;
            request[2] = 0x00;
            request[3] = 0x02;
            request[4] = 0x00;
            request[5] = 0x05;

            ushort crc = CRC16.Calculate(request, 0, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)(crc >> 8);

            return request;
        }

        private byte[] BuildWriteRequest(int deviceId, bool startAging)
        {
            byte[] request = new byte[12];
            request[0] = (byte)deviceId;
            request[1] = 0x10;
            request[2] = 0x00;
            request[3] = 0x01;
            request[4] = 0x00;
            request[5] = 0x01;
            request[6] = 0x02;
            request[7] = (byte)(startAging ? 0x01 : 0x00);
            request[8] = 0x00;

            ushort crc = CRC16.Calculate(request, 0, 9);
            request[9] = (byte)(crc & 0xFF);
            request[10] = (byte)(crc >> 8);

            return request;
        }
    }
}