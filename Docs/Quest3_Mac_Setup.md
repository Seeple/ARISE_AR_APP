# 在当前 Mac 上构建并通过 USB 安装 Quest 3 应用

Quest 3 的独立运行安装包是 **Android APK**，不是 macOS 的 `.pkg`。
本流程使用 USB 安装到头显，不依赖 Unity Cloud 项目绑定。

## 电脑端环境

当前使用 Unity **2022.3.34f1c1**，已安装 Android Build Support、Android SDK & NDK
Tools 和 OpenJDK。Unity Editor 实际使用其安装目录下的配套工具：

| 项目 | 当前配置 |
| --- | --- |
| Android SDK | `PlaybackEngines/AndroidPlayer/SDK` |
| 已安装平台 | Android API 33、34 |
| SDK Build Tools | 32.0.0 |
| ADB / Platform Tools | 37.0.1（2026-10-27 更新） |
| NDK | r23b / 23.1.7779620 |
| JDK | OpenJDK 11.0.14.1 |
| 构建后端及架构 | IL2CPP、ARM64 |
| Active Input Handling | Input System Package (New) |

以上相对路径均位于 `/Applications/Unity/Hub/Editor/2022.3.34f1c1/` 下。
如需在界面核对，打开 **Unity > Settings/Preferences > External Tools > Android**，
使用随 Unity 安装的 JDK、SDK、NDK 和 Gradle。

终端的 `~/.local/bin/adb` 已链接到这套 Unity SDK；该目录已在当前用户的 PATH 中。
也可以在项目根目录运行以下命令，直接使用项目版本对应的 ADB：

```sh
sh Tools/quest-adb.sh version
sh Tools/quest-adb.sh devices -l
```

**macOS 不需要 Ubuntu 的 `plugdev` 用户组或 `udev` 规则，也不需要 Windows 的
Oculus ADB USB 驱动。** Meta Quest Developer Hub 可作为可选的图形化设备管理工具；
使用 Unity Build and Run 或 ADB 安装 APK 不需要先安装它。

## 头显首次连接当前 Mac

1. 确认原先在 Ubuntu 上使用的 Quest 3 仍已开启 Developer Mode。若未开启，在
   配对手机的 **Meta Horizon > 头显 > Headset Settings > Developer Mode** 中开启；
   账号须满足 Meta 的开发者团队及验证要求。
2. 用支持数据传输的 USB-C 线连接 Quest 3 和 Mac，并解锁、戴上头显。
3. 在头显中确认 **Allow USB debugging**。如果信任这台 Mac，可选择
   **Always allow from this computer**。这一步需要在头显中由你确认。
4. 运行 `adb devices -l`。只有设备状态显示 `device` 才代表已经授权，可安装应用。

| 检查结果 | 处理方法 |
| --- | --- |
| 空列表 | 检查数据线、USB 接口、头显开机状态和 Developer Mode |
| `unauthorized` | 在头显内确认这台 Mac 的 USB 调试授权 |
| `offline` | 重新连接 USB 并再次检查；必要时重启头显 |
| `device` | 已就绪，Unity 的 Run Device 中应可选择头显 |

若没有授权提示，按 Meta 的设备设置说明检查头显 Developer 设置中的 MTP Notification。

## 构建和安装

1. 在 Unity 打开本项目，进入 **File > Build Settings**。Unity 2022.3 使用
   **Android** 平台；较新 Meta 文档中的 Meta Quest Build Profile 属于更新版本的 Unity。
2. 确认 **Scenes In Build** 包含已勾选的 `Assets/Scenes/Teleoperation.unity`。
3. 使用 **IL2CPP + ARM64**。关闭 **Build App Bundle** 和 **Export Project**，生成 APK。
4. 可以先点击 **Build** 保存 APK；头显已授权后，在 **Run Device** 中选择设备，点击
   **Build And Run** 完成构建、安装和启动。

已有 APK 时，也可直接安装：

```sh
# 从项目根目录执行；-d 明确选择 USB 连接的唯一设备。
sh Tools/quest-adb.sh -d install -r Build/Android/ARISE_AR_APP.apk
```

若连接了多个 USB Android 设备，将 `-d` 换成 `-s <adb devices 中的设备编号>`。
安装后的开发应用可从头显应用列表的 Unknown Sources / 未知来源中找到。

如果同一应用原先由 Ubuntu 的另一把调试密钥签名，`install -r` 可能返回
`INSTALL_FAILED_UPDATE_INCOMPATIBLE`。需要沿用原签名密钥，或另行决定如何处理原安装；
不要直接卸载有重要数据的旧应用。

## 本机验证记录（2026-10-23）

- Android Player 脚本编译及 IL2CPP ARM64 原生编译完成。
- Unity 生成的 Android Gradle 工程执行完整 `assembleRelease` 成功。
- 产物：`Build/Android/ARISE_AR_APP.apk`，APK v2 签名校验通过。
  已校验的固定副本为 `Build/Android/ARISE_AR_APP-20261023-verified.apk`。
