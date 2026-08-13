using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Text.Json;

class Program
{
    private static NotifyIcon trayIcon;
    private static bool _running = true;
    private static DateTime? lastTriggered = null;
    private static DateTime pauseUntil = DateTime.MinValue;
    private static Settings settings = new Settings();
    private static Random random = new Random();

    private static int breakSessionId = 0;

    class Settings
    {
        public int breakLengthMinutes { get; set; } = 3;
        public int idleSecondsBeforeFullscreen { get; set; } = 10;
        public int watchForIdleSeconds { get; set; } = 60;
        public string[] notificationMessages { get; set; } =
        {
            "Stand up now.",
            "Time to move.",
            "Walk around.",
            "Stretch your legs.",
            "Take a short break."
        };
        public string[] breakScreenMessages { get; set; } =
        {
            "Increase energy by movement",
            "Activate your brain by movement",
            "Sedentary life drains\n.... Move ....",
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        LoadSettings();
        CreateTrayIcon();

        Task.Run(ReminderLoop);

        Application.Run();
    }

    static void LoadSettings()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "settings.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
    }

    static async Task ReminderLoop()
    {
        while (_running)
        {
            if (DateTime.Now < pauseUntil)
            {
                await Task.Delay(10000);
                continue;
            }

            await WaitUntilNextReminder();

            if (!_running)
                break;

            await StartBreak();
        }
    }


static async Task StartBreak()
{
    int mySession = ++breakSessionId;

    if (DateTime.Now < pauseUntil)
        return;

    string message = settings.notificationMessages[random.Next(settings.notificationMessages.Length)];
    ShowNotification(message);

    DateTime deadline = DateTime.Now.AddSeconds(settings.watchForIdleSeconds);

    while (DateTime.Now < deadline)
    {
        if (!_running)
            return;

        if (mySession != breakSessionId)
            return;

        if (DateTime.Now < pauseUntil)
            return;

        if (GetIdleTime() >= TimeSpan.FromSeconds(settings.idleSecondsBeforeFullscreen))
        {
            string breakScreenMessage = settings.breakScreenMessages[random.Next(settings.breakScreenMessages.Length)];
            ShowFullscreenBreakTimer(TimeSpan.FromMinutes(settings.breakLengthMinutes), breakScreenMessage);
            return;
        }

        await Task.Delay(1000);
    }
}



 
    static TimeSpan GetIdleTime()
    {
        LASTINPUTINFO info = new LASTINPUTINFO();
        info.cbSize = (uint)Marshal.SizeOf(info);

        GetLastInputInfo(ref info);

        uint idleTicks = ((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(idleTicks);
    }

    static async Task WaitUntilNextReminder()
    {
        while (_running)
        {
            var now = DateTime.Now;

            if (DateTime.Now < pauseUntil)
                return;

            bool isReminderTime = now.Minute == 0 || now.Minute == 30;

            var slot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            if (isReminderTime && lastTriggered != slot)
            {
                lastTriggered = slot;
                return;
            }

            await Task.Delay(10000);
        }
    }

    static void ShowFullscreenBreakTimer(TimeSpan duration, string message)
    {
        Form form = new Form();
        Label label = new Label();
        Label instruction = new Label();
        Label messageLabel = new Label();

        form.FormBorderStyle = FormBorderStyle.None;
        form.WindowState = FormWindowState.Maximized;
        form.BackColor = Color.Black;
        form.TopMost = true;
        form.ShowInTaskbar = false;
        form.KeyPreview = true;

        messageLabel.ForeColor = Color.MediumSpringGreen;
        messageLabel.BackColor = Color.Black;
        messageLabel.Font = new Font("Segoe UI", 32, FontStyle.Bold);
        messageLabel.Dock = DockStyle.Top;
        messageLabel.Height = 220;
        messageLabel.TextAlign = ContentAlignment.MiddleCenter;
        messageLabel.Text = message;

        label.ForeColor = Color.White;
        label.BackColor = Color.Black;
        label.Font = new Font("Segoe UI", 96, FontStyle.Bold);
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleCenter;

        instruction.ForeColor = Color.Gray;
        instruction.BackColor = Color.Black;
        instruction.Font = new Font("Segoe UI", 18, FontStyle.Regular);
        instruction.Dock = DockStyle.Bottom;
        instruction.Height = 80;
        instruction.TextAlign = ContentAlignment.MiddleCenter;
        instruction.Text = "Move mouse or press Esc to return";

        form.Controls.Add(label);
        form.Controls.Add(messageLabel);
        form.Controls.Add(instruction);

        DateTime endTime = DateTime.Now.Add(duration);
        Point startMousePosition = Cursor.Position;

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        timer.Interval = 250;

        timer.Tick += (sender, e) =>
        {
            if (Math.Abs(Cursor.Position.X - startMousePosition.X) > 8 ||
                Math.Abs(Cursor.Position.Y - startMousePosition.Y) > 8)
            {
                timer.Stop();
                form.Close();
                return;
            }

            TimeSpan remaining = endTime - DateTime.Now;

            if (remaining <= TimeSpan.Zero)
            {
                timer.Stop();
                form.Close();
                return;
            }

            label.Text = remaining.ToString(@"mm\:ss");
        };

        form.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                timer.Stop();
                form.Close();
            }
        };

        label.Text = duration.ToString(@"mm\:ss");
        timer.Start();
        form.ShowDialog();
    }

    static void ShowNotification(string message)
    {
        trayIcon.BalloonTipTitle = "";
        trayIcon.BalloonTipText = message;
        trayIcon.ShowBalloonTip(5000);
    }

    static void CreateTrayIcon()
    {
        trayIcon = new NotifyIcon()
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Visible = true,
            Text = "Break30"
        };

        var contextMenu = new ContextMenuStrip();

        var startBreakItem = new ToolStripMenuItem(
            "Start Break Now",
            null,
            async (sender, e) => await StartBreak());

        var pause1HourItem = new ToolStripMenuItem(
            "Pause 1 Hour",
            null,
            (sender, e) =>
            {
                pauseUntil = DateTime.Now.AddHours(1);
                breakSessionId++;
                ShowNotification("Paused for 1 hour.");

            });

        var pause2HoursItem = new ToolStripMenuItem(
            "Pause 2 Hours",
            null,
            (sender, e) =>
            {
                pauseUntil = DateTime.Now.AddHours(2);
                breakSessionId++;
                ShowNotification("Paused for 2 hours.");
            });

        var exitItem = new ToolStripMenuItem(
            "Exit",
            null,
            (sender, e) => ExitApp());

        contextMenu.Items.Add(startBreakItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(pause1HourItem);
        contextMenu.Items.Add(pause2HoursItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        trayIcon.ContextMenuStrip = contextMenu;
    }

    static void ExitApp()
    {
        _running = false;

        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        Application.Exit();
    }
}