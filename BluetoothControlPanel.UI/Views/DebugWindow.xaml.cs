using System.Windows;

using BluetoothControlPanel.UI.Services.DependencyInjection;
using BluetoothControlPanel.UI.ViewModels;

namespace BluetoothControlPanel.UI.Views;

[SingletonService]
public partial class DebugWindow : Window
{
    public DebugWindow(DebugViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
