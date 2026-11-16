# 本地 APK 版本与保留规则

新包名固定为 `com.seeple.arise.arapp`。应用版本使用三段数字（例如 `0.1.1`），Android `versionCode` 每次递增，保证同一签名下可以更新安装。

仓库只分发 [0.1.0（build 1，基线）](../Releases/ARISE_AR_APP-0.1.0-b1.apk) 和 [0.1.9（build 10，当前版）](../Releases/ARISE_AR_APP-0.1.9-b10.apk)。下面记录的其他 APK 路径属于本机的构建历史。

## 构建新版本

Unity 菜单 **Tools > ARISE > Releases > Build Next Patch APK**：

1. 自动递增 patch 版本和 Android versionCode。
2. 保存到 `Build/Android/Releases/ARISE_AR_APP-<版本>-b<versionCode>.apk`。
3. 构建成功后记录 SHA-256、时间、包名、版本及保留标记。
4. 保留同一包名最新的 5 个版本，以及所有关键版本。

已有发布包禁止覆盖。File > Build 选择已登记的旧 APK 路径时也会拒绝构建，需使用新版本/新文件名。MCP 工作流对应 `VersionedApkBuilds.PrepareNextPatch()` → `manage_build` → 成功后 `RegisterSuccessfulBuild()`；构建失败不会登记为成功版本。

当前版本成为重要里程碑时，选择 **Tools > ARISE > Releases > Mark Current Version as Key**。关键版本不受“最新 5 个”限制。**Open APK Folder** 打开本地版本目录。

本地索引是 `Build/Android/Releases/catalog.json`，另有上一份 `.bak`；Build 目录不进 Git。请保留该目录以保留本地 APK 历史。自动清理仅处理该目录内已登记、校验和一致且不是关键版本的旧 APK；未知文件、目录外文件或被另行修改的文件不会删除。清理过的版本仍留有索引记录。

## 初始迁移

- 之前的 6 个 APK 均保留原文件名和原内容，登记为迁移前的关键里程碑，不做追溯清理。
- `ARISE_AR_APP-Quest3-20261027.apk` 明确登记为 **0.1.0 / build 1**，标记关键：这是首次以新包名安装到 Quest 3 的回退版本。
- 后续发布按下方版本记录保存，旧 APK 不覆盖。

## 0.1.1

键盘默认距离从 85 cm 缩短到 45 cm，下移量仍为 22 cm。保持左摇杆按下显示/隐藏；仅在键盘显示且非校准时使用左摇杆方向轴移动面板。松杆后固定，关闭重开回到默认位置。说明区同步增加了移位操作。

22 项键盘行为检查和 8 项版本保留检查通过。后者使用独立临时文件验证：保留最新五版和旧关键版、删除更老普通版、保护未知/外部/已修改文件和已有发布路径。证据在 `Logs/KeyboardReach/0.1.1/`。

本次 Android / IL2CPP / ARM64 构建成功（0 错误、29 条警告），APK v2 签名及 ZIP 完整性验证通过。ADB 更新安装返回 Success；头显安装记录确认 versionName=0.1.1、versionCode=2，启动返回 Status: ok。全部 6 个原 APK 的 SHA-256 均与修改前一致。

## 0.1.2 / build 3

修复键盘背景、按键和文字共用透明队列时因距离排序而出现的忽明忽暗。明确指定背景、键面、键字和 UI Canvas 的绘制层级，并把生成器误选的动态备用字体改为现成的静态字体。默认距离、摇杆移动和按钮绑定保持不变。输出路径为 `Build/Android/Releases/ARISE_AR_APP-0.1.2-b3.apk`。渲染回归验证见 `Logs/KeyboardRendering/0.1.2/validation.json`。

Android 构建成功（0 错误、23 条警告），APK 为 54,432,604 字节；ZIP 完整性与 v2 签名验证通过。ADB 安装返回 Success，启动返回 Status: ok，头显确认 versionName=0.1.2、versionCode=3。所有已保留旧 APK 哈希一致。SHA-256：`132f716cc2fc56b0017268e5cd59800a809d66f805b0d30907c46acc5ee3e6f6`。


## 0.1.3 / build 4（2026-11-09）

包含法兰下方 200 mm 的模型 TCP 基准、两类 ghost 的 0.25 倍显示、Instruction 下方校准航向角 +/- 1° 控件，以及校准期间键盘开关的输入修复。工具环口渲染方案未实施。

APK：`Build/Android/Releases/ARISE_AR_APP-0.1.3-b4.apk`，54,439,732 字节，SHA-256 `85e217d156d0a1dbececd55f478af4ad73b55556f31a1be0f03c397a6f85126c`。Unity MCP 作业 `build-01fb1200a7` 成功，0 错误、28 条警告；ZIP 完整性和 v2 签名验证通过。Quest 3 更新安装 Success，实际版本 0.1.3 / 4，启动 Status: ok。已有 8 个 APK 均保留且校验值不变。证据位于 `Logs/Releases/0.1.3/`。


## 0.1.4 / build 5（2026-11-10）

修复 calibration mode 下左摇杆不能移动键盘的问题：移除移动方法的校准拦截，将每帧摇杆读取移至校准提前返回之前。其他轨迹交互仍在校准时屏蔽。17 项普通/校准模式方向移动、死区、松杆保持、隐藏保护、开关和坐标系不受影响的检查通过。

