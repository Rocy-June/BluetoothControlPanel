using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using BluetoothControlPanel.UI.Services.DependencyInjection;

namespace BluetoothControlPanel.UI.Services.Monitors;

[SingletonService(ServiceType = typeof(IMonitorService))]
public sealed class MonitorService : IMonitorService
{
    private static readonly MonitorEnumDelegate MonitorEnumCallback = MonitorEnum;

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        var gcHandle = GCHandle.Alloc(monitors);

        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            if (gcHandle.IsAllocated)
            {
                gcHandle.Free();
            }
        }

        return monitors;
    }

    public MonitorInfo? GetPrimaryMonitor()
    {
        foreach (var monitor in GetMonitors())
        {
            if (monitor.IsPrimary)
            {
                return monitor;
            }
        }

        return null;
    }

    private static bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
    {
        var monitorsHandle = GCHandle.FromIntPtr(dwData);
        if (monitorsHandle.Target is not List<MonitorInfo> list)
        {
            return false;
        }

        var info = new MONITORINFOEX
        {
            cbSize = Marshal.SizeOf<MONITORINFOEX>(),
            szDevice = new char[32]
        };

        if (GetMonitorInfo(hMonitor, ref info))
        {
            var monitorArea = new Rect(
                info.rcMonitor.left,
                info.rcMonitor.top,
                info.rcMonitor.right - info.rcMonitor.left,
                info.rcMonitor.bottom - info.rcMonitor.top);

            var workArea = new Rect(
                info.rcWork.left,
                info.rcWork.top,
                info.rcWork.right - info.rcWork.left,
                info.rcWork.bottom - info.rcWork.top);

            var name = new string(info.szDevice).TrimEnd('\0');
            var isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

            list.Add(new MonitorInfo(name, monitorArea, workArea, isPrimary));
        }

        return true;
    }

    #region P/Invoke

    private const int MONITORINFOF_PRIMARY = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct RectStruct
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RectStruct rcMonitor;
        public RectStruct rcWork;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] szDevice;
    }

    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    #endregion
}
