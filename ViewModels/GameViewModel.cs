using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using checkers_game.Models;
using System.Windows.Input;
using checkers_game.Models;

namespace checkers_game.ViewModels;

public partial class GameViewModel : ObservableObject
{
    [ObservableProperty]
    private string _status = "Выберите режим";

    [ObservableProperty]
    private bool _isStartScreen = true;

    public ICommand StartHumanVsHumanCommand { get; }
    public ICommand StartHumanVsAICommand { get; }
    public ICommand ExitCommand { get; }

    public GameViewModel()
    {
        StartHumanVsHumanCommand = new RelayCommand(() => StartGame(GameMode.HumanVsHuman));
        StartHumanVsAICommand = new RelayCommand(() => StartGame(GameMode.HumanVsAI));
        ExitCommand = new RelayCommand(() => System.Environment.Exit(0));
    }

    private void StartGame(GameMode mode)
    {
        IsStartScreen = false;
        Status = "Игра началась! (режим: " + (mode == GameMode.HumanVsHuman ? "человек-человек" : "человек-ИИ") + ")";
        // Пока ничего не делаем — просто скрываем меню
    }
}