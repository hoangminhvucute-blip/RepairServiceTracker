#nullable disable

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RepairServiceTracker
{
    public class RepairOrder
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string FaultDescription { get; set; } = "";
        public DateTime ReceivedDate { get; set; } = DateTime.Today;
        public decimal EstimatedCost { get; set; }
        public string Status { get; set; } = "Mới tiếp nhận";
        public string ImagePath { get; set; } = "";
    }

    public partial class Form1 : Form
    {
        private readonly BindingList<RepairOrder> orders = new BindingList<RepairOrder>();
        private readonly BindingList<RepairOrder> displayOrders = new BindingList<RepairOrder>();
        private readonly BindingSource bindingSource = new BindingSource();
        private readonly ErrorProvider errorProvider = new ErrorProvider();

        private readonly Color navy = Color.FromArgb(23, 39, 65);
        private readonly Color navy2 = Color.FromArgb(35, 56, 88);
        private readonly Color blue = Color.FromArgb(49, 112, 190);
        private readonly Color teal = Color.FromArgb(19, 153, 143);
        private readonly Color red = Color.FromArgb(211, 65, 73);
        private readonly Color orange = Color.FromArgb(231, 151, 38);
        private readonly Color page = Color.FromArgb(244, 247, 251);
        private readonly Color ink = Color.FromArgb(43, 57, 78);

        private readonly TextBox txtOrderId = new TextBox();
        private readonly TextBox txtCustomer = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly TextBox txtDescription = new TextBox();
        private readonly TextBox txtSearch = new TextBox();
        private readonly ComboBox cboDevice = new ComboBox();
        private readonly ComboBox cboStatus = new ComboBox();
        private readonly ComboBox cboFilter = new ComboBox();
        private readonly DateTimePicker dtpReceived = new DateTimePicker();
        private readonly NumericUpDown numCost = new NumericUpDown();
        private readonly PictureBox picDevice = new PictureBox();
        private readonly DataGridView dgvOrders = new DataGridView();

        private Label lblTotalValue = new Label();
        private Label lblNewValue = new Label();
        private Label lblWorkingValue = new Label();
        private Label lblDoneValue = new Label();
        private readonly ToolStripStatusLabel lblStatus = new ToolStripStatusLabel();
        private string selectedImagePath = "";
        private bool loadingRecord;

        public Form1()
        {
            InitializeComponent();
            Text = "Repair Service Tracker";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1120, 760);
            BackColor = page;
            Font = new Font("Segoe UI", 9F);

            errorProvider.ContainerControl = this;
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureControls();
            ConfigureGrid();
            bindingSource.DataSource = displayOrders;
            dgvOrders.DataSource = bindingSource;
            RefreshList();
        }

        // ======================== GIAO DIỆN ========================
        private void BuildInterface()
        {
            MenuStrip menu = new MenuStrip { BackColor = Color.White, Font = new Font("Segoe UI", 9F) };
            ToolStripMenuItem file = new ToolStripMenuItem("Tệp");
            ToolStripMenuItem export = new ToolStripMenuItem("Xuất danh sách đang hiển thị ra CSV");
            ToolStripMenuItem exit = new ToolStripMenuItem("Thoát");
            export.Click += ExportCsv_Click;
            exit.Click += (s, e) => Close();
            file.DropDownItems.Add(export);
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(exit);
            menu.Items.Add(file);
            MainMenuStrip = menu;

            StatusStrip status = new StatusStrip { BackColor = Color.White, SizingGrip = false };
            status.Items.Add(new ToolStripStatusLabel("REPAIR SERVICE TRACKER"));
            status.Items.Add(new ToolStripStatusLabel("  |  "));
            status.Items.Add(lblStatus);

            Panel root = new Panel { Dock = DockStyle.Fill, BackColor = page };
            Panel sidebar = BuildSidebar();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 188;

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 12, 18, 10),
                BackColor = page,
                ColumnCount = 1,
                RowCount = 3
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.Controls.Add(BuildHeader(), 0, 0);
            content.Controls.Add(BuildStatistics(), 0, 1);
            content.Controls.Add(BuildWorkspace(), 0, 2);
            root.Controls.Add(content);
            root.Controls.Add(sidebar);

            Controls.Add(root);
            Controls.Add(status);
            Controls.Add(menu);
            menu.BringToFront();
            status.BringToFront();
        }

        private Panel BuildSidebar()
        {
            Panel side = new Panel { BackColor = navy, Width = 188 };
            Label logo = new Label
            {
                Text = "REPAIR",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                Location = new Point(20, 22),
                AutoSize = true
            };
            Label sub = new Label
            {
                Text = "SERVICE MANAGEMENT",
                ForeColor = Color.FromArgb(155, 181, 211),
                Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
                Location = new Point(23, 66),
                AutoSize = true
            };
            Label menuTitle = new Label
            {
                Text = "MENU CHÍNH",
                ForeColor = Color.FromArgb(148, 166, 190),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(20, 125),
                AutoSize = true
            };
            Button overview = NavButton("▣   Tổng quan", true);
            overview.Location = new Point(8, 153);
            overview.Click += (s, e) => { txtSearch.Clear(); cboFilter.SelectedIndex = 0; };
            Button manage = NavButton("▤   Quản lý đơn sửa", false);
            manage.Location = new Point(8, 199);
            manage.Click += (s, e) => txtOrderId.Focus();
            Button report = NavButton("⇩   Xuất báo cáo", false);
            report.Location = new Point(8, 245);
            report.Click += ExportCsv_Click;
            Label footer = new Label
            {
                Text = "REPAIR SERVICE TRACKER\nTheo dõi tiến độ sửa chữa",
                ForeColor = Color.FromArgb(165, 183, 207),
                Font = new Font("Segoe UI", 8F),
                Dock = DockStyle.Bottom,
                Height = 58,
                TextAlign = ContentAlignment.MiddleCenter
            };
            side.Controls.AddRange(new Control[] { logo, sub, menuTitle, overview, manage, report, footer });
            return side;
        }

        private Button NavButton(string text, bool active)
        {
            return new Button
            {
                Text = text,
                Width = 172,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? navy2 : navy,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            }.Also(b => { b.FlatAppearance.BorderSize = active ? 1 : 0; b.FlatAppearance.BorderColor = Color.FromArgb(91, 125, 165); });
        }

        private Panel BuildHeader()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = page };
            Label title = new Label
            {
                Text = "Quản lý đơn sửa chữa thiết bị",
                AutoSize = true,
                Font = new Font("Segoe UI", 21F, FontStyle.Bold),
                ForeColor = navy,
                Location = new Point(2, 0)
            };
            Label subtitle = new Label
            {
                Text = "Tiếp nhận yêu cầu • Theo dõi tiến độ • Ước tính chi phí",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Location = new Point(5, 39)
            };
            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);
            return panel;
        }

        private Control BuildStatistics()
        {
            TableLayoutPanel cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = page,
                Padding = new Padding(0, 3, 0, 7)
            };
            for (int i = 0; i < 4; i++)
            {
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 10, 0) };
                Panel stripe = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = new[] { blue, orange, teal, red }[i] };
                Label caption = new Label
                {
                    Text = new[] { "TỔNG SỐ ĐƠN", "MỚI TIẾP NHẬN", "ĐANG SỬA / CHỜ LK", "HOÀN TẤT" }[i],
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Location = new Point(17, 12),
                    AutoSize = true
                };
                Label value = new Label
                {
                    Text = "0",
                    ForeColor = new[] { blue, orange, teal, red }[i],
                    Font = new Font("Segoe UI", 21F, FontStyle.Bold),
                    Location = new Point(17, 32),
                    AutoSize = true
                };
                if (i == 0) lblTotalValue.Text = "0";
                if (i == 1) lblNewValue.Text = "0";
                if (i == 2) lblWorkingValue.Text = "0";
                if (i == 3) lblDoneValue.Text = "0";
                if (i == 0) { lblTotalValue = value; }
                if (i == 1) { lblNewValue = value; }
                if (i == 2) { lblWorkingValue = value; }
                if (i == 3) { lblDoneValue = value; }
                card.Controls.Add(caption);
                card.Controls.Add(value);
                card.Controls.Add(stripe);
                cards.Controls.Add(card, i, 0);
            }
            return cards;
        }

        private Control BuildWorkspace()
        {
            TableLayoutPanel workspace = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = page,
                Padding = new Padding(0, 2, 0, 0)
            };
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));

            GroupBox input = new GroupBox
            {
                Text = "THÔNG TIN ĐƠN SỬA CHỮA",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = navy,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 10, 0)
            };
            input.Controls.Add(BuildInputPanel());

            GroupBox list = new GroupBox
            {
                Text = "DANH SÁCH ĐƠN TIẾP NHẬN",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = navy,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(10),
                Margin = new Padding(0)
            };
            list.Controls.Add(BuildListPanel());
            workspace.Controls.Add(input, 0, 0);
            workspace.Controls.Add(list, 1, 0);
            return workspace;
        }

        private Control BuildInputPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));

            TableLayoutPanel fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9,
                BackColor = Color.White,
                Padding = new Padding(2, 2, 8, 0),
                AutoScroll = true
            };
            for (int i = 0; i < 9; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 76 : 58));
            fields.Controls.Add(Field("Mã đơn sửa chữa", txtOrderId), 0, 0);
            fields.Controls.Add(Field("Tên khách hàng", txtCustomer), 0, 1);
            fields.Controls.Add(Field("Số điện thoại", txtPhone), 0, 2);
            fields.Controls.Add(Field("Loại thiết bị", cboDevice), 0, 3);
            fields.Controls.Add(Field("Ngày tiếp nhận", dtpReceived), 0, 4);
            fields.Controls.Add(Field("Mô tả lỗi", txtDescription), 0, 5);
            fields.Controls.Add(Field("Chi phí dự kiến (VNĐ)", numCost), 0, 6);
            fields.Controls.Add(Field("Trạng thái xử lý", cboStatus), 0, 7);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                WrapContents = true,
                Padding = new Padding(0, 8, 0, 0),
                BackColor = Color.White
            };
            Button add = ActionButton("＋ Thêm mới", teal, 105);
            Button update = ActionButton("✎ Cập nhật", blue, 105);
            Button delete = ActionButton("✕ Xóa", red, 82);
            Button clear = ActionButton("↻ Làm mới", Color.FromArgb(105, 122, 145), 100);
            add.Click += Add_Click;
            update.Click += Update_Click;
            delete.Click += Delete_Click;
            clear.Click += (s, e) => ClearInput();
            buttons.Controls.AddRange(new Control[] { add, update, delete, clear });
            fields.Controls.Add(buttons, 0, 8);

            TableLayoutPanel imagePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(2, 2, 0, 0)
            };
            imagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            imagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            imagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Label imageTitle = new Label { Text = "ẢNH THIẾT BỊ", Dock = DockStyle.Fill, ForeColor = ink, Font = new Font("Segoe UI", 8F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            picDevice.Dock = DockStyle.Fill;
            picDevice.BorderStyle = BorderStyle.FixedSingle;
            picDevice.BackColor = Color.FromArgb(248, 250, 253);
            picDevice.SizeMode = PictureBoxSizeMode.Zoom;
            picDevice.Image = null;
            Label emptyImage = new Label { Text = "Chưa chọn ảnh", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Silver };
            picDevice.Controls.Add(emptyImage);
            Button chooseImage = ActionButton("＋ Chọn ảnh", navy2, 105);
            chooseImage.Dock = DockStyle.Fill;
            chooseImage.Click += ChooseImage_Click;
            imagePanel.Controls.Add(imageTitle, 0, 0);
            imagePanel.Controls.Add(picDevice, 0, 1);
            imagePanel.Controls.Add(chooseImage, 0, 2);
            panel.Controls.Add(fields, 0, 0);
            panel.Controls.Add(imagePanel, 1, 0);
            return panel;
        }

        private Control Field(string caption, Control control)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 0, 2, 1) };
            Label label = new Label { Text = caption, Dock = DockStyle.Top, Height = 21, ForeColor = ink, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            control.Dock = DockStyle.Top;
            control.Height = 28;
            p.Controls.Add(control);
            p.Controls.Add(label);
            return p;
        }

        private Control BuildListPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White,
                Padding = new Padding(2, 2, 2, 0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label searchLabel = new Label { Text = "TÌM KIẾM VÀ LỌC", Dock = DockStyle.Fill, ForeColor = navy, Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            TableLayoutPanel filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(0, 2, 8, 2);
            txtSearch.PlaceholderText = "Mã đơn, tên khách hàng hoặc số điện thoại...";
            cboFilter.Dock = DockStyle.Fill;
            cboFilter.Margin = new Padding(0, 2, 0, 2);
            filters.Controls.Add(txtSearch, 0, 0);
            filters.Controls.Add(cboFilter, 1, 0);
            dgvOrders.Dock = DockStyle.Fill;
            panel.Controls.Add(searchLabel, 0, 0);
            panel.Controls.Add(filters, 0, 1);
            panel.Controls.Add(dgvOrders, 0, 2);
            return panel;
        }

        private Button ActionButton(string text, Color color, int width)
        {
            Button b = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(0, 0, 5, 5),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ======================== CẤU HÌNH ========================
        private void ConfigureControls()
        {
            txtOrderId.PlaceholderText = "VD: SC001";
            txtCustomer.PlaceholderText = "Nhập họ tên khách hàng";
            txtPhone.PlaceholderText = "Nhập số điện thoại";
            txtDescription.Multiline = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            txtDescription.PlaceholderText = "Mô tả tình trạng / lỗi thiết bị...";

            cboDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cboDevice.Items.AddRange(new object[] { "Laptop", "Máy tính để bàn", "Điện thoại", "Máy tính bảng", "Máy in", "Thiết bị khác" });
            if (cboDevice.Items.Count > 0) cboDevice.SelectedIndex = 0;
            cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStatus.Items.AddRange(new object[] { "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy" });
            cboStatus.SelectedIndex = 0;
            cboFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFilter.Items.AddRange(new object[] { "Tất cả trạng thái", "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy" });
            cboFilter.SelectedIndex = 0;

            dtpReceived.Format = DateTimePickerFormat.Custom;
            dtpReceived.CustomFormat = "dd/MM/yyyy";
            numCost.Minimum = 0;
            numCost.Maximum = 1000000000000M;
            numCost.ThousandsSeparator = true;
            numCost.DecimalPlaces = 0;

            txtSearch.TextChanged += (s, e) => RefreshList();
            cboFilter.SelectedIndexChanged += (s, e) => RefreshList();
            dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;
        }

        private void ConfigureGrid()
        {
            dgvOrders.AutoGenerateColumns = false;
            dgvOrders.AllowUserToAddRows = false;
            dgvOrders.AllowUserToDeleteRows = false;
            dgvOrders.ReadOnly = true;
            dgvOrders.MultiSelect = false;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOrders.RowHeadersVisible = false;
            dgvOrders.BackgroundColor = Color.White;
            dgvOrders.BorderStyle = BorderStyle.None;
            dgvOrders.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvOrders.EnableHeadersVisualStyles = false;
            dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = navy2;
            dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvOrders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvOrders.ColumnHeadersHeight = 38;
            dgvOrders.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F);
            dgvOrders.DefaultCellStyle.ForeColor = ink;
            dgvOrders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 232, 249);
            dgvOrders.DefaultCellStyle.SelectionForeColor = navy;
            dgvOrders.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
            dgvOrders.RowTemplate.Height = 32;

            AddColumn("OrderId", "Mã đơn", 82);
            AddColumn("CustomerName", "Khách hàng", 125);
            AddColumn("Phone", "Điện thoại", 105);
            AddColumn("DeviceType", "Thiết bị", 100);
            AddColumn("ReceivedDate", "Ngày nhận", 88, "dd/MM/yyyy");
            AddColumn("EstimatedCost", "Chi phí dự kiến", 110, "N0");
            AddColumn("Status", "Trạng thái", 105);
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void AddColumn(string property, string header, int width, string format = null)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                MinimumWidth = 65,
                FillWeight = width
            };
            if (!string.IsNullOrEmpty(format)) col.DefaultCellStyle.Format = format;
            dgvOrders.Columns.Add(col);
        }

        // ======================== XỬ LÝ DỮ LIỆU ========================
        private bool ValidateInput(RepairOrder current = null)
        {
            errorProvider.Clear();
            bool valid = true;
            string id = txtOrderId.Text.Trim();
            if (id.Length == 0)
            {
                errorProvider.SetError(txtOrderId, "Vui lòng nhập mã đơn.");
                valid = false;
            }
            else if (orders.Any(x => x.OrderId.Equals(id, StringComparison.OrdinalIgnoreCase) && x != current))
            {
                errorProvider.SetError(txtOrderId, "Mã đơn đã tồn tại.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtCustomer.Text))
            {
                errorProvider.SetError(txtCustomer, "Vui lòng nhập tên khách hàng.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                errorProvider.SetError(txtPhone, "Vui lòng nhập số điện thoại.");
                valid = false;
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(txtPhone.Text.Trim(), @"^(0|\+84)[0-9]{9,10}$"))
            {
                errorProvider.SetError(txtPhone, "Số điện thoại không hợp lệ (10–11 chữ số).");
                valid = false;
            }
            if (cboDevice.SelectedIndex < 0)
            {
                errorProvider.SetError(cboDevice, "Vui lòng chọn loại thiết bị.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                errorProvider.SetError(txtDescription, "Vui lòng mô tả lỗi thiết bị.");
                valid = false;
            }
            if (numCost.Value < 0)
            {
                errorProvider.SetError(numCost, "Chi phí phải là số không âm.");
                valid = false;
            }
            return valid;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;
            RepairOrder order = ReadInput();
            orders.Add(order);
            RefreshList();
            ClearInput();
            MessageBox.Show("Thêm đơn sửa chữa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Update_Click(object sender, EventArgs e)
        {
            RepairOrder selected = GetSelectedOrder();
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ValidateInput(selected)) return;
            RepairOrder updated = ReadInput();
            selected.OrderId = updated.OrderId;
            selected.CustomerName = updated.CustomerName;
            selected.Phone = updated.Phone;
            selected.DeviceType = updated.DeviceType;
            selected.FaultDescription = updated.FaultDescription;
            selected.ReceivedDate = updated.ReceivedDate;
            selected.EstimatedCost = updated.EstimatedCost;
            selected.Status = updated.Status;
            selected.ImagePath = updated.ImagePath;
            RefreshList();
            MessageBox.Show("Cập nhật đơn sửa chữa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            RepairOrder selected = GetSelectedOrder();
            if (selected == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Bạn có chắc chắn muốn xóa đơn " + selected.OrderId + "?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            orders.Remove(selected);
            ClearInput();
            RefreshList();
        }

        private RepairOrder ReadInput()
        {
            return new RepairOrder
            {
                OrderId = txtOrderId.Text.Trim(),
                CustomerName = txtCustomer.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                DeviceType = cboDevice.Text,
                FaultDescription = txtDescription.Text.Trim(),
                ReceivedDate = dtpReceived.Value.Date,
                EstimatedCost = numCost.Value,
                Status = cboStatus.Text,
                ImagePath = selectedImagePath
            };
        }

        private RepairOrder GetSelectedOrder()
        {
            if (dgvOrders.CurrentRow == null) return null;
            return dgvOrders.CurrentRow.DataBoundItem as RepairOrder;
        }

        private void DgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (loadingRecord) return;
            RepairOrder order = GetSelectedOrder();
            if (order == null) return;
            loadingRecord = true;
            txtOrderId.Text = order.OrderId;
            txtCustomer.Text = order.CustomerName;
            txtPhone.Text = order.Phone;
            cboDevice.SelectedItem = order.DeviceType;
            txtDescription.Text = order.FaultDescription;
            dtpReceived.Value = order.ReceivedDate;
            numCost.Value = Math.Min(numCost.Maximum, Math.Max(numCost.Minimum, order.EstimatedCost));
            cboStatus.SelectedItem = order.Status;
            selectedImagePath = order.ImagePath;
            LoadDeviceImage(selectedImagePath);
            loadingRecord = false;
        }

        private void ClearInput()
        {
            loadingRecord = true;
            errorProvider.Clear();
            txtOrderId.Clear();
            txtCustomer.Clear();
            txtPhone.Clear();
            txtDescription.Clear();
            if (cboDevice.Items.Count > 0) cboDevice.SelectedIndex = 0;
            if (cboStatus.Items.Count > 0) cboStatus.SelectedIndex = 0;
            dtpReceived.Value = DateTime.Today;
            numCost.Value = 0;
            selectedImagePath = "";
            LoadDeviceImage("");
            dgvOrders.ClearSelection();
            loadingRecord = false;
            txtOrderId.Focus();
        }

        private void RefreshList()
        {
            string keyword = txtSearch.Text.Trim();
            string status = cboFilter.SelectedItem?.ToString() ?? "Tất cả trạng thái";
            var filtered = orders.Where(x =>
                (status == "Tất cả trạng thái" || x.Status == status) &&
                (keyword.Length == 0 || x.OrderId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 x.CustomerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 x.Phone.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

            displayOrders.RaiseListChangedEvents = false;
            displayOrders.Clear();
            foreach (RepairOrder item in filtered) displayOrders.Add(item);
            displayOrders.RaiseListChangedEvents = true;
            displayOrders.ResetBindings();

            lblTotalValue.Text = orders.Count.ToString();
            lblNewValue.Text = orders.Count(x => x.Status == "Mới tiếp nhận").ToString();
            lblWorkingValue.Text = orders.Count(x => x.Status == "Đang sửa" || x.Status == "Chờ linh kiện").ToString();
            lblDoneValue.Text = orders.Count(x => x.Status == "Hoàn tất").ToString();
            decimal pendingCost = orders.Where(x => x.Status != "Hoàn tất" && x.Status != "Đã hủy").Sum(x => x.EstimatedCost);
            lblStatus.Text = $"Số đơn theo bộ lọc: {filtered.Count}  |  Tổng chi phí dự kiến chưa hoàn tất: {pendingCost:N0} VNĐ";
        }

        // ======================== ẢNH THIẾT BỊ ========================
        private void ChooseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Chọn ảnh thiết bị";
                dialog.Filter = "Tệp hình ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tất cả tệp|*.*";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                selectedImagePath = dialog.FileName;
                LoadDeviceImage(selectedImagePath);
            }
        }

        private void LoadDeviceImage(string path)
        {
            try
            {
                Image old = picDevice.Image;
                picDevice.Image = null;
                old?.Dispose();
                picDevice.Controls.Clear();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    using (Image source = Image.FromFile(path))
                        picDevice.Image = new Bitmap(source);
                }
                else
                {
                    Label empty = new Label { Text = "Chưa chọn ảnh", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Silver };
                    picDevice.Controls.Add(empty);
                }
            }
            catch
            {
                picDevice.Image = null;
                picDevice.Controls.Clear();
                picDevice.Controls.Add(new Label { Text = "Không thể tải ảnh", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = red });
            }
        }

        // ======================== XUẤT CSV ========================
        private void ExportCsv_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog save = new SaveFileDialog())
            {
                save.Title = "Xuất danh sách đơn sửa chữa";
                save.Filter = "CSV (*.csv)|*.csv";
                save.FileName = "DanhSachDonSuaChua.csv";
                if (save.ShowDialog() != DialogResult.OK) return;
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("MaDon,TenKhachHang,SoDienThoai,LoaiThietBi,MoTaLoi,NgayTiepNhan,ChiPhiDuKien,TrangThai,AnhThietBi");
                foreach (RepairOrder x in displayOrders)
                {
                    csv.AppendLine(string.Join(",", new[]
                    {
                        Csv(x.OrderId), Csv(x.CustomerName), Csv(x.Phone), Csv(x.DeviceType),
                        Csv(x.FaultDescription), Csv(x.ReceivedDate.ToString("dd/MM/yyyy")),
                        Csv(x.EstimatedCost.ToString("0")), Csv(x.Status), Csv(x.ImagePath)
                    }));
                }
                File.WriteAllText(save.FileName, "\uFEFF" + csv.ToString(), Encoding.UTF8);
                MessageBox.Show("Đã xuất " + displayOrders.Count + " đơn ra file CSV.", "Xuất dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }

    internal static class ControlExtensions
    {
        public static T Also<T>(this T control, Action<T> action) where T : Control
        {
            action(control);
            return control;
        }
    }
}
