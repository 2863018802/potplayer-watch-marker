using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

internal sealed class Preferences
{
    public bool Notifications { get; set; }
    public Preferences() { Notifications = true; }
}

internal sealed class SettingsWindow : Form
{
    readonly Label activity;
    readonly Label detail;
    readonly CheckBox startup;
    readonly CheckBox notifications;
    readonly Label feedback;
    readonly Action<bool, bool> apply;
    readonly Timer feedbackTimer;
    internal readonly Button SaveButton;
    internal CheckBox StartupOption { get { return startup; } }
    internal CheckBox NotificationOption { get { return notifications; } }

    static Label TextLabel(string text, float size, Color color)
    {
        return new Label { Text = text, AutoSize = false, Font = new Font("Microsoft YaHei UI", size), ForeColor = color, BackColor = Color.Transparent };
    }

    internal SettingsWindow(string root, bool autoStart, bool notify, Icon icon, Action<bool, bool> save)
    {
        apply = save;
        Text = "追剧标记";
        Icon = icon;
        Font = new Font("Microsoft YaHei UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(870, 590);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(26, 27, 30);
        ForeColor = Color.FromArgb(225, 222, 215);
        var heading = TextLabel("设置", 23F, Color.FromArgb(236, 229, 214));
        heading.SetBounds(30, 24, 480, 48);
        Controls.Add(heading);
        var intro = TextLabel("关闭播放器后，自动标记最后观看的一集", 10F, Color.FromArgb(170, 163, 150));
        intro.SetBounds(32, 78, 630, 26);
        Controls.Add(intro);

        var status = new Panel { BackColor = Color.FromArgb(36, 37, 40) };
        status.SetBounds(30, 120, 640, 184);
        Controls.Add(status);
        var statusHeading = TextLabel("观看记录", 11F, Color.FromArgb(194, 157, 87));
        statusHeading.SetBounds(20, 16, 580, 28);
        status.Controls.Add(statusHeading);
        activity = TextLabel("等待 PotPlayer 播放", 12F, Color.FromArgb(236, 229, 214));
        activity.AutoEllipsis = true;
        activity.SetBounds(20, 51, 600, 51);
        status.Controls.Add(activity);
        detail = TextLabel("后台监控已启动", 9F, Color.FromArgb(170, 163, 150));
        detail.AutoEllipsis = true;
        detail.SetBounds(20, 112, 600, 57);
        status.Controls.Add(detail);

        var settings = new Panel { BackColor = Color.FromArgb(36, 37, 40) };
        settings.SetBounds(30, 318, 640, 130);
        Controls.Add(settings);
        startup = new CheckBox { Text = "开机自启动", Checked = autoStart, AutoSize = true, Location = new Point(20, 17) };
        notifications = new CheckBox { Text = "显示通知提醒", Checked = notify, AutoSize = true, Location = new Point(20, 61) };
        settings.Controls.Add(startup);
        settings.Controls.Add(notifications);
        var hint = TextLabel("包含标记成功与异常提醒", 9F, Color.FromArgb(170, 163, 150));
        hint.SetBounds(40, 92, 560, 24);
        settings.Controls.Add(hint);
        var folder = TextLabel("监控目录  " + root, 9F, Color.FromArgb(170, 163, 150));
        folder.AutoEllipsis = true;
        folder.SetBounds(32, 461, 634, 30);
        Controls.Add(folder);
        var open = new Button { Text = "打开视频目录", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 37, 40) };
        open.FlatAppearance.BorderColor = Color.FromArgb(75, 67, 51);
        open.SetBounds(30, 510, 142, 40);
        open.Click += (s, e) => { try { Process.Start("explorer.exe", root); } catch (Exception error) { MessageBox.Show(this, error.Message, "打开目录失败"); } };
        Controls.Add(open);
        SaveButton = new Button { Text = "保存设置", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(194, 157, 87), ForeColor = Color.FromArgb(24, 25, 28) };
        SaveButton.FlatAppearance.BorderSize = 0;
        SaveButton.SetBounds(528, 510, 142, 40);
        SaveButton.Click += (s, e) => SaveSettings();
        Controls.Add(SaveButton);
        feedback = TextLabel("关闭窗口后继续在托盘运行", 9F, Color.FromArgb(170, 163, 150));
        feedback.TextAlign = ContentAlignment.MiddleRight;
        feedback.SetBounds(180, 513, 334, 35);
        Controls.Add(feedback);
        feedbackTimer = new Timer { Interval = 4000 };
        feedbackTimer.Tick += (s, e) => { feedback.Text = "关闭窗口后继续在托盘运行"; feedbackTimer.Stop(); };
        startup.CheckedChanged += (s, e) => feedback.Text = "设置已更改，点击保存生效";
        notifications.CheckedChanged += (s, e) => feedback.Text = "设置已更改，点击保存生效";

        var settingsPage = new Panel { Bounds = new Rectangle(170, 0, 700, 590), BackColor = BackColor };
        foreach (Control control in Controls.Cast<Control>().ToArray()) settingsPage.Controls.Add(control);
        Controls.Add(settingsPage);
        var helpPage = new Panel { Bounds = settingsPage.Bounds, BackColor = BackColor, Visible = false };
        Controls.Add(helpPage);
        var helpHeading = TextLabel("使用说明", 23F, Color.FromArgb(216, 186, 126));
        helpHeading.SetBounds(30, 24, 600, 48);
        helpPage.Controls.Add(helpHeading);
        var help = new TextBox {
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(30, 31, 34),
            ForeColor = Color.FromArgb(225, 222, 215), Font = Font,
            Bounds = new Rectangle(30, 100, 640, 450),
            Text = "开始使用\n\n启动工具后，照常用 PotPlayer 打开 MP4。播放器内部自动下一集或手动换集，都可以记录。\n\n关闭播放器后\n\n工具会更新最后一集所在目录的箭头标记，并刷新已打开的对应文件夹。按名称排序即可找到观看位置，双击标记可打开视频。\n\n标记文件\n\n显示为“视频名称 看到这里”，实际格式为 .lnk 快捷方式。每个文件夹保留一个工具标记，请保留快捷方式描述中的归属标识。\n\n后台运行\n\n关闭此窗口后继续在托盘运行。双击托盘图标可重新打开。开机启动、通知提醒可在设置中调整并保存。退出工具后会停止记录。\n\n监控范围\n\n" + root + "\n程序监控所在文件夹的上一级目录及子目录中的 MP4。\n\n运行限制\n\n播放器标题需要显示 MP4 文件名。同名视频无法唯一定位时会提示。关闭通知后，异常仍可在设置页面查看。"
        };
        help.Text = help.Text.Replace("\n", "\r\n");
        helpPage.Controls.Add(help);
        var sidebar = new Panel { Bounds = new Rectangle(0, 0, 170, 590), BackColor = Color.FromArgb(20, 21, 23) };
        Controls.Add(sidebar);
        var brand = TextLabel("追剧标记", 15F, Color.FromArgb(216, 186, 126));
        brand.SetBounds(22, 40, 140, 40);
        sidebar.Controls.Add(brand);
        var settingsNav = new Button { Text = "设置", FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(14, 132, 142, 44) };
        var helpNav = new Button { Text = "使用说明", FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(14, 188, 142, 44) };
        foreach (Button button in new[] { settingsNav, helpNav })
        {
            button.FlatAppearance.BorderSize = 0;
            button.ForeColor = Color.FromArgb(216, 186, 126);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(12, 0, 0, 0);
            sidebar.Controls.Add(button);
        }
        Action<bool> navigate = showHelp => {
            settingsPage.Visible = !showHelp;
            helpPage.Visible = showHelp;
            settingsNav.BackColor = showHelp ? sidebar.BackColor : Color.FromArgb(48, 43, 34);
            helpNav.BackColor = showHelp ? Color.FromArgb(48, 43, 34) : sidebar.BackColor;
        };
        settingsNav.Click += (s, e) => navigate(false);
        helpNav.Click += (s, e) => navigate(true);
        navigate(false);
        var version = TextLabel("版本 0.4.0", 9F, Color.FromArgb(147, 140, 126));
        version.SetBounds(26, 538, 130, 26);
        sidebar.Controls.Add(version);
    }

