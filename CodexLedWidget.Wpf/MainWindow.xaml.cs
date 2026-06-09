using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using CodexLedWidget.Core;
using WinForms = System.Windows.Forms;

namespace CodexLedWidget.Wpf;

public partial class MainWindow : Window
{
    private const double BaseWidth = 460;
    private const double BaseHeight = 292;
    private const int MinimumResizeWidth = 320;
    private const int MinimumResizeHeight = 203;
    private const double AspectRatio = BaseWidth / BaseHeight;

    private readonly CodexQuotaClient quotaClient = new();
    private readonly DispatcherTimer refreshTimer;
    private WinForms.NotifyIcon? trayIcon;
    private bool isTopmost = true;
    private bool isEnglish;
    private QuotaSnapshot? lastSnapshot;

    public MainWindow()
    {
        InitializeComponent();
        refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        refreshTimer.Tick += async (_, _) => await RefreshQuotaAsync();

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PlaceWindowTopRight();
        CreateTrayIcon();
        refreshTimer.Start();
        await RefreshQuotaAsync();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        trayIcon?.Dispose();
    }

    private async Task RefreshQuotaAsync()
    {
        SetLoading();

        try
        {
            lastSnapshot = await quotaClient.GetQuotaAsync();
            RenderQuota(lastSnapshot);
        }
        catch (Exception ex)
        {
            StateText.Text = isEnglish ? "Error" : "读取失败";
            StatusText.Text = ex.Message;
            RenderMeter(DualQuotaMeter.FromSnapshot(CreateEmptySnapshot(), CultureName));
            SetStateBrush(Colors.IndianRed);
        }
    }

    private void SetLoading()
    {
        StateText.Text = isEnglish ? "Reading" : "读取中";
        StatusText.Text = isEnglish ? "Reading Codex quota..." : "正在读取 Codex 额度...";
        SetStateBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
    }

    private void RenderQuota(QuotaSnapshot snapshot)
    {
        int remaining = snapshot.RemainingPercent ?? 0;
        RenderMeter(DualQuotaMeter.FromSnapshot(snapshot, CultureName));
        PrimaryLabel.Text = isEnglish ? "5h window" : "5小时窗口";
        SecondaryLabel.Text = isEnglish ? "7d window" : "7天窗口";
        PlanLabel.Text = isEnglish ? "Plan" : "计划";
        PrimaryText.Text = QuotaTextFormatter.FormatWindow(snapshot.Primary, CultureName);
        SecondaryText.Text = QuotaTextFormatter.FormatWindow(snapshot.Secondary, CultureName);
        PlanText.Text = QuotaTextFormatter.FormatPlan(snapshot.PlanType);
        StateText.Text = remaining <= 0 ? (isEnglish ? "Empty" : "耗尽") : remaining < 10 ? (isEnglish ? "Low" : "偏低") : (isEnglish ? "Ready" : "可用");
        StatusText.Text = $"{(isEnglish ? "Updated" : "已更新")} {DateTime.Now:HH:mm}";
        SetStateBrush(remaining <= 0 ? Colors.IndianRed : remaining < 10 ? System.Windows.Media.Color.FromRgb(241, 183, 47) : System.Windows.Media.Color.FromRgb(24, 182, 115));
    }

    private static QuotaSnapshot CreateEmptySnapshot()
    {
        return new QuotaSnapshot(
            LimitId: "codex",
            LimitName: "Codex",
            PlanType: "unknown",
            Primary: null,
            Secondary: null,
            FetchedAt: DateTimeOffset.Now);
    }

    private void RenderMeter(DualQuotaMeter meter)
    {
        PrimaryMeterLabel.Text = meter.Left.ShortLabel;
        PrimaryMeterPercent.Text = meter.Left.PercentText;
        SecondaryMeterLabel.Text = meter.Right.ShortLabel;
        SecondaryMeterPercent.Text = meter.Right.PercentText;
        PrimaryHalfFill.Height = FillHeight(meter.Left);
        SecondaryHalfFill.Height = FillHeight(meter.Right);
        PrimaryHalfFill.Background = FillBrush(meter.Left);
        SecondaryHalfFill.Background = FillBrush(meter.Right);
    }

    private static double FillHeight(DualQuotaMeterSegment segment)
    {
        if (!segment.HasData)
        {
            return 0;
        }

        return Math.Max(5, Math.Min(118, segment.RemainingPercent / 100.0 * 118));
    }

    private static System.Windows.Media.Brush FillBrush(DualQuotaMeterSegment segment)
    {
        System.Windows.Media.Color bottom = segment.RemainingPercent <= 0
            ? Colors.IndianRed
            : segment.RemainingPercent < 10
                ? System.Windows.Media.Color.FromRgb(241, 183, 47)
                : System.Windows.Media.Color.FromRgb(24, 182, 115);

        System.Windows.Media.Color top = System.Windows.Media.Color.FromArgb(184, bottom.R, bottom.G, bottom.B);
        System.Windows.Media.Color end = System.Windows.Media.Color.FromArgb(230, bottom.R, bottom.G, bottom.B);
        return new LinearGradientBrush(top, end, new System.Windows.Point(0, 0), new System.Windows.Point(0, 1));
    }

    private string CultureName => isEnglish ? "en-US" : "zh-CN";

    private void SetStateBrush(System.Windows.Media.Color color)
    {
        SolidColorBrush brush = new(color);
        TrafficLight.Fill = brush;
        StatusDot.Fill = brush;
    }

