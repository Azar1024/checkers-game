using Avalonia.Controls;
using CheckersGame.ViewModels;

namespace CheckersGame.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new GameViewModel();
    }
}