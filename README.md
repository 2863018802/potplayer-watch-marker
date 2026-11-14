# 追剧标记

Windows 本地 PotPlayer 观看位置标记工具。支持播放器内连播，退出播放器后在视频目录更新黑金箭头标记，并刷新已打开的对应文件夹。

## 使用

将项目文件夹放在视频根目录下，运行 `追剧标记.exe`。程序监控上一级目录中的 MP4。设置与使用说明位于窗口左侧，支持开机自启动与通知开关。详细操作见 [使用说明](使用说明.md)。

## 构建

需要 Windows 和 .NET Framework 4 编译器。退出正在运行的程序，然后在 PowerShell 中执行 `powershell -ExecutionPolicy Bypass -File .\build.ps1`。运行或分发时保留 EXE 旁的 `assets` 文件夹。

测试可执行 `追剧标记.exe --self-test`，结果写入 `self-test-result.txt`。自测试会临时切换自启动与通知设置，并在结束后恢复。

## 数据与限制

用户设置与观看状态保存在 `%LOCALAPPDATA%\PotWatchMarker`。源码不包含这些用户数据。视频名通过播放器窗口标题识别，需要标题包含 MP4 文件名。同名文件无法唯一定位时暂缓标记。标记是带归属描述的 `.lnk` 快捷方式。

资源管理器刷新会向对应目录发送系统变更通知，并刷新可通过 Windows Shell 访问的匹配窗口。不同 Windows 版本、多标签页布局的实际显示效果可能不同。

## 更新记录

见 [CHANGELOG.md](CHANGELOG.md)。当前版本 0.4.0，开源许可证尚待选定。
