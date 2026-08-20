using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using ModbusMonitor.Controls;
using ModbusMonitor.Models;
using ModbusMonitor.Services;

namespace ModbusMonitor
{
    public partial class MainForm : Form
    {
        private readonly ModbusService _modbusService = new ModbusService();
        private readonly PollingManager _pollingManager;
        private readonly List<DeviceCard> _deviceCards = new List<DeviceCard>();

        private ComboBox _comboBoxPort;
        private ComboBox _comboBoxBaudRate;
        private ComboBox _comboBoxDeviceCount;
        private Button _buttonConnect;
        private Button _buttonStartAging;
        private Button _buttonStopAging;
        private Label _labelStatus;
        private Panel _panelStatus;
        private Label _labelOnlineCount;
        private Label _labelOfflineCount;
        private FlowLayoutPanel _flowLayoutPanelDevices;

        public MainForm()
        {
            _pollingManager = new PollingManager(_modbusService);
            _pollingManager.DataUpdated += OnDataUpdated;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "微安老化柜控制系统";
            Size = new Size(1024, 768);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                RowStyles = { new RowStyle(SizeType.Absolute, 40), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 40) }
            };

            Panel topPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            FlowLayoutPanel topFlow = new FlowLayoutPanel { Dock = DockStyle.Fill };

