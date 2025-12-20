using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.Services.Bluetooth;
using BluetoothControlPanel.Application.Services.Config;
using BluetoothControlPanel.Application.Services.Logging;
using BluetoothControlPanel.Domain.Bluetooth;

namespace BluetoothControlPanel.Application.ViewModels;

[SingletonService]
public partial class DebugViewModel : ViewModelBase
{
    public DebugViewModel(ILogService logService)
    {
        ConfigureLogging(logService);
    }

}
