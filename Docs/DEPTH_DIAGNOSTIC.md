# VAC 深度调试版

基线为 DonGCoco/VAC 的 fix/pilot-depth-stability-stats 分支。
在 VAC 项目里切换到调试分支。

```sh
git fetch origin
git switch --track origin/codex/depth-diagnostic
```

用 Unity 6000.2.15f1 打开平时打 Pilot APK 用的同一个场景，确认当前目标是 Android。
菜单选 **VAC → Build Depth Diagnostic APK**，产物为 **Builds/VAC-Depth-Diagnostic.apk**。
用 Magic Leap Hub 安装文件，头显里打开 **VAC Depth Diagnostic**。它使用独立应用 ID。

## 现场

每组先面向正前方按下扳机。每道题出球后，先说哪边更远、哪边更暗，再按扳机换题。
分不清就说“看不出来”。亮度相同就说“一样亮”。这版扳机只用于推进。
每组八题，远侧各四题，随机顺序，换题时黑屏仅指虚拟球隐藏 0.5 秒，现实背景仍可见。

| 组别 | 深度差 | 头显数字 |
| --- | --- | --- |
| 1 | Pilot 原值，当前应为 6 cm | 无 |
| 2 | Pilot 原值的四倍，当前应为 24 cm | 无 |
| 3 | Pilot 原值 | 有 |
| 4 | Pilot 原值的四倍 | 有 |

每题的亮度和深度口头回答由操作者手记。系统日志会输出题号、计划远侧和按球坐标算出的左右深度。

```sh
adb logcat -s Unity | grep VACDepthDiagnostic
```

例如 left_m=1.0300、right_m=0.9700 表示左边远。用日志判断深度是否交换，再比对口头答案。
如果暗侧跟着远侧换，检查深度相关的呈现。如果暗侧固定，检查左右球、背景和头显显示。
