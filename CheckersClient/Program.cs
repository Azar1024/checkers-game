using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading;

namespace CheckersGame
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // НАСТРОЙКА AVALONIA ПРИЛОЖЕНИЯ 
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()    // Автоопределение платформы
                .WithInterFont()        // Шрифт по умолчанию
                .LogToTrace()           // Логирование в трассировку
                .UseReactiveUI();       // Поддержка ReactiveUI для MVVM
    }
}