using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private WinForms.ContextMenuStrip? floatingOrbMenu;
    private bool isTopmost = true;
    private bool isEnglish;
    private QuotaSnapshot? lastSnapshot;
    private QuotaDisplayAdjustment? displayAdjustment;
    private WidgetViewMode viewMode = WidgetViewMode.Panel;
    private Rect expandedBounds = Rect.Empty;
    private System.Windows.Point? floatingOrbMouseDownPoint;
    private bool floatingOrbWasDragged;
    private System.Windows.Point? nativeFloatingMouseDownPoint;
    private System.Windows.Point nativeFloatingWindowOrigin;
    private bool nativeFloatingWasDragged;

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
        floatingOrbMenu?.Dispose();
        trayIcon?.Dispose();
    }

    private async Task RefreshQuotaAsync()
    {
        displayAdjustment = null;
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
        QuotaSnapshot displaySnapshot = ApplyDisplayAdjustment(snapshot);
        int remaining = displaySnapshot.RemainingPercent ?? 0;
        RenderMeter(DualQuotaMeter.FromSnapshot(displaySnapshot, CultureName));
        PrimaryLabel.Text = isEnglish ? "5h window" : "5小时窗口";
        SecondaryLabel.Text = isEnglish ? "7d window" : "7天窗口";
        PlanLabel.Text = isEnglish ? "Plan" : "计划";
        PrimaryText.Text = QuotaTextFormatter.FormatWindow(displaySnapshot.Primary, CultureName);
        SecondaryText.Text = QuotaTextFormatter.FormatWindow(displaySnapshot.Secondary, CultureName);
        PlanText.Text = QuotaTextFormatter.FormatPlan(displaySnapshot.PlanType);
        StateText.Text = remaining <= 0 ? (isEnglish ? "Empty" : "耗尽") : remaining < 10 ? (isEnglish ? "Low" : "偏低") : (isEnglish ? "Ready" : "可用");
        StatusText.Text = $"{(isEnglish ? "Updated" : "已更新")} {DateTime.Now:HH:mm}";
        SetStateBrush(remaining <= 0 ? Colors.IndianRed : remaining < 10 ? System.Windows.Media.Color.FromRgb(241, 183, 47) : System.Windows.Media.Color.FromRgb(24, 182, 115));
    }

    private QuotaSnapshot ApplyDisplayAdjustment(QuotaSnapshot snapshot)
    {
        return displayAdjustment?.Apply(snapshot) ?? snapshot;
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
        PanelQuotaOrb.Render(meter);
        FloatingQuotaOrb.Render(meter);
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
        menu.Items.Add(viewMode == WidgetViewMode.Panel ? "收起悬浮球" : "展开面板", null, (_, _) => ToggleWidgetMode());
        menu.Items.Add("刷新额度", null, async (_, _) => await RefreshQuotaAsync());
        menu.Items.Add(isTopmost ? "取消置顶" : "置顶", null, (_, _) => ToggleTopmost());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    private WinForms.ContextMenuStrip BuildFloatingOrbMenu()
    {
        WinForms.ContextMenuStrip menu = new();
        menu.Items.Add(isEnglish ? "Expand panel" : "展开面板", null, (_, _) => Dispatcher.Invoke(ExpandPanel));
        menu.Items.Add(isEnglish ? "Refresh quota" : "刷新额度", null, async (_, _) => await Dispatcher.InvokeAsync(RefreshQuotaAsync));
        menu.Items.Add(isTopmost ? (isEnglish ? "Unpin" : "取消置顶") : (isEnglish ? "Pin on top" : "置顶"), null, (_, _) => Dispatcher.Invoke(ToggleTopmost));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(isEnglish ? "Exit" : "退出", null, (_, _) => Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()));
        return menu;
    }

    private void ShowFloatingOrbMenuAtCursor()
    {
        NativePoint point = GetCursorPoint();
        floatingOrbMenu?.Dispose();
        floatingOrbMenu = BuildFloatingOrbMenu();
        floatingOrbMenu.Show(new System.Drawing.Point(point.X, point.Y));
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

    private void ToggleWidgetMode()
    {
        if (viewMode == WidgetViewMode.Panel)
        {
            CollapseToFloatingOrb();
            return;
        }

        ExpandPanel();
    }

    private void CollapseToFloatingOrb()
    {
        if (viewMode == WidgetViewMode.FloatingOrb)
        {
            return;
        }

        expandedBounds = new Rect(Left, Top, Width, Height);
        WidgetWindowLayout layout = WidgetLayout.ForMode(WidgetViewMode.FloatingOrb);
        System.Windows.Point orbCenter = PanelQuotaOrb.TranslatePoint(new System.Windows.Point(PanelQuotaOrb.ActualWidth / 2, PanelQuotaOrb.ActualHeight / 2), this);

        viewMode = WidgetViewMode.FloatingOrb;
        PanelView.Visibility = Visibility.Collapsed;
        FloatingOrbView.Visibility = Visibility.Visible;
        MinWidth = layout.MinWidth;
        MinHeight = layout.MinHeight;
        MaxWidth = layout.Width;
        MaxHeight = layout.Height;
        ResizeMode = layout.CanResize ? ResizeMode.CanResize : ResizeMode.NoResize;
        Left += orbCenter.X - layout.Width / 2;
        Top += orbCenter.Y - layout.Height / 2;
        Width = layout.Width;
        Height = layout.Height;
        UpdateTrayMenu();
    }

    private void ExpandPanel()
    {
        if (viewMode == WidgetViewMode.Panel)
        {
            return;
        }

        WidgetWindowLayout layout = WidgetLayout.ForMode(WidgetViewMode.Panel);
        viewMode = WidgetViewMode.Panel;
        FloatingOrbView.Visibility = Visibility.Collapsed;
        PanelView.Visibility = Visibility.Visible;
        MinWidth = layout.MinWidth;
        MinHeight = layout.MinHeight;
        MaxWidth = double.PositiveInfinity;
        MaxHeight = double.PositiveInfinity;
        ResizeMode = layout.CanResize ? ResizeMode.CanResize : ResizeMode.NoResize;

        if (expandedBounds.IsEmpty)
        {
            Width = layout.Width;
            Height = layout.Height;
        }
        else
        {
            Left = expandedBounds.Left;
            Top = expandedBounds.Top;
            Width = expandedBounds.Width;
            Height = expandedBounds.Height;
        }

        UpdateTrayMenu();
    }

    private void UpdateTrayMenu()
    {
        if (trayIcon is not null)
        {
            trayIcon.ContextMenuStrip = BuildTrayMenu();
        }
    }

    private void ApplyFloatingOrbPenalty()
    {
        if (lastSnapshot is null)
        {
            PlayPenaltyAnimation();
            return;
        }

        displayAdjustment = (displayAdjustment ?? QuotaDisplayAdjustment.PrimaryRemainingDelta(0))
            .AddPrimaryRemainingDelta(-1);
        RenderQuota(lastSnapshot);
        PlayPenaltyAnimation();
    }

    private void PlayPenaltyAnimation()
    {
        PenaltyText.Visibility = Visibility.Visible;
        PenaltyText.Opacity = 1;
        PenaltyTextOffset.Y = 0;

        Duration duration = new(TimeSpan.FromMilliseconds(720));
        Storyboard storyboard = new();

        DoubleAnimation fade = new()
        {
            From = 1,
            To = 0,
            Duration = duration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, PenaltyText);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(fade);

        DoubleAnimation lift = new()
        {
            From = 0,
            To = -22,
            Duration = duration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(lift, PenaltyTextOffset);
        Storyboard.SetTargetProperty(lift, new PropertyPath(TranslateTransform.YProperty));
        storyboard.Children.Add(lift);
        storyboard.Completed += (_, _) => PenaltyText.Visibility = Visibility.Collapsed;
        storyboard.Begin(this, true);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshQuotaAsync();
    }

    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        CollapseToFloatingOrb();
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
        CollapseButton.ToolTip = isEnglish ? "Collapse to orb" : "收起为悬浮球";
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

    private void FloatingOrb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        floatingOrbMouseDownPoint = PointToScreen(e.GetPosition(this));
        floatingOrbWasDragged = false;
        Mouse.Capture((IInputElement)sender);
        e.Handled = true;
    }

    private void FloatingOrb_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (viewMode != WidgetViewMode.FloatingOrb || e.LeftButton != MouseButtonState.Pressed || floatingOrbMouseDownPoint is not System.Windows.Point startPoint)
        {
            return;
        }

        System.Windows.Point currentPoint = PointToScreen(e.GetPosition(this));
        if (!HasMovedBeyondDragThreshold(startPoint, currentPoint))
        {
            return;
        }

        floatingOrbWasDragged = true;
        floatingOrbMouseDownPoint = null;
        Mouse.Capture(null);
        DragMove();
        e.Handled = true;
    }

    private void FloatingOrb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        bool shouldApplyPenalty = floatingOrbMouseDownPoint.HasValue && !floatingOrbWasDragged;
        floatingOrbMouseDownPoint = null;
        floatingOrbWasDragged = false;
        Mouse.Capture(null);

        if (shouldApplyPenalty)
        {
            ApplyFloatingOrbPenalty();
        }

        e.Handled = true;
    }

    private void FloatingOrb_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ShowFloatingOrbMenuAtCursor();
        e.Handled = true;
    }

    private static bool HasMovedBeyondDragThreshold(System.Windows.Point startPoint, System.Windows.Point currentPoint)
    {
        return Math.Abs(currentPoint.X - startPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(currentPoint.Y - startPoint.Y) >= SystemParameters.MinimumVerticalDragDistance;
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

        if (viewMode == WidgetViewMode.FloatingOrb)
        {
            return HandleFloatingOrbWindowMessage(hwnd, msg, wParam, ref handled);
        }

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

    private IntPtr HandleFloatingOrbWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, ref bool handled)
    {
        const int wmNcHitTest = 0x0084;
        const int wmMouseMove = 0x0200;
        const int wmLButtonDown = 0x0201;
        const int wmLButtonUp = 0x0202;
        const int wmRButtonUp = 0x0205;
        const int htClient = 1;

        if (msg == wmNcHitTest)
        {
            handled = true;
            return new IntPtr(htClient);
        }

        if (msg == wmLButtonDown)
        {
            NativePoint point = GetCursorPoint();
            nativeFloatingMouseDownPoint = new System.Windows.Point(point.X, point.Y);
            nativeFloatingWindowOrigin = new System.Windows.Point(Left, Top);
            nativeFloatingWasDragged = false;
            SetCapture(hwnd);
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == wmMouseMove && nativeFloatingMouseDownPoint is System.Windows.Point startPoint && (wParam.ToInt32() & 1) == 1)
        {
            NativePoint point = GetCursorPoint();
            System.Windows.Vector delta = DeviceDeltaToDips(new System.Windows.Vector(point.X - startPoint.X, point.Y - startPoint.Y));
            if (!HasMovedBeyondDragThreshold(new System.Windows.Point(0, 0), new System.Windows.Point(delta.X, delta.Y)))
            {
                handled = true;
                return IntPtr.Zero;
            }

            nativeFloatingWasDragged = true;
            Left = nativeFloatingWindowOrigin.X + delta.X;
            Top = nativeFloatingWindowOrigin.Y + delta.Y;
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == wmLButtonUp && nativeFloatingMouseDownPoint.HasValue)
        {
            bool shouldApplyPenalty = !nativeFloatingWasDragged;
            nativeFloatingMouseDownPoint = null;
            nativeFloatingWasDragged = false;
            ReleaseCapture();
            if (shouldApplyPenalty)
            {
                ApplyFloatingOrbPenalty();
            }

            handled = true;
            return IntPtr.Zero;
        }

        if (msg == wmRButtonUp)
        {
            ShowFloatingOrbMenuAtCursor();
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private System.Windows.Vector DeviceDeltaToDips(System.Windows.Vector delta)
    {
        PresentationSource? source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformFromDevice.Transform(delta) ?? delta;
    }

    private static NativePoint GetCursorPoint()
    {
        GetCursorPos(out NativePoint point);
        return point;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCapture(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
}
