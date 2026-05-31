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

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FreeConsole();

    [STAThread]
    static void Main()
    {
        FreeConsole();

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
            await WaitUntilNextReminder();

            if (!_running)
                break;

            ShowNotification($"The time is {DateTime.Now:HH:mm}. Good time for a break!");

            await Task.Delay(TimeSpan.FromMinutes(3));

            if (!_running)
                break;

            ShowNotification($"The time is {DateTime.Now:HH:mm}. Break time finished!");
        }

        ExitApp();
    }

    static async Task WaitUntilNextReminder()
    {
        while (_running)
        {
            var now = DateTime.Now;

            bool isReminderTime =
                now.Minute == 0 ||
                now.Minute == 30;

            var slot = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                0);

            if (isReminderTime && lastTriggered != slot)
            {
                lastTriggered = slot;
                return;
            }

            await Task.Delay(10000);
        }
    }

    static void ShowNotification(string message)
    {
        MessageBox(IntPtr.Zero, message, "Break Reminder", 0);
    }

    static void CreateTrayIcon()
    {
        trayIcon = new NotifyIcon()
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Visible = true,
            Text = "Break Reminder"
        };


        var contextMenu = new ContextMenuStrip();

        var exitItem = new ToolStripMenuItem(
            "Exit",
            null,
            (sender, e) => ExitApp());

        contextMenu.Items.Add(exitItem);

        trayIcon.ContextMenuStrip = contextMenu;
        trayIcon.MouseClick += TrayIcon_MouseClick;
    }

    static void TrayIcon_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            trayIcon.ContextMenuStrip.Show(Cursor.Position);
        }
    }

    static void ExitApp()
    {
        _running = false;

        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        Environment.Exit(0);
    }
}