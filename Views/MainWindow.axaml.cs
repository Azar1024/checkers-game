using Avalonia.Controls;

namespace checkers_game.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModels.GameViewModel();
    }
}