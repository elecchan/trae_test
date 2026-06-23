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
            Size = new Size(200, 120);
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(8);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            _deviceLabel = new Label
            {
                Text = $"设备 #{DeviceId:D3}",
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _statusPanel = new Panel
            {
                Size = new Size(12, 12),
                BackColor = Color.Red
            };

            _statusLabel = new Label
            {
                Text = "离线",
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.Gray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            };

            Panel statusPanelContainer = new Panel { Dock = DockStyle.Fill };
            statusPanelContainer.Paint += (s, e) =>
            {
                _statusPanel.Location = new Point(0, (statusPanelContainer.Height - 12) / 2);
            };
            statusPanelContainer.Controls.Add(_statusPanel);
            statusPanelContainer.Controls.Add(_statusLabel);
            _statusLabel.Location = new Point(18, 0);
            _statusLabel.Size = new Size(statusPanelContainer.Width - 18, statusPanelContainer.Height);

            _workStateLabel = new Label
            {
                Text = "工作状态: --",
                Font = new Font("Microsoft YaHei", 9),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _inputEnergyLabel = new Label
            {
                Text = "输入电量: -- KWh",
                Font = new Font("Microsoft YaHei", 9),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _outputEnergyLabel = new Label
            {
                Text = "输出电量: -- KWh",
                Font = new Font("Microsoft YaHei", 9),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            layout.Controls.Add(_deviceLabel, 0, 0);
            layout.Controls.Add(statusPanelContainer, 1, 0);

            layout.Controls.Add(_workStateLabel, 0, 1);
            layout.SetColumnSpan(_workStateLabel, 2);

            layout.Controls.Add(_inputEnergyLabel, 0, 2);
            layout.SetColumnSpan(_inputEnergyLabel, 2);

            layout.Controls.Add(_outputEnergyLabel, 0, 3);
            layout.SetColumnSpan(_outputEnergyLabel, 2);

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