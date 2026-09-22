using System.IO;
using System.Windows;
using FfmpegGui.Core.Models;
using FfmpegGui.Core.Services;
using Microsoft.Win32;

namespace FfmpegGui.App;

public partial class MainWindow : Window
{
    private const int MaxLogLines = 500;

    private readonly Queue<string> _logLines = new();

    private FFmpegPaths? _ffmpegPaths;
    private MediaInfo? _currentMediaInfo;
    private CancellationTokenSource? _transcodeCancellation;
    private bool _isUpdatingOutputOptions;
    private bool _isTranscoding;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _ffmpegPaths = FFmpegLocator.TryLocate();
        StatusTextBlock.Text = _ffmpegPaths is null
            ? "未找到 ffmpeg.exe / ffprobe.exe，请将它们放入 tools/ffmpeg/ 目录。"
            : "FFmpeg 已就绪，请选择要分析的媒体文件。";

        InitializeOutputModes();
        UpdateOutputControlsState();
    }

    private void InitializeOutputModes()
    {
        var modes = new List<OutputModeOption>
        {
            new(OutputKind.VideoAndAudio, "视频 + 音频"),
            new(OutputKind.VideoOnly, "仅视频"),
            new(OutputKind.AudioOnly, "仅音频")
        };

        _isUpdatingOutputOptions = true;
        OutputModeComboBox.ItemsSource = modes;
        OutputModeComboBox.SelectedIndex = 0;
        _isUpdatingOutputOptions = false;

        RefreshContainerOptions();
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

            _currentMediaInfo = mediaInfo;
            DataContext = mediaInfo;

            if (string.IsNullOrWhiteSpace(OutputFolderTextBox.Text))
            {
                OutputFolderTextBox.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            }

            RefreshTrackOptions(mediaInfo);
            ConfigureOutputModesForMedia(mediaInfo);
            StatusTextBlock.Text = $"读取完成：{mediaInfo.FileName}";
        }
        catch (Exception exception)
        {
            _currentMediaInfo = null;
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
            UpdateOutputControlsState();
        }
    }

    private void RefreshTrackOptions(MediaInfo mediaInfo)
    {
        _isUpdatingOutputOptions = true;

        try
        {
            VideoTrackComboBox.ItemsSource = mediaInfo.VideoStreams;
            VideoTrackComboBox.SelectedIndex = mediaInfo.VideoStreams.Count > 0 ? 0 : -1;

            AudioTrackComboBox.ItemsSource = mediaInfo.AudioStreams;
            AudioTrackComboBox.SelectedIndex = mediaInfo.AudioStreams.Count > 0 ? 0 : -1;
        }
        finally
        {
            _isUpdatingOutputOptions = false;
        }
    }

    private void ConfigureOutputModesForMedia(MediaInfo mediaInfo)
    {
        var hasVideo = mediaInfo.VideoStreams.Count > 0;
        var hasAudio = mediaInfo.AudioStreams.Count > 0;
        var modes = new List<OutputModeOption>();

        if (hasVideo && hasAudio)
        {
            modes.Add(new OutputModeOption(OutputKind.VideoAndAudio, "视频 + 音频"));
        }

        if (hasVideo)
        {
            modes.Add(new OutputModeOption(OutputKind.VideoOnly, "仅视频"));
        }

        if (hasAudio)
        {
            modes.Add(new OutputModeOption(OutputKind.AudioOnly, "仅音频"));
        }

        _isUpdatingOutputOptions = true;
        OutputModeComboBox.ItemsSource = modes;
        OutputModeComboBox.SelectedIndex = modes.Count > 0 ? 0 : -1;
        _isUpdatingOutputOptions = false;

        RefreshContainerOptions();
    }

    private void OutputModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        RefreshContainerOptions();
    }

    private void ContainerComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        RefreshCodecOptions();
    }

    private void VideoCodecComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void VideoQualityComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void AudioCodecComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void AudioBitrateComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void VideoTrackComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void AudioTrackComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void OutputFolderTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        UpdateOutputControlsState();
    }

    private void RefreshContainerOptions()
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        _isUpdatingOutputOptions = true;

        try
        {
            var outputKind = GetSelectedOutputKind();
            var containers = MediaFormatCatalog.GetContainers(outputKind);
            var previousContainerId = (ContainerComboBox.SelectedItem as ContainerOption)?.Id;

            ContainerComboBox.ItemsSource = containers;
            ContainerComboBox.SelectedItem =
                containers.FirstOrDefault(container => string.Equals(container.Id, previousContainerId, StringComparison.OrdinalIgnoreCase)) ??
                containers.FirstOrDefault();

            RefreshCodecOptionsCore();
        }
        finally
        {
            _isUpdatingOutputOptions = false;
        }

        UpdateOutputControlsState();
    }

    private void RefreshCodecOptions()
    {
        if (_isUpdatingOutputOptions)
        {
            return;
        }

        _isUpdatingOutputOptions = true;

        try
        {
            RefreshCodecOptionsCore();
        }
        finally
        {
            _isUpdatingOutputOptions = false;
        }

        UpdateOutputControlsState();
    }

    private void RefreshCodecOptionsCore()
    {
        var container = ContainerComboBox.SelectedItem as ContainerOption;
        var previousVideoCodecId = (VideoCodecComboBox.SelectedItem as VideoCodecOption)?.Id;
        var previousAudioCodecId = (AudioCodecComboBox.SelectedItem as AudioCodecOption)?.Id;

        var videoCodecs = container is null
            ? Array.Empty<VideoCodecOption>()
            : MediaFormatCatalog.GetVideoCodecs(container).ToArray();

        var audioCodecs = container is null
            ? Array.Empty<AudioCodecOption>()
            : MediaFormatCatalog.GetAudioCodecs(container).ToArray();

        VideoCodecComboBox.ItemsSource = videoCodecs;
        VideoCodecComboBox.SelectedItem =
            videoCodecs.FirstOrDefault(codec => string.Equals(codec.Id, previousVideoCodecId, StringComparison.OrdinalIgnoreCase)) ??
            videoCodecs.FirstOrDefault();

        AudioCodecComboBox.ItemsSource = audioCodecs;
        AudioCodecComboBox.SelectedItem =
            audioCodecs.FirstOrDefault(codec => string.Equals(codec.Id, previousAudioCodecId, StringComparison.OrdinalIgnoreCase)) ??
            audioCodecs.FirstOrDefault();

        VideoQualityComboBox.ItemsSource = MediaFormatCatalog.VideoQualities;
        VideoQualityComboBox.SelectedIndex = 1;

        AudioBitrateComboBox.ItemsSource = MediaFormatCatalog.AudioBitrates;
        AudioBitrateComboBox.SelectedItem =
            MediaFormatCatalog.AudioBitrates.FirstOrDefault(option => option.BitrateKbps == 192) ??
            MediaFormatCatalog.AudioBitrates.FirstOrDefault();
    }

    private OutputKind GetSelectedOutputKind()
    {
        return OutputModeComboBox.SelectedItem is OutputModeOption option
            ? option.Kind
            : OutputKind.VideoAndAudio;
    }

    private void UpdateOutputControlsState()
    {
        var running = _isTranscoding;
        var outputKind = GetSelectedOutputKind();
        var container = ContainerComboBox.SelectedItem as ContainerOption;
        var videoCodec = VideoCodecComboBox.SelectedItem as VideoCodecOption;
        var audioCodec = AudioCodecComboBox.SelectedItem as AudioCodecOption;
        var videoTrack = VideoTrackComboBox.SelectedItem as MediaStreamInfo;
        var audioTrack = AudioTrackComboBox.SelectedItem as MediaStreamInfo;

        var needsVideo = outputKind != OutputKind.AudioOnly;
        var needsAudio = outputKind != OutputKind.VideoOnly;

        OutputModeComboBox.IsEnabled = !running;
        ContainerComboBox.IsEnabled = !running && container is not null;
        VideoCodecComboBox.IsEnabled = !running && needsVideo && videoCodec is not null;
        VideoQualityComboBox.IsEnabled = !running && needsVideo && videoCodec is not null;
        AudioCodecComboBox.IsEnabled = !running && needsAudio && audioCodec is not null;
        AudioBitrateComboBox.IsEnabled = !running && needsAudio && audioCodec?.IsLossy == true;
        VideoTrackComboBox.IsEnabled = !running && needsVideo && videoTrack is not null;
        AudioTrackComboBox.IsEnabled = !running && needsAudio && audioTrack is not null;
        OutputFolderTextBox.IsEnabled = !running;
        BrowseOutputFolderButton.IsEnabled = !running;
        SelectFileButton.IsEnabled = !running;

        var hasVideo = _currentMediaInfo?.VideoStreams.Count > 0;
        var hasAudio = _currentMediaInfo?.AudioStreams.Count > 0;

        var ready = !running &&
                    _ffmpegPaths is not null &&
                    _currentMediaInfo is not null &&
                    container is not null &&
                    (!needsVideo || (hasVideo && videoCodec is not null && videoTrack is not null)) &&
                    (!needsAudio || (hasAudio && audioCodec is not null && audioTrack is not null)) &&
                    !string.IsNullOrWhiteSpace(OutputFolderTextBox.Text);

        StartButton.IsEnabled = ready;
        CancelButton.IsEnabled = running;
    }

    private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择输出文件夹"
        };

        if (Directory.Exists(OutputFolderTextBox.Text))
        {
            dialog.InitialDirectory = OutputFolderTextBox.Text;
        }

        if (dialog.ShowDialog(this) == true)
        {
            OutputFolderTextBox.Text = dialog.FolderName;
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ffmpegPaths is null || _currentMediaInfo is null)
        {
            return;
        }

        var outputKind = GetSelectedOutputKind();
        var container = ContainerComboBox.SelectedItem as ContainerOption;
        var videoCodec = VideoCodecComboBox.SelectedItem as VideoCodecOption;
        var audioCodec = AudioCodecComboBox.SelectedItem as AudioCodecOption;
        var videoQuality = VideoQualityComboBox.SelectedItem as VideoQualityOption;
        var audioBitrate = AudioBitrateComboBox.SelectedItem as AudioBitrateOption;
        var videoTrack = VideoTrackComboBox.SelectedItem as MediaStreamInfo;
        var audioTrack = AudioTrackComboBox.SelectedItem as MediaStreamInfo;
        var outputFolder = OutputFolderTextBox.Text.Trim();

        if (container is null)
        {
            MessageBox.Show(this, "请选择封装格式。", "输出设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (outputKind != OutputKind.AudioOnly && (videoCodec is null || videoTrack is null))
        {
            MessageBox.Show(this, "请选择视频编码器和视频轨道。", "输出设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (outputKind != OutputKind.VideoOnly && (audioCodec is null || audioTrack is null))
        {
            MessageBox.Show(this, "请选择音频编码器和音频轨道。", "输出设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            MessageBox.Show(this, "请选择输出文件夹。", "输出设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string outputPath;

        try
        {
            outputPath = OutputFileService.CreateOutputPath(
                _currentMediaInfo.FilePath,
                outputFolder,
                container,
                outputKind);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "输出路径错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var settings = new OutputSettings
        {
            InputPath = _currentMediaInfo.FilePath,
            OutputPath = outputPath,
            OutputKind = outputKind,
            Container = container,
            VideoCodec = outputKind == OutputKind.AudioOnly ? null : videoCodec,
            VideoQuality = outputKind == OutputKind.AudioOnly ? null : videoQuality,
            VideoStreamOrdinal = videoTrack is null ? 0 : VideoTrackComboBox.SelectedIndex,
            AudioCodec = outputKind == OutputKind.VideoOnly ? null : audioCodec,
            AudioBitrate = outputKind == OutputKind.VideoOnly ? null : audioBitrate,
            AudioStreamOrdinal = audioTrack is null ? 0 : AudioTrackComboBox.SelectedIndex
        };

        IReadOnlyList<string> arguments;

        try
        {
            arguments = FfmpegCommandBuilder.Build(settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "FFmpeg 参数错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ClearLog();
        _isTranscoding = true;
        _transcodeCancellation = new CancellationTokenSource();
        ProgressBar.IsIndeterminate = false;
        ProgressBar.Value = 0;
        ProgressTextBlock.Text = "开始转码...";
        UpdateOutputControlsState();

        var progress = new Progress<FfmpegProgress>(UpdateProgress);
        var logProgress = new Progress<string>(AppendLog);

        try
        {
            var runner = new FfmpegRunner(_ffmpegPaths);
            await runner.RunAsync(
                arguments,
                _currentMediaInfo.Duration,
                progress,
                logProgress,
                _transcodeCancellation.Token);

            ProgressBar.Value = 100;
            ProgressTextBlock.Text = "转码完成";
            StatusTextBlock.Text = $"输出文件：{outputPath}";

            MessageBox.Show(
                this,
                $"转码完成。\n\n输出文件：\n{outputPath}",
                "完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            ProgressTextBlock.Text = "已取消";
            StatusTextBlock.Text = "转码已取消。";
        }
        catch (FfmpegException exception)
        {
            ProgressTextBlock.Text = "转码失败";
            StatusTextBlock.Text = "转码失败。";

            var detail = exception.Message;
            if (!string.IsNullOrWhiteSpace(exception.ErrorOutput))
            {
                detail += $"\n\nFFmpeg 错误：\n{exception.ErrorOutput}";
            }

            MessageBox.Show(this, detail, "FFmpeg 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception exception)
        {
            ProgressTextBlock.Text = "转码失败";
            StatusTextBlock.Text = "转码失败。";
            MessageBox.Show(this, exception.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isTranscoding = false;
            _transcodeCancellation?.Dispose();
            _transcodeCancellation = null;
            UpdateOutputControlsState();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_transcodeCancellation is null)
        {
            return;
        }

        CancelButton.IsEnabled = false;
        ProgressTextBlock.Text = "正在取消...";
        _transcodeCancellation.Cancel();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        ClearLog();
    }

    private void ClearLog()
    {
        _logLines.Clear();
        LogTextBox.Text = string.Empty;
    }

    private void AppendLog(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        _logLines.Enqueue(line);

        while (_logLines.Count > MaxLogLines)
        {
            _logLines.Dequeue();
        }

        LogTextBox.Text = string.Join(Environment.NewLine, _logLines);
        LogTextBox.ScrollToEnd();
    }

    private void UpdateProgress(FfmpegProgress progress)
    {
        if (progress.Percentage is { } percentage)
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = percentage;
        }
        else
        {
            ProgressBar.IsIndeterminate = true;
        }

        var parts = new List<string>();

        if (progress.Percentage is { } percent)
        {
            parts.Add($"{percent:0.0}%");
        }

        if (progress.Speed is { } speed && speed > 0)
        {
            parts.Add($"{speed:0.00}x");
        }

        if (progress.ProcessedDuration is { } processed)
        {
            parts.Add(processed.ToString(@"hh\:mm\:ss"));
        }

        ProgressTextBlock.Text = parts.Count > 0 ? string.Join("  |  ", parts) : "转码中...";
    }
}
