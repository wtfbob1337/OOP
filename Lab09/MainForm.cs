using System.Globalization;

namespace Lab09;

internal sealed class MainForm : Form
{
    private readonly GraphCanvas canvas = new();
    private readonly NumericUpDown aBox = new();
    private readonly NumericUpDown bBox = new();
    private readonly NumericUpDown tMinBox = new();
    private readonly NumericUpDown tMaxBox = new();
    private readonly NumericUpDown stepBox = new();
    private readonly CheckBox fillAreaBox = new();
    private readonly CheckBox pointsBox = new();
    private readonly Label domainLabel = new();
    private readonly ToolStripStatusLabel statusLabel = new();
    private readonly DataGridView table = new();

    public MainForm()
    {
        Text = "Лабораторна робота №9 - графік варіанта 10";
        MinimumSize = new Size(1160, 730);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        InitializeLayout();
        BuildGraph();
    }

    private void InitializeLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.FromArgb(243, 239, 230)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(root);

        var side = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            BackColor = Color.FromArgb(35, 43, 41)
        };
        root.Controls.Add(side, 0, 0);

        var title = new Label
        {
            Text = "Parametric Studio",
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
            ForeColor = Color.White
        };
        side.Controls.Add(title);

        var formula = new Label
        {
            Text = "Варіант 10\r\nx = a cos(t)(cos(t)+b)\r\ny = sin(t)(sin(t)+b)",
            Dock = DockStyle.Top,
            Height = 96,
            ForeColor = Color.FromArgb(215, 225, 219)
        };
        side.Controls.Add(formula);

        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 8,
            Height = 324,
            Padding = new Padding(0, 10, 0, 8)
        };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        side.Controls.Add(controls);

        ConfigureNumber(aBox, -1000, 1000, 2, 3, 0.25m);
        ConfigureNumber(bBox, -1000, 1000, 1, 3, 0.25m);
        ConfigureNumber(tMinBox, -10000, 10000, 0, 3, 0.1m);
        ConfigureNumber(tMaxBox, -10000, 10000, 6.283m, 3, 0.1m);
        ConfigureNumber(stepBox, 0.001m, 1000, 0.01m, 3, 0.005m);
        AddRow(controls, 0, "a", aBox);
        AddRow(controls, 1, "b", bBox);
        AddRow(controls, 2, "t початкове", tMinBox);
        AddRow(controls, 3, "t кінцеве", tMaxBox);
        AddRow(controls, 4, "крок", stepBox);

        fillAreaBox.Text = "залити внутрішню область";
        fillAreaBox.Dock = DockStyle.Fill;
        fillAreaBox.Checked = true;
        fillAreaBox.ForeColor = Color.White;
        fillAreaBox.CheckedChanged += (_, _) => BuildGraph();
        controls.Controls.Add(fillAreaBox, 0, 5);
        controls.SetColumnSpan(fillAreaBox, 2);

        pointsBox.Text = "показати точки";
        pointsBox.Dock = DockStyle.Fill;
        pointsBox.ForeColor = Color.White;
        pointsBox.CheckedChanged += (_, _) => BuildGraph();
        controls.Controls.Add(pointsBox, 0, 6);
        controls.SetColumnSpan(pointsBox, 2);

        var buildButton = new Button
        {
            Text = "Побудувати",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(0, 124, 120),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        buildButton.FlatAppearance.BorderSize = 0;
        buildButton.Click += (_, _) => BuildGraph();
        controls.Controls.Add(buildButton, 0, 7);
        controls.SetColumnSpan(buildButton, 2);

        domainLabel.Dock = DockStyle.Top;
        domainLabel.Height = 74;
        domainLabel.ForeColor = Color.FromArgb(215, 225, 219);
        side.Controls.Add(domainLabel);

        var saveButton = new Button
        {
            Text = "Зберегти PNG",
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = Color.FromArgb(181, 78, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += (_, _) => SaveImage();
        side.Controls.Add(saveButton);

        table.Dock = DockStyle.Fill;
        table.AllowUserToAddRows = false;
        table.AllowUserToDeleteRows = false;
        table.ReadOnly = true;
        table.RowHeadersVisible = false;
        table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        table.BackgroundColor = Color.FromArgb(250, 248, 242);
        table.BorderStyle = BorderStyle.None;
        table.EnableHeadersVisualStyles = false;
        table.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(234, 226, 213);
        table.Columns.Add("t", "t");
        table.Columns.Add("x", "x");
        table.Columns.Add("y", "y");
        side.Controls.Add(table);

        canvas.Dock = DockStyle.Fill;
        canvas.Margin = new Padding(12);
        root.Controls.Add(canvas, 1, 0);

        var status = new StatusStrip
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(255, 253, 247)
        };
        status.Items.Add(statusLabel);
        root.Controls.Add(status, 0, 1);
        root.SetColumnSpan(status, 2);
    }

    private void ConfigureNumber(NumericUpDown box, decimal min, decimal max, decimal value, int decimals, decimal increment)
    {
        box.Minimum = min;
        box.Maximum = max;
        box.Value = value;
        box.DecimalPlaces = decimals;
        box.Increment = increment;
        box.Dock = DockStyle.Fill;
        box.ValueChanged += (_, _) => BuildGraph();
    }

    private static void AddRow(TableLayoutPanel panel, int row, string text, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White
        };
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(control, 1, row);
    }

    private void BuildGraph()
    {
        var settings = new GraphSettings
        {
            A = (double)aBox.Value,
            B = (double)bBox.Value,
            TMin = (double)tMinBox.Value,
            TMax = (double)tMaxBox.Value,
            Step = (double)stepBox.Value,
            FillArea = fillAreaBox.Checked,
            ShowPoints = pointsBox.Checked
        };

        canvas.Build(settings);
        UpdateDomain();
        FillTable();
        statusLabel.Text = $"Побудовано точок: {canvas.Points.Count}";
    }

    private void UpdateDomain()
    {
        domainLabel.Text = "Область визначення: усі дійсні t.\r\nРекомендований інтервал для повного контуру: 0 <= t <= 2π.";
    }

    private void FillTable()
    {
        table.Rows.Clear();

        foreach (var point in canvas.Points.Take(120))
        {
            table.Rows.Add(Format(point.T), Format(point.X), Format(point.Y));
        }
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private void SaveImage()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PNG зображення|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = "lab09_variant10.png"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        canvas.SaveImage(dialog.FileName);
        statusLabel.Text = $"Збережено: {dialog.FileName}";
    }
}
