using System.IO;
using System.Windows;
using FfmpegGui.Core.Services;
using Microsoft.Win32;

namespace FfmpegGui.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateFFmpegStatus();
    }

    private void UpdateFFmpegStatus()
    {
        var paths = FFmpegLocator.TryLocate();
        StatusTextBlock.Text = paths is null
            ? "未找到 ffmpeg.exe / ffprobe.exe，请将它们放入 tools/ffmpeg/ 目录。"
            : "FFmpeg 已就绪，请选择要分析的媒体文件。";
    }

    private async void SelectFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择视频或音频文件",
            Filter = "媒体文件|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.flv;*.ts;*.m2ts;*.mp3;*.m4a;*.aac;*.flac;*.wav;*.ogg;*.opus|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        FilePathTextBox.Text = dialog.FileName;
        SelectFileButton.IsEnabled = false;
        StatusTextBlock.Text = "正在读取媒体信息...";

        try
        {
            var paths = FFmpegLocator.Locate();
            var service = new FfprobeService(paths);
            var mediaInfo = await service.ProbeAsync(dialog.FileName);

            DataContext = mediaInfo;
            StatusTextBlock.Text = $"读取完成：{mediaInfo.FileName}";
        }
        catch (Exception exception)
        {
            DataContext = null;
            StatusTextBlock.Text = "读取失败。";

            MessageBox.Show(
                this,
                exception.Message,
                "ffprobe 错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SelectFileButton.IsEnabled = true;
        }
    }
}
