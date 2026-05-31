using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private static NotifyIcon trayIcon;
    private static bool _running = true;
    private static DateTime? lastTriggered = null;
    private static DateTime pauseUntil = DateTime.MinValue;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        CreateTrayIcon();

        Task.Run(ReminderLoop);

        Application.Run();
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

/*
    static async Task StartBreak()
    {
        ShowNotification($"The time is {DateTime.Now:HH:mm}. Good time for a break!");

        await Task.Delay(TimeSpan.FromMinutes(3));

        if (!_running)
            return;

        ShowNotification($"The time is {DateTime.Now:HH:mm}. Break time finished!");
    }
*/

    static async Task StartBreak()
    {
        ShowNotification("Break started. 3 minutes.");

        await Task.Delay(TimeSpan.FromMinutes(3));

        if (!_running)
            return;

        ShowNotification("Break finished.");
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

/*
    static void ShowNotification(string message)
    {
        MessageBox(IntPtr.Zero, message, "Break30", 0);
    }
*/
    static void ShowNotification(string message)
    {
        trayIcon.BalloonTipTitle = "Break30";
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
                ShowNotification("Break reminders paused for 1 hour.");
            });

        var pause2HoursItem = new ToolStripMenuItem(
            "Pause 2 Hours",
            null,
            (sender, e) =>
            {
                pauseUntil = DateTime.Now.AddHours(2);
                ShowNotification("Break reminders paused for 2 hours.");
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