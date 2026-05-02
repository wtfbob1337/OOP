namespace Lab10;

internal sealed class MainForm : Form
{
    private readonly TextBox inputBox = new();
    private readonly TextBox keyBox = new();
    private readonly TextBox rc2OutputBox = new();
    private readonly TextBox mdc2OutputBox = new();
    private readonly TextBox esignOutputBox = new();
    private readonly ProgressBar rc2Progress = new();
    private readonly ProgressBar mdc2Progress = new();
    private readonly ProgressBar esignProgress = new();
    private readonly ToolStripStatusLabel statusLabel = new();
    private readonly Button runAllButton = new();
    private readonly Button runRc2Button = new();
    private readonly Button runMdc2Button = new();
    private readonly Button runEsignButton = new();
    private readonly Button cancelButton = new();
    private CancellationTokenSource? cancellation;
    private bool busy;

    public MainForm()
    {
        Text = "Лабораторна робота №10 - варіант 10";
        MinimumSize = new Size(1160, 760);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        InitializeLayout();
    }

    private void InitializeLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.FromArgb(242, 245, 248)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        Controls.Add(root);

        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            BackColor = Color.FromArgb(242, 245, 248)
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 304));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(workspace, 0, 0);

        workspace.Controls.Add(CreateSidebar(), 0, 0);
        workspace.Controls.Add(CreateMainArea(), 1, 0);

        var status = new StatusStrip
        {
            Dock = DockStyle.Fill,
            SizingGrip = false,
            BackColor = Color.White
        };
        statusLabel.Text = "Готово";
        status.Items.Add(statusLabel);
        root.Controls.Add(status, 0, 1);
    }

    private Control CreateSidebar()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            ColumnCount = 1,
            Padding = new Padding(20, 18, 20, 18),
            BackColor = Color.FromArgb(23, 31, 41)
        };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Лабораторна\nробота №10",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        sidebar.Controls.Add(title, 0, 0);

        var variant = new Label
        {
            Text = "Варіант 10\nRC2 · MDC-2 · ESIGN",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(182, 201, 217),
            Font = new Font("Segoe UI", 11f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        sidebar.Controls.Add(variant, 0, 1);

        sidebar.Controls.Add(SideLabel("Повідомлення"), 0, 2);
        inputBox.Dock = DockStyle.Fill;
        inputBox.Multiline = true;
        inputBox.ScrollBars = ScrollBars.Vertical;
        inputBox.BorderStyle = BorderStyle.FixedSingle;
        inputBox.Text = "Текст для лабораторної роботи №10. Варіант 10 демонструє RC2, MDC-2 та ESIGN у паралельних задачах.";
        sidebar.Controls.Add(inputBox, 0, 3);

        sidebar.Controls.Add(SideLabel("Ключ RC2"), 0, 4);
        keyBox.Dock = DockStyle.Fill;
        keyBox.BorderStyle = BorderStyle.FixedSingle;
        keyBox.Text = "variant-10-rc2-key";
        sidebar.Controls.Add(keyBox, 0, 5);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        sidebar.Controls.Add(buttonPanel, 0, 7);

        PrepareButton(runAllButton, "Запустити всі", Color.FromArgb(36, 132, 255), Color.White);
        PrepareButton(runRc2Button, "Окремо RC2", Color.FromArgb(231, 239, 249), Color.FromArgb(22, 42, 62));
        PrepareButton(runMdc2Button, "Окремо MDC-2", Color.FromArgb(231, 239, 249), Color.FromArgb(22, 42, 62));
        PrepareButton(runEsignButton, "Окремо ESIGN", Color.FromArgb(231, 239, 249), Color.FromArgb(22, 42, 62));
        PrepareButton(cancelButton, "Скасувати", Color.FromArgb(94, 106, 119), Color.White);

        buttonPanel.Controls.Add(runAllButton, 0, 0);
        buttonPanel.Controls.Add(cancelButton, 0, 1);

        runAllButton.Click += async (_, _) => await RunAllAsync();
        runRc2Button.Click += async (_, _) => await RunSingleAsync(RunRc2Async);
        runMdc2Button.Click += async (_, _) => await RunSingleAsync(RunMdc2Async);
        runEsignButton.Click += async (_, _) => await RunSingleAsync(RunEsignAsync);
        cancelButton.Click += (_, _) => cancellation?.Cancel();
        cancelButton.Enabled = false;

        var note = new Label
        {
            Text = "Кожен метод запускається асинхронно, має власний прогрес і показує номер потоку виконання.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(139, 158, 174),
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.BottomLeft
        };
        sidebar.Controls.Add(note, 0, 8);

        return sidebar;
    }

    private Control CreateMainArea()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = Color.FromArgb(242, 245, 248)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(242, 245, 248)
        };
        main.Controls.Add(header, 0, 0);

        var heading = new Label
        {
            Text = "Криптографічний стенд",
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = Color.FromArgb(20, 29, 38),
            Font = new Font("Segoe UI Semibold", 21f, FontStyle.Bold)
        };
        header.Controls.Add(heading);

        var description = new Label
        {
            Text = "Порівняння трьох методів з варіанта 10: блокове шифрування RC2, хешування MDC-2 та електронний підпис ESIGN.",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(86, 100, 113),
            Font = new Font("Segoe UI", 10.5f)
        };
        header.Controls.Add(description);

        main.Controls.Add(MethodPanel("Метод 1", "RC2", "Шифрування та розшифрування повідомлення блоками по 64 біти", runRc2Button, rc2Progress, rc2OutputBox, Color.FromArgb(36, 132, 255)), 0, 1);
        main.Controls.Add(MethodPanel("Метод 2", "MDC-2", "Обчислення 128-бітного контрольного значення для повідомлення", runMdc2Button, mdc2Progress, mdc2OutputBox, Color.FromArgb(25, 166, 126)), 0, 2);
        main.Controls.Add(MethodPanel("Метод 3", "ESIGN", "Створення і перевірка цифрового підпису за хешем повідомлення", runEsignButton, esignProgress, esignOutputBox, Color.FromArgb(204, 112, 41)), 0, 3);

        return main;
    }

    private static Control MethodPanel(string tag, string title, string description, Button actionButton, ProgressBar progress, TextBox output, Color accent)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(16),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(layout);

        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(0, 0, 14, 0)
        };
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.Controls.Add(info, 0, 0);

        var tagLabel = new Label
        {
            Text = tag,
            Dock = DockStyle.Fill,
            ForeColor = accent,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        info.Controls.Add(tagLabel, 0, 0);

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(20, 29, 38),
            Font = new Font("Segoe UI Semibold", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        info.Controls.Add(titleLabel, 0, 1);

        var descriptionLabel = new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(86, 100, 113),
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.TopLeft
        };
        info.Controls.Add(descriptionLabel, 0, 2);

        actionButton.Margin = new Padding(0, 2, 0, 4);
        info.Controls.Add(actionButton, 0, 3);

        progress.Dock = DockStyle.Fill;
        progress.Margin = new Padding(0, 5, 0, 0);
        info.Controls.Add(progress, 0, 4);

        output.Dock = DockStyle.Fill;
        output.Multiline = true;
        output.ScrollBars = ScrollBars.Both;
        output.ReadOnly = true;
        output.WordWrap = false;
        output.BorderStyle = BorderStyle.FixedSingle;
        output.Font = new Font("Consolas", 9.5f);
        output.BackColor = Color.FromArgb(249, 251, 253);
        output.ForeColor = Color.FromArgb(21, 30, 39);
        layout.Controls.Add(output, 1, 0);

        return panel;
    }

    private static Label SideLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(202, 217, 229),
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static void PrepareButton(Button button, string text, Color backColor, Color foreColor)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 0, 6);
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
    }

    private async Task RunAllAsync()
    {
        if (busy)
        {
            return;
        }

        SetBusy(true);
        cancellation = new CancellationTokenSource();
        ResetOutput();
        statusLabel.Text = "Виконуються всі методи варіанта 10...";

        try
        {
            await Task.WhenAll(
                RunRc2Async(cancellation.Token),
                RunMdc2Async(cancellation.Token),
                RunEsignAsync(cancellation.Token));
            statusLabel.Text = "Усі методи завершено";
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Виконання скасовано";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Помилка: {ex.Message}";
        }
        finally
        {
            cancellation.Dispose();
            cancellation = null;
            SetBusy(false);
        }
    }

    private async Task RunSingleAsync(Func<CancellationToken, Task> action)
    {
        if (busy)
        {
            return;
        }

        SetBusy(true);
        cancellation = new CancellationTokenSource();
        ResetOutput();
        statusLabel.Text = "Виконується вибраний метод...";

        try
        {
            await action(cancellation.Token);
            statusLabel.Text = "Метод завершено";
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Виконання скасовано";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Помилка: {ex.Message}";
        }
        finally
        {
            cancellation.Dispose();
            cancellation = null;
            SetBusy(false);
        }
    }

    private async Task RunRc2Async(CancellationToken token)
    {
        var text = inputBox.Text;
        var key = keyBox.Text;
        rc2OutputBox.Text = "Виконується RC2...";
        var progress = new Progress<int>(value => rc2Progress.Value = Math.Clamp(value, 0, 100));
        var result = await Task.Run(() => CryptoHelpers.RunRc2(text, key, token, progress), token);
        rc2OutputBox.Text = FormatRc2(result);
    }

    private async Task RunMdc2Async(CancellationToken token)
    {
        var text = inputBox.Text;
        mdc2OutputBox.Text = "Виконується MDC-2...";
        var progress = new Progress<int>(value => mdc2Progress.Value = Math.Clamp(value, 0, 100));
        var result = await Task.Run(() => CryptoHelpers.RunMdc2(text, token, progress), token);
        mdc2OutputBox.Text = FormatMdc2(result);
    }

    private async Task RunEsignAsync(CancellationToken token)
    {
        var text = inputBox.Text;
        esignOutputBox.Text = "Виконується ESIGN...";
        var progress = new Progress<int>(value => esignProgress.Value = Math.Clamp(value, 0, 100));
        var result = await Task.Run(() => CryptoHelpers.RunEsign(text, token, progress), token);
        esignOutputBox.Text = FormatEsign(result);
    }

    private void SetBusy(bool value)
    {
        busy = value;
        runAllButton.Enabled = !value;
        runRc2Button.Enabled = !value;
        runMdc2Button.Enabled = !value;
        runEsignButton.Enabled = !value;
        cancelButton.Enabled = value;
        inputBox.Enabled = !value;
        keyBox.Enabled = !value;
        UseWaitCursor = value;
    }

    private void ResetOutput()
    {
        rc2Progress.Value = 0;
        mdc2Progress.Value = 0;
        esignProgress.Value = 0;
        rc2OutputBox.Clear();
        mdc2OutputBox.Clear();
        esignOutputBox.Clear();
    }

    private static string FormatRc2(Rc2Result result)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "RC2",
            $"Потік: {result.ThreadId}",
            $"Час: {result.Duration.TotalMilliseconds:F0} мс",
            $"Блоків: {result.Blocks}",
            $"Ключ: {SplitHex(result.KeyHex)}",
            "Шифротекст:",
            SplitHex(result.CipherHex),
            $"Відновлений текст: {result.PlainText}"
        });
    }

    private static string FormatMdc2(Mdc2Result result)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "MDC-2",
            $"Потік: {result.ThreadId}",
            $"Час: {result.Duration.TotalMilliseconds:F0} мс",
            $"Блоків: {result.Blocks}",
            $"Хеш: {result.HashHex}"
        });
    }

    private static string FormatEsign(EsignResult result)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "ESIGN",
            $"Потік: {result.ThreadId}",
            $"Час: {result.Duration.TotalMilliseconds:F0} мс",
            $"p: {result.P}",
            $"q: {result.Q}",
            $"n: {result.N}",
            $"e: {result.E}",
            $"SHA-256: {SplitHex(result.HashHex)}",
            $"Число повідомлення: {result.MessageNumber}",
            $"Підпис: {result.Signature}",
            $"Підпис підтверджено: {(result.Verified ? "так" : "ні")}"
        });
    }

    private static string SplitHex(string value)
    {
        const int line = 64;

        if (value.Length <= line)
        {
            return value;
        }

        var parts = new List<string>();

        for (var i = 0; i < value.Length; i += line)
        {
            parts.Add(value.Substring(i, Math.Min(line, value.Length - i)));
        }

        return string.Join(Environment.NewLine, parts);
    }
}