    void SaveSettings()
    {
        try
        {
            apply(startup.Checked, notifications.Checked);
            feedback.Text = "设置已保存";
            feedbackTimer.Start();
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "设置保存失败"); }
    }

    internal void RefreshStatus(string title, string description)
    {
        if (activity.Text != title) activity.Text = title;
        if (detail.Text != description) detail.Text = description;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) feedbackTimer.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class Session
{
    public int Pid { get; set; }
    public long Started { get; set; }
    public string File { get; set; }
}

internal static class Program
{
    internal const string Suffix = " 看到这里";
    static readonly string Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
    static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PotWatchMarker");
    static readonly string State = Path.Combine(Data, "sessions.json");
    static readonly string MarkerIcon = Path.Combine(Data, "marker-arrow.ico");
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
    static List<Session> sessions = new List<Session>();
    static Dictionary<string, List<string>> index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    static DateTime indexed = DateTime.MinValue;
    static NotifyIcon tray;
    static string owner;
    static string lastError;
    static string current;
    static Preferences preferences = new Preferences();
    static SettingsWindow settingsWindow;
    static System.Threading.EventWaitHandle showRequested;
    static Icon appIcon;
    static bool playing;
    static string lastMarked;
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "PotWatchMarker";
    static System.Threading.Mutex mutex;
    delegate bool EnumWindow(IntPtr window, IntPtr argument);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindow callback, IntPtr argument);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int count);

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--self-test")) { SelfTest(); return; }
        Application.EnableVisualStyles();
        appIcon = CreateIcon();
        if (args.Contains("--refresh-test"))
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "refresh-test-result.txt"), RefreshExplorer(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')).ToString());
            appIcon.Dispose();
            return;
        }
        if (args.Contains("--preview") || args.Contains("--preview-help"))
        {
            using (var preview = new SettingsWindow(Root, false, true, appIcon, (a, n) => { }))
            {
                preview.RefreshStatus("正在记录 · 第 69 集", "识别到播放器内部切换的集数\n退出 PotPlayer 后更新文件夹标记");
                preview.Show();
                if (args.Contains("--preview-help")) preview.Controls.OfType<Panel>().SelectMany(panel => panel.Controls.OfType<Button>()).First(button => button.Text == "使用说明").PerformClick();
                Application.DoEvents();
                using (var bitmap = new Bitmap(preview.Width, preview.Height))
                { preview.DrawToBitmap(bitmap, new Rectangle(0, 0, preview.Width, preview.Height)); bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "界面预览.png")); }
            }
            return;
        }
        showRequested = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "Local\\PotWatchMarker.Show");
        bool created;
        mutex = new System.Threading.Mutex(true, "Local\\PotWatchMarker", out created);
        if (!created) { if (!args.Contains("--background")) showRequested.Set(); showRequested.Dispose(); mutex.Dispose(); return; }
        try
        {
            Directory.CreateDirectory(Data);
            InstallMarkerIcon();
            string preferencesPath = Path.Combine(Data, "settings.json");
            if (File.Exists(preferencesPath)) preferences = Json.Deserialize<Preferences>(File.ReadAllText(preferencesPath)) ?? new Preferences();
            string ownerPath = Path.Combine(Data, "owner.txt");
            if (!File.Exists(ownerPath)) File.WriteAllText(ownerPath, Guid.NewGuid().ToString("N"));
            owner = "PotWatchMarker/1 " + File.ReadAllText(ownerPath).Trim();
            if (File.Exists(State)) sessions = Json.Deserialize<List<Session>>(File.ReadAllText(State));
            tray = new NotifyIcon { Icon = appIcon, Text = "追剧标记 · 等待 PotPlayer", Visible = true };
            var menu = new ContextMenuStrip();
            menu.Items.Add("打开设置", null, (s, e) => ShowStatus());
            var notificationItem = new ToolStripMenuItem("通知提醒") { Checked = preferences.Notifications, CheckOnClick = true };
            notificationItem.Click += (s, e) =>
            {
                try { ApplyPreferences(IsAutoStart(), notificationItem.Checked); if (settingsWindow != null) { settingsWindow.Dispose(); settingsWindow = null; } }
                catch (Exception error) { notificationItem.Checked = preferences.Notifications; MessageBox.Show(error.Message, "设置保存失败"); }
            };
            menu.Items.Add(notificationItem);
            menu.Opening += (s, e) => notificationItem.Checked = preferences.Notifications;
            menu.Items.Add("打开视频目录", null, (s, e) => Process.Start("explorer.exe", Root));
            menu.Items.Add("退出工具", null, (s, e) => Application.Exit());
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += (s, e) => ShowStatus();
            BuildIndex();
            MigrateLegacyMarkers();
            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => Tick();
            Tick();
            timer.Start();
            if (!args.Contains("--background")) ShowStatus();
            Application.Run();
            timer.Dispose();
        }
        catch (Exception error) { MessageBox.Show(error.Message, "追剧标记启动失败"); }
        finally { if (settingsWindow != null) settingsWindow.Dispose(); if (tray != null) tray.Dispose(); appIcon.Dispose(); showRequested.Dispose(); mutex.ReleaseMutex(); mutex.Dispose(); }
    }

    static void ShowStatus()
    {
        if (settingsWindow == null || settingsWindow.IsDisposed)
            settingsWindow = new SettingsWindow(Root, IsAutoStart(), preferences.Notifications, appIcon, ApplyPreferences);
        RefreshStatus();
        settingsWindow.Show();
        if (settingsWindow.WindowState == FormWindowState.Minimized) settingsWindow.WindowState = FormWindowState.Normal;
        settingsWindow.Activate();
    }

    static bool IsAutoStart()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
            return key != null && string.Equals(key.GetValue(RunName) as string, StartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    static string StartupCommand() { return "\"" + Application.ExecutablePath + "\" --background"; }

    static void ApplyPreferences(bool autoStart, bool notifications)
    {
        var next = new Preferences { Notifications = notifications };
        string path = Path.Combine(Data, "settings.json");
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, Json.Serialize(next), Encoding.UTF8);
        using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            object previous = key.GetValue(RunName);
            if (autoStart) key.SetValue(RunName, StartupCommand());
            else key.DeleteValue(RunName, false);
            try
            {
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch
            {
                if (previous == null) key.DeleteValue(RunName, false);
                else key.SetValue(RunName, previous);
                throw;
            }
        }
        preferences = next;
    }

    static void RefreshStatus()
    {
        string title = playing ? "正在记录 · " + Path.GetFileName(current ?? "等待识别") : "等待 PotPlayer 播放";
        string description = lastError ?? (playing ? "关闭 PotPlayer 后自动更新标记\n" + Path.GetDirectoryName(current ?? Root) :
            lastMarked != null ? "最近标记\n" + lastMarked : "后台监控已启动");
        if (settingsWindow != null && !settingsWindow.IsDisposed) settingsWindow.RefreshStatus(title, description);
        string tip = "追剧标记 · " + (playing ? Path.GetFileName(current ?? "等待识别") : "等待播放");
        tray.Text = tip.Length > 63 ? tip.Substring(0, 60) + "…" : tip;
    }

    static Icon CreateIcon()
    {
        string asset = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "marker-arrow.ico");
        if (File.Exists(asset)) return new Icon(asset, 32, 32);
        using (var bitmap = new Bitmap(32, 32))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using (var blue = new SolidBrush(Color.FromArgb(194, 157, 87))) graphics.FillEllipse(blue, 0, 0, 31, 31);
            graphics.FillPolygon(Brushes.White, new[] { new Point(11, 7), new Point(22, 7), new Point(22, 25), new Point(16, 21), new Point(11, 25) });
            IntPtr handle = bitmap.GetHicon();
            try { using (var original = Icon.FromHandle(handle)) return (Icon)original.Clone(); }
            finally { DestroyIcon(handle); }
        }
    }
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);

    static void Notify(string title, string message, ToolTipIcon kind)
    {
        if (preferences.Notifications) tray.ShowBalloonTip(3000, title, message, kind);
    }

    static bool InsideRoot(string path)
    {
        return Path.GetFullPath(path).StartsWith(Root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
    }

    static void BuildIndex()
    {
        var replacement = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var folders = new Stack<string>();
        folders.Push(Root);
        while (folders.Count > 0)
        {
            string folder = folders.Pop();
            try
            {
                foreach (string child in Directory.GetDirectories(folder))
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) folders.Push(child);
                foreach (string file in Directory.GetFiles(folder, "*.mp4"))
                {
                    string name = Path.GetFileName(file);
                    if (!replacement.ContainsKey(name)) replacement[name] = new List<string>();
                    replacement[name].Add(file);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        index = replacement;
        indexed = DateTime.UtcNow;
    }

    static string WindowFile(int processId)
    {
        string result = null;
        EnumWindows((window, argument) =>
        {
            uint pid;
            GetWindowThreadProcessId(window, out pid);
            if (pid != processId) return true;
            var title = new StringBuilder(4096);
            GetWindowText(window, title, title.Capacity);
            string value = title.ToString();
            const string ending = " - PotPlayer";
            if (value.EndsWith(ending, StringComparison.OrdinalIgnoreCase))
            {
                string name = value.Substring(0, value.Length - ending.Length);
                if (name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) result = name;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }

    static string Resolve(string name, Process player)
    {
        if (Path.IsPathRooted(name) && InsideRoot(name) && File.Exists(name)) return name;
        List<string> candidates;
        if (!index.TryGetValue(name, out candidates)) { BuildIndex(); index.TryGetValue(name, out candidates); }
        if (candidates == null) return null;
        candidates = candidates.Where(File.Exists).ToList();
        if (candidates.Count == 1) return candidates[0];
        // Playlist entries provide paths even when playname has not been flushed yet.
        string playlist = Path.Combine(Path.GetDirectoryName(player.MainModule.FileName), "Playlist", player.ProcessName + ".dpl");
        if (File.Exists(playlist))
        {
            string[] paths = File.ReadAllLines(playlist).Where(line => line.Contains("*file*")).Select(line => line.Substring(line.IndexOf("*file*", StringComparison.Ordinal) + 6)).ToArray();
            var matches = candidates.Where(path => paths.Contains(path, StringComparer.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 1) return matches[0];
        }
        throw new IOException("无法唯一确定视频目录，暂缓更新标记\n" + name);
    }

    static void Save()
    {
        string temporary = State + ".tmp";
        File.WriteAllText(temporary, Json.Serialize(sessions), Encoding.UTF8);
        if (File.Exists(State)) File.Replace(temporary, State, null);
        else File.Move(temporary, State);
    }

    static void Tick()
    {
        try
        {
            if (showRequested.WaitOne(0)) ShowStatus();
            playing = false;
            if ((DateTime.UtcNow - indexed).TotalMinutes >= 2) BuildIndex();
            var live = new HashSet<string>();
            bool changed = false;
            foreach (Process player in Process.GetProcesses().Where(p => p.ProcessName == "PotPlayerMini64" || p.ProcessName == "PotPlayerMini" || p.ProcessName == "PotPlayer64" || p.ProcessName == "PotPlayer"))
            using (player)
            {
                long started;
                try { started = player.StartTime.ToUniversalTime().Ticks; }
                catch (InvalidOperationException) { continue; }
                live.Add(player.Id + "/" + started);
                var session = sessions.FirstOrDefault(item => item.Pid == player.Id && item.Started == started);
                string name = WindowFile(player.Id);
                if (name == null) continue;
                string path;
                try { path = Resolve(name, player); }
                catch (Exception error) { Report(error.Message); path = null; }
                if (session == null)
                {
                    session = new Session { Pid = player.Id, Started = started };
                    sessions.Add(session);
                    changed = true;
                }
                // Clear a previous episode when the new title cannot be resolved safely.
                if (session.File != path) { session.File = path; changed = true; }
                current = path ?? ("待识别 " + name);
                playing = true;
            }
            if (changed) Save();
            foreach (Session ended in sessions.Where(item => !live.Contains(item.Pid + "/" + item.Started)).ToList())
            {
                try
                {
                    if (ended.File != null) UpdateMarker(ended.File, owner);
                    lastError = null;
                    if (ended.File != null)
                    {
                        try { RefreshExplorer(Path.GetDirectoryName(ended.File)); }
                        catch (Exception error) { Report("标记已保存，刷新文件夹失败\n" + error.Message); }
                    }
                    sessions.Remove(ended);
                    Save();
                    if (ended.File != null) { lastMarked = ended.File; Notify("观看位置已记录", Path.GetFileNameWithoutExtension(ended.File), ToolTipIcon.Info); }
                }
                catch (Exception error) { Report(error.Message); }
            }
        }
        catch (Exception error) { Report(error.Message); }
        RefreshStatus();
    }

    static void Report(string message)
    {
        if (lastError == message) return;
        lastError = message;
        File.AppendAllText(Path.Combine(Data, "errors.log"), DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
        Notify("追剧标记需要注意", message, ToolTipIcon.Warning);
    }

    internal static bool Owned(string file, string identity)
    {
        if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) return false;
        if (file.EndsWith(Suffix + ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            try { return ReadShortcut(file, "Description") == identity; }
            catch (COMException) { return false; }
            catch (System.Reflection.TargetInvocationException) { return false; }
        }
        return new FileInfo(file).Length < 256 && File.ReadAllText(file, Encoding.UTF8) == identity;
    }

    static object ComProperty(object instance, string name, params object[] arguments)
    {
        return instance.GetType().InvokeMember(name,
            arguments.Length == 0 ? System.Reflection.BindingFlags.GetProperty : System.Reflection.BindingFlags.SetProperty,
            null, instance, arguments);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern void SHChangeNotify(uint eventId, uint flags, string first, IntPtr second);

    internal static int RefreshExplorer(string folder)
    {
        SHChangeNotify(0x1000, 0x5, folder, IntPtr.Zero);
        object shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
        object windows = null;
        int refreshed = 0;
        try
        {
            windows = shell.GetType().InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shell, null);
            int count = Convert.ToInt32(ComProperty(windows, "Count"));
            for (int i = 0; i < count; i++)
            {
                object window = null, document = null, currentFolder = null, item = null;
                try
                {
                    window = windows.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    document = ComProperty(window, "Document");
                    currentFolder = ComProperty(document, "Folder");
                    item = ComProperty(currentFolder, "Self");
                    string path = (string)ComProperty(item, "Path");
                    if (string.Equals(path.TrimEnd('\\'), folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    {
                        window.GetType().InvokeMember("Refresh", System.Reflection.BindingFlags.InvokeMethod, null, window, null);
                        refreshed++;
                    }
                }
                catch (COMException) { }
                catch (System.Reflection.TargetInvocationException) { }
                finally
                {
                    foreach (object resource in new[] { item, currentFolder, document, window })
                        if (resource != null && Marshal.IsComObject(resource)) Marshal.FinalReleaseComObject(resource);
                }
            }
        }
        finally { if (windows != null) Marshal.FinalReleaseComObject(windows); Marshal.FinalReleaseComObject(shell); }
        return refreshed;
    }

    static string ReadShortcut(string path, string property)
    {
        object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        object link = null;
        try
        {
            link = shell.GetType().InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { path });
            return (string)ComProperty(link, property);
        }
        finally { if (link != null) Marshal.FinalReleaseComObject(link); Marshal.FinalReleaseComObject(shell); }
    }

    static void WriteShortcut(string path, string video, string identity)
    {
        object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        object link = null;
        try
        {
            link = shell.GetType().InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { path });
            ComProperty(link, "TargetPath", video);
            ComProperty(link, "WorkingDirectory", Path.GetDirectoryName(video));
            ComProperty(link, "Description", identity);
            ComProperty(link, "IconLocation", MarkerIcon + ",0");
            link.GetType().InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, link, null);
        }
        finally { if (link != null) Marshal.FinalReleaseComObject(link); Marshal.FinalReleaseComObject(shell); }
    }

    static void InstallMarkerIcon()
    {
        Directory.CreateDirectory(Data);
        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "marker-arrow.ico"), MarkerIcon, true);
    }

    static void MigrateLegacyMarkers()
    {
        foreach (string folder in index.Values.SelectMany(paths => paths).Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                foreach (string marker in Directory.GetFiles(folder, "*" + Suffix).Where(file => Owned(file, owner)))
                {
                    string name = Path.GetFileName(marker);
                    string video = Path.Combine(folder, name.Substring(0, name.Length - Suffix.Length) + ".mp4");
                    if (File.Exists(video)) UpdateMarker(video, owner);
                }
            }
            catch (Exception error) { Report(error.Message); }
        }
    }

    internal static void UpdateMarker(string video, string identity)
    {
        if (!File.Exists(video)) throw new IOException("视频文件已移动，保留旧标记\n" + video);
        string folder = Path.GetDirectoryName(video);
        string target = Path.Combine(folder, Path.GetFileNameWithoutExtension(video) + Suffix + ".lnk");
        if ((File.Exists(target) && !Owned(target, identity)) || Directory.Exists(target))
            throw new IOException("标记名称已被其他文件占用\n" + target);
        var old = Directory.GetFiles(folder).Where(file =>
            (file.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase) || file.EndsWith(Suffix + ".lnk", StringComparison.OrdinalIgnoreCase)) && Owned(file, identity)).ToList();
        string temporary = Path.Combine(folder, ".watchmarker-" + Guid.NewGuid().ToString("N") + ".lnk");
        try
        {
            WriteShortcut(temporary, video, identity);
            if (File.Exists(target))
            {
                if (!Owned(target, identity)) throw new IOException("标记文件归属已变化\n" + target);
                File.Replace(temporary, target, null);
            }
            else File.Move(temporary, target);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        foreach (string file in old)
            if (!string.Equals(file, target, StringComparison.OrdinalIgnoreCase) && Owned(file, identity)) File.Delete(file);
    }

    static void SelfTest()
    {
        Application.EnableVisualStyles();
        InstallMarkerIcon();
        string folder = Path.Combine(Path.GetTempPath(), "PotWatchMarkerTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string a = Path.Combine(folder, "第01集.mp4"), b = Path.Combine(folder, "第02集.mp4");
        File.WriteAllText(a, ""); File.WriteAllText(b, "");
        string identity = "PotWatchMarker/1 test";
        UpdateMarker(a, identity); UpdateMarker(a, identity); UpdateMarker(b, identity);
        if (Directory.GetFiles(folder, "*" + Suffix + ".lnk").Length != 1 || !Owned(Path.Combine(folder, "第02集" + Suffix + ".lnk"), identity)) throw new Exception("重命名测试失败");
        string shortcut = Path.Combine(folder, "第02集" + Suffix + ".lnk");
        if (ReadShortcut(shortcut, "TargetPath") != b || !ReadShortcut(shortcut, "IconLocation").StartsWith(MarkerIcon, StringComparison.OrdinalIgnoreCase))
            throw new Exception("快捷方式目标或图标错误");
        File.WriteAllText(Path.Combine(folder, "用户文件" + Suffix), "personal");
        UpdateMarker(a, identity);
        if (File.ReadAllText(Path.Combine(folder, "用户文件" + Suffix)) != "personal") throw new Exception("用户文件保护失败");
        WriteShortcut(Path.Combine(folder, "第02集" + Suffix + ".lnk"), b, "personal");
        bool conflict = false;
        try { UpdateMarker(b, identity); } catch (IOException) { conflict = true; }
        if (!conflict || !Owned(Path.Combine(folder, "第01集" + Suffix + ".lnk"), identity)) throw new Exception("冲突保护失败");
        File.WriteAllText(Path.Combine(folder, "重复" + Suffix), identity);
        UpdateMarker(a, identity);
        if (Directory.GetFiles(folder).Count(file => Owned(file, identity)) != 1) throw new Exception("重复清理失败");
        bool settingsSaved = false;
        using (var icon = CreateIcon())
        using (var window = new SettingsWindow(folder, false, true, icon, (startup, notify) => settingsSaved = startup && !notify))
        {
            window.Show();
            window.StartupOption.Checked = true;
            window.NotificationOption.Checked = false;
            window.SaveButton.PerformClick();
            if (!settingsSaved) throw new Exception("设置界面保存失败");
        }
        if (Json.Deserialize<Preferences>(Json.Serialize(new Preferences { Notifications = false })).Notifications)
            throw new Exception("通知设置持久化失败");
        Directory.CreateDirectory(Data);
        string preferencesPath = Path.Combine(Data, "settings.json");
        byte[] previousSettings = File.Exists(preferencesPath) ? File.ReadAllBytes(preferencesPath) : null;
        using (var run = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            object previous = run.GetValue(RunName);
            RegistryValueKind kind = previous == null ? RegistryValueKind.String : run.GetValueKind(RunName);
            try
            {
                ApplyPreferences(true, false);
                if (!IsAutoStart() || Json.Deserialize<Preferences>(File.ReadAllText(preferencesPath)).Notifications)
                    throw new Exception("自启动启用或通知保存失败");
                ApplyPreferences(false, true);
                if (run.GetValue(RunName) != null || !Json.Deserialize<Preferences>(File.ReadAllText(preferencesPath)).Notifications)
                    throw new Exception("自启动关闭或通知保存失败");
            }
            finally
            {
                if (previous == null) run.DeleteValue(RunName, false);
                else run.SetValue(RunName, previous, kind);
                if (previousSettings == null) File.Delete(preferencesPath);
                else File.WriteAllBytes(preferencesPath, previousSettings);
            }
        }
        foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
        Directory.Delete(folder);
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "self-test-result.txt"), "PASS: repeat, rename, ownership, conflict, duplicate cleanup, settings UI, preferences roundtrip, startup enable/disable, notification persistence; original settings restored");
    }
}
