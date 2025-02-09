using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private static NotifyIcon trayIcon;
    private static bool _running = true;

    static async Task Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        HideConsoleWindow(); // Hide CMD window
        CreateTrayIcon(); // Add system tray icon
        Application.Run(); // Keep application running for tray icon
    }

    static async Task ReminderLoop()
    {
        while (_running)
        {
            await WaitUntilNextReminder();
            if (!_running) break;
            ShowNotification($"The time is {DateTime.Now:HH:mm}. Good time for a break!");
            await Task.Delay(TimeSpan.FromMinutes(3)); // 3-minute break
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
            await Task.Delay(10000); // Check every 10 seconds
        }
    }

    static void ShowNotification(string message)
    {
        MessageBox(IntPtr.Zero, message, "Break Reminder", 0);
    }

    static void HideConsoleWindow()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, 0); // Hide the console window
        }
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
        trayIcon.MouseClick += TrayIcon_MouseClick; // Handle right-click

        Task.Run(ReminderLoop); // Start reminder loop
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

    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
