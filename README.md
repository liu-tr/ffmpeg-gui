# FFmpeg GUI Tool

一个基于 FFmpeg 的便携 Windows 桌面端转码工具。

## 技术栈

- C# / .NET 10
- WPF
- FFmpeg / ffprobe 命令行进程
- Git + GitHub

## 项目结构

```text
ffmpeg-gui/
  FfmpegGui.sln
  src/
    FfmpegGui.App/      WPF 主程序
    FfmpegGui.Core/     核心业务逻辑
  tools/
    ffmpeg/             FFmpeg 可执行文件目录
  README.md
```

## 开发环境

- .NET SDK 10
- Windows 10/11
- Visual Studio 2022+、Rider 或 VS Code

## 构建

```powershell
dotnet build FfmpegGui.sln
```

如果当前环境遇到 MSBuild 多节点管道问题，可以使用：

```powershell
dotnet build FfmpegGui.sln -m:1
```

## FFmpeg 查找顺序

程序按以下顺序查找 `ffmpeg.exe` 和 `ffprobe.exe`：

1. 程序目录或上级目录下的 `tools/ffmpeg/`
2. 程序运行目录
3. 环境变量 `FFMPEG_TOOLS_DIR` 指定的目录
4. 系统 `PATH`

便携发布时，将 `ffmpeg.exe` 和 `ffprobe.exe` 放到：

```text
tools/ffmpeg/
```

## 当前功能

- 选择视频/音频文件
- 使用 `ffprobe` 读取媒体信息
- 显示封装格式、时长、码率、文件大小
- 显示视频流：编码、Profile、分辨率、帧率、码率
- 显示音频流：编码、码率、采样率、声道数
- 显示多流信息、语言、默认轨、强制轨
- 选择输出模式：视频 + 音频、仅视频、仅音频
- 选择封装格式：MP4、MKV、WebM、MOV、M4A、MP3、FLAC、WAV、OGG、Opus
- 选择视频编码：H.264、H.265、VP9
- 选择音频编码：AAC、MP3、Opus、Vorbis、FLAC、PCM
- 选择视频质量预设和音频码率
- 自动过滤容器与编码器的兼容组合
- 自动生成不覆盖已有文件的输出路径
- 调用 FFmpeg 执行转码/转封装
- 显示进度和编码速度
- 支持取消转码

## 待办

- 批量任务队列
- 字幕流和多音轨选择
- 硬件编码
- 更完整的 FFmpeg 日志和错误解析
- 便携发布配置
- FFmpeg 二进制体积优化
