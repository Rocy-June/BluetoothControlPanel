using System.Windows;
using BluetoothControlPanel.UI.ViewModels;

namespace BluetoothControlPanel.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