            _comboBoxPort = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _comboBoxBaudRate = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _comboBoxDeviceCount = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            _buttonConnect = new Button { Text = "连接", Width = 80 };
            _panelStatus = new Panel { Size = new Size(12, 12), BackColor = Color.Red };
            _labelStatus = new Label { Text = "串口未打开", AutoSize = true, Margin = new Padding(5, 0, 0, 0) };
            LinkLabel _linkHelp = new LinkLabel
            {
                Text = "操作指导",
                AutoSize = true,
                Margin = new Padding(30, 10, 0, 0),
                LinkColor = Color.FromArgb(0, 102, 204),
                ActiveLinkColor = Color.FromArgb(0, 70, 150),
                VisitedLinkColor = Color.FromArgb(102, 102, 102)
            };
            _linkHelp.Click += (s, e) =>
            {
                string helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help.html");
                if (File.Exists(helpPath))
                {
                    Process.Start(new ProcessStartInfo(helpPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"未找到帮助文档：{helpPath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _comboBoxBaudRate.Items.AddRange(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 });
            _comboBoxBaudRate.SelectedIndex = 3;

            for (int i = 1; i <= 100; i++)
            {
                _comboBoxDeviceCount.Items.Add(i);
            }
            _comboBoxDeviceCount.SelectedIndex = 9;

            RefreshPorts();

            topFlow.Controls.Add(new Label { Text = "串口号:", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
            topFlow.Controls.Add(_comboBoxPort);
            topFlow.Controls.Add(new Label { Text = "波特率:", AutoSize = true, Margin = new Padding(10, 10, 5, 0) });
            topFlow.Controls.Add(_comboBoxBaudRate);
            topFlow.Controls.Add(new Label { Text = "设备数:", AutoSize = true, Margin = new Padding(10, 10, 5, 0) });
            topFlow.Controls.Add(_comboBoxDeviceCount);
            topFlow.Controls.Add(_buttonConnect);
            topFlow.Controls.Add(_panelStatus);
            topFlow.Controls.Add(_labelStatus);
            topFlow.Controls.Add(_linkHelp);

            topPanel.Controls.Add(topFlow);
            mainLayout.Controls.Add(topPanel, 0, 0);

            Panel middlePanel = new Panel { Dock = DockStyle.Fill };
            _flowLayoutPanelDevices = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = true
            };
            middlePanel.Controls.Add(_flowLayoutPanelDevices);
            mainLayout.Controls.Add(middlePanel, 0, 1);

            Panel bottomPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            FlowLayoutPanel bottomFlow = new FlowLayoutPanel { Dock = DockStyle.Fill };

            _buttonStartAging = new Button { Text = "开始老化", Width = 100, Enabled = false };
            _buttonStopAging = new Button { Text = "停止老化", Width = 100, Enabled = false };
            _labelOnlineCount = new Label { Text = "运行中: 0", AutoSize = true, Margin = new Padding(20, 10, 10, 0), ForeColor = Color.Green };
            _labelOfflineCount = new Label { Text = "离线: 0", AutoSize = true, Margin = new Padding(0, 10, 0, 0), ForeColor = Color.Red };

            bottomFlow.Controls.Add(_buttonStartAging);
            bottomFlow.Controls.Add(_buttonStopAging);
            bottomFlow.Controls.Add(_labelOnlineCount);
            bottomFlow.Controls.Add(_labelOfflineCount);

            bottomPanel.Controls.Add(bottomFlow);
            mainLayout.Controls.Add(bottomPanel, 0, 2);

            Controls.Add(mainLayout);

            _buttonConnect.Click += ButtonConnect_Click;
            _buttonStartAging.Click += ButtonStartAging_Click;
            _buttonStopAging.Click += ButtonStopAging_Click;
            _comboBoxPort.DropDown += ComboBoxPort_DropDown;

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;
        }

        private void RefreshPorts()
        {
            string currentPort = _comboBoxPort.SelectedItem?.ToString();
            _comboBoxPort.Items.Clear();
            foreach (string port in SerialPort.GetPortNames())
            {
                _comboBoxPort.Items.Add(port);
            }
            if (!string.IsNullOrEmpty(currentPort) && _comboBoxPort.Items.Contains(currentPort))
            {
                _comboBoxPort.SelectedItem = currentPort;
            }
            else if (_comboBoxPort.Items.Count > 0)
            {
                _comboBoxPort.SelectedIndex = 0;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CreateDeviceCards();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _pollingManager.Stop();
            _modbusService.Close();
        }

        private void CreateDeviceCards()
        {
            _flowLayoutPanelDevices.Controls.Clear();
            _deviceCards.Clear();

            int deviceCount = (int)_comboBoxDeviceCount.SelectedItem;
            for (int i = 1; i <= deviceCount; i++)
            {
                DeviceCard card = new DeviceCard(i);
                card.Margin = new Padding(5);
                _deviceCards.Add(card);
                _flowLayoutPanelDevices.Controls.Add(card);
            }
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (_modbusService.IsOpen)
            {
                _pollingManager.Stop();
                _modbusService.Close();
                UpdateConnectionStatus(false);
                return;
            }

            string portName = _comboBoxPort.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(portName))
            {
                MessageBox.Show("请选择串口号");
                return;
            }

            int baudRate = (int)_comboBoxBaudRate.SelectedItem;
            int deviceCount = (int)_comboBoxDeviceCount.SelectedItem;

            bool success = _modbusService.Open(portName, baudRate);
            if (success)
            {
                _pollingManager.Initialize(deviceCount);
                CreateDeviceCards();
                _pollingManager.Start();
                UpdateConnectionStatus(true);
            }
            else
            {
                MessageBox.Show("无法打开串口，请检查串口是否被占用");
                UpdateConnectionStatus(false);
            }
        }

        private void ButtonStartAging_Click(object sender, EventArgs e)
        {
            _pollingManager.SendControlAll(true);
        }

        private void ButtonStopAging_Click(object sender, EventArgs e)
        {
            _pollingManager.SendControlAll(false);
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (connected)
            {
                _buttonConnect.Text = "断开";
                _panelStatus.BackColor = Color.LimeGreen;
                _labelStatus.Text = "串口已打开";
                _labelStatus.ForeColor = Color.Green;
                _buttonStartAging.Enabled = true;
                _buttonStopAging.Enabled = true;
                _comboBoxPort.Enabled = false;
                _comboBoxBaudRate.Enabled = false;
                _comboBoxDeviceCount.Enabled = false;
            }
            else
            {
                _buttonConnect.Text = "连接";
                _panelStatus.BackColor = Color.Red;
                _labelStatus.Text = "串口未打开";
                _labelStatus.ForeColor = Color.Red;
                _buttonStartAging.Enabled = false;
                _buttonStopAging.Enabled = false;
                _comboBoxPort.Enabled = true;
                _comboBoxBaudRate.Enabled = true;
                _comboBoxDeviceCount.Enabled = true;
            }
        }

        private void OnDataUpdated(DeviceData data)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DeviceData>(OnDataUpdated), data);
                return;
            }

            DeviceCard card = _deviceCards.Find(c => c.DeviceId == data.DeviceId);
            if (card != null)
            {
                card.UpdateData(data);
            }

            _labelOnlineCount.Text = $"运行中: {_pollingManager.OnlineCount}";
            _labelOfflineCount.Text = $"离线: {_pollingManager.OfflineCount}";
        }

        private void ComboBoxPort_DropDown(object sender, EventArgs e)
        {
            RefreshPorts();
        }
    }
}