using System;
using PCL.Core.App.IoC;
#if WPF
using System.Windows;
#endif

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.WindowCreating, Priority = int.MaxValue)]
public sealed class MainWindowService : GeneralService
{
#if WPF
    public static Func<Window>? Loading { private get; set; }

    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private MainWindowService() : base("window", "主窗体", false) { _context = ServiceContext; }
    
    public override void Start()
    {
        Context.Debug("正在初始化 WPF 窗体");
        var window = Loading!.Invoke();
        window.Loaded += (_, _) => Lifecycle.OnWindowCreated();
        ((Application)Lifecycle.CurrentApplication).MainWindow = window;
        Context.Trace("窗体创建完毕");
    }
#else
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private MainWindowService() : base("window", "主窗体", false) { _context = ServiceContext; }
    
    public override void Start()
    {
        Context.Debug("窗体服务初始化 (非 WPF 模式)");
        // OnWindowCreated 由宿主应用（Avalonia）在窗口实际显示时调用
    }
#endif
}
