# FFmpeg GUI Tool

一个基于 FFmpeg 的便携 Windows 桌面端转码工具。

## 技术栈

- C# / .NET 10
- WPF
- FFmpeg / ffprobe 命令行进程
- Git + GitHub

## 目标

- 导入视频/音频文件
- 显示容器、视频编码、音频编码等媒体信息
- 选择输出封装格式和音视频编码器
- 调用 FFmpeg 转码/转封装
- 输出到指定文件夹
- 支持进度显示、取消和日志

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

## FFmpeg 准备

将 `ffmpeg.exe` 和 `ffprobe.exe` 放到：

```text
tools/ffmpeg/
```

## 状态

项目骨架初始化中。
