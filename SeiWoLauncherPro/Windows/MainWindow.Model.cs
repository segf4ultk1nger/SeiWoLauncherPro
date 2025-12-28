using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SeiWoLauncherPro {
    // 必须是 partial，因为 Source Generator 需要在另一个文件中生成剩下的部分
    public partial class MainWindowViewModel : ObservableObject {
        [ObservableProperty]
        private string _title = "Hello from CommunityToolkit!";

        [ObservableProperty]
        private int _clickCount = 0;

        // 自动生成名为 IncreaseCountCommand 的 ICommand
        [RelayCommand]
        private void IncreaseCount() {
            ClickCount++;
            Title = $"You clicked {ClickCount} times!";
        }

        // 甚至可以写异步命令，自带并发控制
        [RelayCommand]
        private async Task FetchDataAsync() {
            Title = "Fetching...";
            await Task.Delay(1000); // 模拟网络延迟
            Title = "Data Received!";
        }
    }
}