Unity MCP 编译及 Android 构建成功，0 错误、29 条警告。APK `Build/Android/Releases/ARISE_AR_APP-0.1.4-b5.apk` 为 54,439,732 字节，SHA-256 `8fec5db762d8e6b6eac39300800a5e673bad55aa0e9cc324abac8ef9fb39837a`；ZIP 完整性与 v2 签名验证通过。Quest 3 更新安装 Success，启动 Status: ok，实际版本 0.1.4 / 5。已有 9 个 APK 均保留且哈希不变。证据：`Logs/Releases/0.1.4/`。


## 0.1.5 / build 6（2026-11-10）

按用户最新要求，将 action chunk 和 Robot TCP 两种 ghost 恢复 1:1 原尺寸显示；Prefab 根原点仍为 TCP，法兰到 TCP 的显示距离为 200 mm。保留校准时左摇杆移动键盘及 +/- 1° 角度微调。通过 MCP 验证两条实际创建路径的 scale=1 和法兰距离约 0.200 m，主场景已保存。

Unity MCP 编译及 Android 构建成功，0 错误、28 条警告。APK `Build/Android/Releases/ARISE_AR_APP-0.1.5-b6.apk` 为 54,439,760 字节，SHA-256 `980122e9b8b5ce83e23d39e35968a6be5c958055904c682ecbafa5d75526752e`；ZIP 完整性与 v2 签名验证通过。Quest 3 更新安装 Success，启动 Status: ok，实际版本 0.1.5 / 6。已有 10 个 APK 均保留且哈希不变。证据：`Logs/Releases/0.1.5/`。


## 0.1.6 / build 7（2026-11-10）

两种 ghost 改为 50% 显示。保留 TCP 相对基准，模型内法兰到 TCP 为 200 mm，显示距离为 100 mm；实际创建路径已通过 MCP 验证。

Unity 脚本编译及 Android 构建成功，0 错误、28 条警告。APK `Build/Android/Releases/ARISE_AR_APP-0.1.6-b7.apk` 为 54,439,744 字节，SHA-256 `e05be2323ea3a2d08e3e9cb5d2708ba88391905048c4bbd86ffca8e10dd414dd`；ZIP 和 v2 签名验证通过。Quest 3 安装 Success，启动 Status: ok，实际版本 0.1.6 / 7。保留最新五版及关键版本，依规则清理非关键 0.1.1；其他已保留 APK 哈希不变。证据：`Logs/Releases/0.1.6/`。构建完成后 Editor 曾记录 Library/Search/propertyAliases.db 的 Sharing violation，构建结果及头显启动均成功。

## 0.1.7 / build 8（2026-11-16）

两种夹爪恢复为 25% 和第一版原色（黑灰机身、橙色软指，运行时 Alpha 0.05）。放大轨迹地图支持左手移动与缩放、右手拖点时锁住地图；键盘 Instruction 增加手势说明。

Unity MCP 作业 `build-891f0346a6` 构建成功，0 错误、29 条警告。APK：`Build/Android/Releases/ARISE_AR_APP-0.1.7-b8.apk`，54,443,016 字节，SHA-256 `9e04eaaa43d772736db48434c74b0827a8f8a1a9a29a842e39c881601e08f367`。ZIP 完整性和 v2 签名验证通过，Quest 3 已更新并启动，安装版本为 0.1.7 / 8。按保留规则清理非关键 0.1.2，其余保留版本哈希不变。详情见 `Logs/Releases/0.1.7/`。

## 0.1.8 / build 9（2026-11-16）

两种原色夹爪的不透明度提高到 50%，保留 25% 缩放。单独松开 A 或 X 分别切换夹爪或轨迹；A+X 校准组合键不会改变显示状态。左手用拇指和食指捏合来拖动轨迹地图，松开后用两指间距缩放。键盘英文说明同步更新。

Unity MCP 作业 `build-07c7b5fa42` 构建成功（0 错误、33 条警告），53 项编辑器检查通过。APK：`Build/Android/Releases/ARISE_AR_APP-0.1.8-b9.apk`，54,443,488 字节，SHA-256 `6617076abcd5ef87c15f89cf7f406fed69d71d829a763d807e57123f32a9c782`。签名和完整性验证通过，Quest 3 更新后成功启动。按规则清理非关键 0.1.3，其余保留版本哈希不变。详情见 `Logs/Releases/0.1.8/`。

## 0.1.9 / build 10（2026-11-16）

两种原色夹爪的不透明度提高至 75%，保留 25% 显示比例。左手捏合拖动地图，缩放改用实际握住的右侧控制器摇杆。按左右来源隔离本地按键与工作站按钮/扳机通道，放下控制器后保持中立，重新拾起需先松键回中；右手拖点时冻结地图。

Unity MCP 作业 `build-f0779a4b55` 构建成功（0 错误、12 条警告），71 项编辑器检查通过。APK：`Build/Android/Releases/ARISE_AR_APP-0.1.9-b10.apk`，54,445,952 字节，SHA-256 `984b0fe29389c31be4f1e74f845c41dca73df07a78e797b30bf34e8a256d99f7`。签名和完整性验证通过，Quest 3 更新后成功启动。实际操作验收见 `[ARISE Input]`，可以核对本地显示、发包按键和轨迹点数。证据：`Logs/Releases/0.1.9/`。