    private void PlaceWindowTopRight()
    {
        Rect area = SystemParameters.WorkArea;
        Left = area.Right - Width - 24;
        Top = area.Top + 24;
    }

    private void CreateTrayIcon()
    {
        trayIcon = new WinForms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "Codex LED Widget",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        trayIcon.Click += (_, _) => ToggleWindow();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        System.Windows.Resources.StreamResourceInfo? iconResource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/App.ico"));
        if (iconResource is null)
        {
            return System.Drawing.SystemIcons.Application;
        }

        return new System.Drawing.Icon(iconResource.Stream);
    }

    private WinForms.ContextMenuStrip BuildTrayMenu()
    {
        WinForms.ContextMenuStrip menu = new();
        menu.Items.Add("显示/隐藏", null, (_, _) => ToggleWindow());
        menu.Items.Add("刷新额度", null, async (_, _) => await RefreshQuotaAsync());
        menu.Items.Add(isTopmost ? "取消置顶" : "置顶", null, (_, _) => ToggleTopmost());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    private void ToggleWindow()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        Show();
        Activate();
    }

    private void ToggleTopmost()
    {
        isTopmost = !isTopmost;
        Topmost = isTopmost;
        PinButton.Opacity = isTopmost ? 1 : 0.6;
        PinButton.Foreground = new SolidColorBrush(isTopmost
            ? System.Windows.Media.Color.FromRgb(59, 130, 246)
            : System.Windows.Media.Color.FromRgb(82, 98, 115));
        if (trayIcon is not null)
        {
            trayIcon.ContextMenuStrip = BuildTrayMenu();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshQuotaAsync();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleTopmost();
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        isEnglish = !isEnglish;
        LanguageButton.Content = isEnglish ? "中" : "EN";
        if (lastSnapshot is not null)
        {
            RenderQuota(lastSnapshot);
        }
    }

    private void WidgetRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not System.Windows.Controls.Button)
        {
            DragMove();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int wmNcHitTest = 0x0084;
        const int wmSizing = 0x0214;

        if (msg == wmSizing)
        {
            KeepAspectRatioDuringSizing(wParam, lParam);
            handled = true;
            return new IntPtr(1);
        }

        if (msg != wmNcHitTest)
        {
            return IntPtr.Zero;
        }

        long packedPoint = lParam.ToInt64();
        System.Windows.Point point = PointFromScreen(new System.Windows.Point((short)(packedPoint & 0xFFFF), (short)((packedPoint >> 16) & 0xFFFF)));
        const double grip = 10;
        bool left = point.X <= grip;
        bool right = point.X >= ActualWidth - grip;
        bool top = point.Y <= grip;
        bool bottom = point.Y >= ActualHeight - grip;

        handled = left || right || top || bottom;
        if (!handled)
        {
            return IntPtr.Zero;
        }

        if (top && left) return new IntPtr(13);
        if (top && right) return new IntPtr(14);
        if (bottom && left) return new IntPtr(16);
        if (bottom && right) return new IntPtr(17);
        if (left) return new IntPtr(10);
        if (right) return new IntPtr(11);
        if (top) return new IntPtr(12);
        return new IntPtr(15);
    }

    private static void KeepAspectRatioDuringSizing(IntPtr edgeParameter, IntPtr rectanglePointer)
    {
        ResizeRectangle rectangle = Marshal.PtrToStructure<ResizeRectangle>(rectanglePointer);
        int edge = edgeParameter.ToInt32();
        int width = Math.Max(MinimumResizeWidth, rectangle.Right - rectangle.Left);
        int height = Math.Max(MinimumResizeHeight, rectangle.Bottom - rectangle.Top);

        const int left = 1;
        const int right = 2;
        const int top = 3;
        const int topLeft = 4;
        const int topRight = 5;
        const int bottom = 6;
        const int bottomLeft = 7;
        const int bottomRight = 8;

        if (edge is left or right)
        {
            height = (int)Math.Round(width / AspectRatio);
            ApplyHorizontalResize(edge, width, ref rectangle);
            rectangle.Bottom = rectangle.Top + Math.Max(MinimumResizeHeight, height);
        }
        else if (edge is top or bottom)
        {
            width = (int)Math.Round(height * AspectRatio);
            ApplyVerticalResize(edge, height, ref rectangle);
            rectangle.Right = rectangle.Left + Math.Max(MinimumResizeWidth, width);
        }
        else if (edge is topLeft or topRight or bottomLeft or bottomRight)
        {
            height = (int)Math.Round(width / AspectRatio);
            if (height < MinimumResizeHeight)
            {
                height = MinimumResizeHeight;
                width = (int)Math.Round(height * AspectRatio);
            }

            ApplyHorizontalResize(edge, width, ref rectangle);
            ApplyVerticalResize(edge, height, ref rectangle);
        }

        Marshal.StructureToPtr(rectangle, rectanglePointer, false);
    }

    private static void ApplyHorizontalResize(int edge, int width, ref ResizeRectangle rectangle)
    {
        if (edge is 1 or 4 or 7)
        {
            rectangle.Left = rectangle.Right - width;
            return;
        }

        rectangle.Right = rectangle.Left + width;
    }

    private static void ApplyVerticalResize(int edge, int height, ref ResizeRectangle rectangle)
    {
        if (edge is 3 or 4 or 5)
        {
            rectangle.Top = rectangle.Bottom - height;
            return;
        }

        rectangle.Bottom = rectangle.Top + height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ResizeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
