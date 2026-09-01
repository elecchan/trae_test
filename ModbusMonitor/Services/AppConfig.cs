using System;
using System.IO;
using System.Windows.Forms;

namespace ModbusMonitor.Services
{
    /// <summary>
    /// 应用配置保存与加载
    /// </summary>
    public static class AppConfig
    {
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        /// <summary>
        /// 保存配置
        /// </summary>
        public static void Save(string portName, int baudRate, int deviceCount)
        {
            try
            {
                string[] lines =
                {
                    $"Port={portName}",
                    $"BaudRate={baudRate}",
                    $"DeviceCount={deviceCount}"
                };
                File.WriteAllLines(ConfigPath, lines);
            }
            catch
            {
                // 保存失败不影响程序运行
            }
        }

        /// <summary>
        /// 加载配置到下拉框
        /// </summary>
        public static void Load(ComboBox comboPort, ComboBox comboBaudRate, ComboBox comboDeviceCount)
        {
            if (!File.Exists(ConfigPath))
                return;

            try
            {
                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (key)
                    {
                        case "Port":
                            if (comboPort.Items.Contains(value))
                                comboPort.SelectedItem = value;
                            break;
                        case "BaudRate":
                            if (int.TryParse(value, out int baud) && comboBaudRate.Items.Contains(baud))
                                comboBaudRate.SelectedItem = baud;
                            break;
                        case "DeviceCount":
                            if (int.TryParse(value, out int count) && comboDeviceCount.Items.Contains(count))
                                comboDeviceCount.SelectedItem = count;
                            break;
                    }
                }
            }
            catch
            {
                // 加载失败使用默认值
            }
        }
    }
}
