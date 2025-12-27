using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SeiWoLauncherPro {
    public class Program {
        [STAThread]
        public static void Main(string[] args) {
            // 创建通用主机
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) => {
                    // 核心逻辑：把 App 和 MainWindow 都塞进容器
                    services.AddSingleton<Application>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainWindowViewModel>();

                })
                .Build();

            // 从容器里捞出 App 实例并启动
            var app = host.Services.GetRequiredService<Application>();
            var mainWindow = host.Services.GetRequiredService<MainWindow>();

            // 运行 WPF 消息循环
            app.Run(mainWindow);
        }
    }
}