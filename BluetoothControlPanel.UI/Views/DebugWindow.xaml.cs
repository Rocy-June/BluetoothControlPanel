using System.Windows;

using BluetoothControlPanel.Application.DependencyInjection;
using BluetoothControlPanel.Application.ViewModels;

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