- APK 实际参数：`arm64-v8a`、min SDK 29、target SDK 34；沿用原包名
  `com.RobotFlow.TactAR` 和应用显示名 `ARToolHand`。
- 原自定义 Manifest 的 `application` 中删除了指向 launcher 专有资源的
  `@string/app_name`、`@mipmap/app_icon` 引用；launcher 继续提供实际名称和图标，
  并通过了最终 APK 检查。
- 旧 JDK 的一次 Maven TLS 下载失败在重试后恢复。仍使用 Unity 自带 JDK 11；
  没有安装校验未通过的备用 JDK 下载。
- 可选的 DetailedBuildReport 验证曾触发当前 Editor 的原生崩溃，记录保存在
  `Logs/Quest3/`；后续使用标准构建选项。
- 最后一次从 Editor 发起的重复构建复核遇到 MCP 超时；当时 Mac 已锁屏，
  无法检查界面提示。该次复核未计为成功，已成功的 Gradle 构建及签名检查有独立记录。
- ADB 最后检查结果为空列表，因此尚未验证头显授权、安装和真机运行。

详细构建日志见 `Logs/Quest3/gradle-validation.log`，校验摘要见
`Logs/Quest3/host-validation.json`。

## 2026-10-27：本机安装与并存方案

- Unity MCP 控制台显示此前 USB 未授权及短暂断连，进而导致 `adb reverse tcp:55504 tcp:55504` 失败。实际重试时设备已显示 `device`，端口转发成功；SDK/NDK/JDK 路径均正确，无需重装 Android SDK。
- 原包 `com.RobotFlow.TactAR` 的签名与本机调试签名不同，覆盖安装返回 `INSTALL_FAILED_UPDATE_INCOMPATIBLE`。用户明确选择保留旧应用及其数据，使用新包名并存安装。
- 新包名：`com.seeple.arise.arapp`；显示名：`ARISE_AR_APP`；版本：`0.1.0`。这些设置已保存到 Player Settings，后续从同一 Mac 构建可直接更新新应用。
- 构建：Unity MCP `build-daddd0e255` 成功，约 15.6 秒，0 错误、22 条警告。APK：`Build/Android/ARISE_AR_APP-Quest3-20261027.apk`，54,434,352 字节；APK v2 签名与 ZIP 完整性验证通过。
- ADB 安装返回 `Success`，显式启动返回 `Status: ok`，随后观察到应用 PID。读取该进程的 Unity/AndroidRuntime 错误日志时无输出。旧包仍存在，未卸载或清除其数据。
- 保持连线时仍观察到 USB 间歇性掉线；Mac ADB 日志为 `usb_read failed: e00002ed`（设备无响应）。已通过官方 SDK Manager 将 Platform Tools 从 32.0.0 更新到 37.0.1，并对原生与 libusb 两种后端做了约一分钟的实际 shell 往返测试：分别 31 次中 17 次、24 次失败。升级与后端切换均未消除掉线，最终恢复官方默认原生后端。需进一步排查直连接口、数据线、Hub 和头显唤醒/USB 状态。安装与启动已验证完成，USB 稳定性尚未解决。
- 原 Platform Tools 32.0.0 的备份位于 `Logs/QuestDeployment/2026-10-27/platform-tools-32.0.0-backup/`。
- 诊断和验证收据位于 `Logs/QuestDeployment/2026-10-27/`，其中保留了头显原 APK 的安装文件备份；这不是应用私有数据备份。

macOS 的应用菜单位于整个屏幕顶端。点击 Unity 窗口后，从系统菜单栏选择 **File > Build Settings**；菜单隐藏时将鼠标移到屏幕顶端，也可按 **Command + Shift + B**。选择 Android、勾选 Teleoperation、保持 Build App Bundle 与 Export Project 关闭，在 Run Device 选择 Quest 3，然后点击 **Build And Run**。只有 Build 会生成文件；Build And Run 才会进一步安装并启动。

已有新版 APK 时：

```sh
sh Tools/quest-adb.sh devices -l
QUEST_SERIAL=YOUR_QUEST_SERIAL
sh Tools/quest-adb.sh -s "$QUEST_SERIAL" install -r Releases/ARISE_AR_APP-0.1.9-b10.apk
sh Tools/quest-adb.sh -s "$QUEST_SERIAL" shell am start -n com.seeple.arise.arapp/com.unity3d.player.UnityPlayerActivity
```

## 官方依据

- [Meta：设备设置与首次 USB 调试授权](https://developers.meta.com/vr/documentation/native/android/mobile-device-setup/)
- [Android：macOS 无需额外 USB 配置；Ubuntu 的 plugdev 与 udev 要求](https://developer.android.com/studio/run/device)
- [Unity 2022.3：Android 环境设置与配套 SDK/NDK/JDK](https://docs.unity3d.com/2022.3/Documentation/Manual/android-sdksetup.html)
- [Meta：APK 构建、Run Device 和 Build and Run](https://developers.meta.com/vr/documentation/unity/unity-build/)
