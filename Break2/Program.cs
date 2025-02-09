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

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FreeConsole();

    [STAThread]
    static void Main()
    {
        FreeConsole(); // ✅ Hide the console window
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        CreateTrayIcon(); // ✅ Add system tray icon
        Task.Run(ReminderLoop); // ✅ Run reminder loop in background
        Application.Run(); // ✅ Keep application running for tray icon
    }

    static async Task ReminderLoop()
    {
        while (_running)
        {
            await WaitUntilNextReminder(); // ✅ Uses await now
            if (!_running) break;
            ShowNotification($"The time is {DateTime.Now:HH:mm}. Good time for a break!");
            await Task.Delay(TimeSpan.FromMinutes(3)); // ✅ Keeps async behavior
            if (!_running) break;
            ShowNotification($"The time is {DateTime.Now:HH:mm}. Break time finished!");
        }
        ExitApp();
    }

    static async Task WaitUntilNextReminder()
    {
        while (_running)
        {
            var now = DateTime.Now;
            if (now.Minute == 0 || now.Minute == 30)
                return;
            await Task.Delay(10000); // ✅ Keeps async behavior
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
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "Break Reminder"
        };

        var contextMenu = new ContextMenuStrip();
        var exitItem = new ToolStripMenuItem("Exit", null, (sender, e) => ExitApp());
        contextMenu.Items.Add(exitItem);
        trayIcon.ContextMenuStrip = contextMenu;
        trayIcon.MouseClick += TrayIcon_MouseClick; // ✅ Handle right-click
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
        trayIcon.Visible = false;
        trayIcon.Dispose();
        Environment.Exit(0);
    }
}
