using System.Drawing;
using System.Windows.Forms;
using ModbusMonitor.Models;

namespace ModbusMonitor.Controls
{
    public class DeviceCard : UserControl
    {
        private readonly Label _deviceLabel;
        private readonly Label _statusLabel;
        private readonly Label _workStateLabel;
        private readonly Label _inputEnergyLabel;
        private readonly Label _outputEnergyLabel;
        private readonly Panel _statusPanel;

        public int DeviceId { get; }

        public DeviceCard(int deviceId)
        {
            DeviceId = deviceId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Size = new Size(180, 120);
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(8);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4
            };

            _deviceLabel = new Label
            {
                Text = $"设备 #{DeviceId:D3}",
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Left
            };

            _statusPanel = new Panel
            {
                Size = new Size(12, 12),
                BackColor = Color.Red,
                CornerRadius = 6,
                Anchor = AnchorStyles.Right
            };

            _statusLabel = new Label
            {
                Text = "离线",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Right
            };

            _workStateLabel = new Label
            {
                Text = "工作状态: --",
                Font = new Font("Microsoft YaHei", 9),
                Anchor = AnchorStyles.Left
            };

            _inputEnergyLabel = new Label
            {
                Text = "输入电量: -- KWh",
                Font = new Font("Microsoft YaHei", 9),
                Anchor = AnchorStyles.Left
            };

            _outputEnergyLabel = new Label
            {
                Text = "输出电量: -- KWh",
                Font = new Font("Microsoft YaHei", 9),
                Anchor = AnchorStyles.Left
            };

            layout.Controls.Add(_deviceLabel, 0, 0);

            Panel statusContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 0)
            };
            statusContainer.Controls.Add(_statusPanel);
            _statusPanel.Location = new Point(statusContainer.Width - 16 - 50, 0);
            statusContainer.Controls.Add(_statusLabel);
            _statusLabel.Location = new Point(statusContainer.Width - 45, 0);
            layout.Controls.Add(statusContainer, 1, 0);

            layout.Controls.Add(_workStateLabel, 0, 1);
            layout.Controls.Add(new Label(), 1, 1);

            layout.Controls.Add(_inputEnergyLabel, 0, 2);
            layout.Controls.Add(new Label(), 1, 2);

            layout.Controls.Add(_outputEnergyLabel, 0, 3);
            layout.Controls.Add(new Label(), 1, 3);

            Controls.Add(layout);
        }

        public void UpdateData(DeviceData data)
        {
            if (data == null) return;

            if (data.Status == DeviceStatus.Online)
            {
                _statusPanel.BackColor = Color.LimeGreen;
                _statusLabel.Text = "在线";
                _statusLabel.ForeColor = Color.LimeGreen;
                _workStateLabel.Text = $"工作状态: {(data.WorkState == WorkState.Running ? "运行中" : "停止")}";
                _inputEnergyLabel.Text = $"输入电量: {data.InputEnergy:F2} KWh";
                _outputEnergyLabel.Text = $"输出电量: {data.OutputEnergy:F2} KWh";
            }
            else
            {
                _statusPanel.BackColor = Color.Red;
                _statusLabel.Text = "离线";
                _statusLabel.ForeColor = Color.Red;
                _workStateLabel.Text = "工作状态: --";
                _inputEnergyLabel.Text = "输入电量: -- KWh";
                _outputEnergyLabel.Text = "输出电量: -- KWh";
            }
        }
    }
}