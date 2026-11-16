# TCP 基准与校准微调（2026-11-08）

## 夹爪 TCP

大寰 [AG 系列官方资料](https://www.dh-robotics.com/product/ag)列出的 AG-160-95 尺寸为 184.6 × 162.3 × 67 mm、行程 95 mm，适用于原厂指形，不是本项目自定义软指的 TCP 定义。现有模型是照片和官方比例重建的显示模型，并非厂家 CAD。200 mm 来自本次用户实测要求。

当前模型的法兰安装环外表面坐标为旧模型局部 Y=202+12/2=208 mm；指尖方向为局部 -Y。因此新的 TCP 对应旧坐标 Y=208-200=8 mm。将全部可视几何统一下移 8 mm，Prefab 根原点即位于法兰下方 200 mm。没有对网络中的姿态/轨迹坐标加额外偏移。2026-11-15 将轨迹与 Robot TCP 两种 ghost 恢复到 25% 显示，模型内法兰到 TCP 为 200 mm，场景显示为 50 mm。保留 8 mm 几何基准偏移，轨迹坐标不变，轨迹点继续与模型 TCP 根原点重合。FlangeMountDatum 与实际法兰环顶点一致；开合不改变法兰到 TCP 的距离。构建器也使用同一常量，重新生成不会丢失基准。

## 角度微调

Instruction 下方增加 `CALIBRATION YAW / 1 DEG STEP`，显示 `+ 90.0° -`。读数是 Calibration.forward 投影到地面后的世界航向角，按 Unity 约定绕世界 +Y，范围 [0,360)。每次 release 增/减 1°。直接更新 Calibration.rotation，保持其位置不动；机器人可视坐标和发出的逆坐标转换使用同一根变换。手动 trigger 调整后读数会随之刷新。

常态下按钮和数值灰显，PokeInteractable 禁用，回调中另有 IsRunning 检查；进入校准后变亮并启用，退出立即锁定。左摇杆按下显示/隐藏键盘的处理移到校准提前返回之前；校准重置只保留右摇杆按下，左摇杆不再重置坐标系。2026-11-10 修复：校准时同样允许左摇杆方向轴移动键盘，轨迹编辑等其他操作仍在校准提前返回之后屏蔽。

X+A 改为组合键的按下沿切换，避免持续按住每隔 500 ms 反复进入/退出。原来的 trigger 粗调和平移、坐标轴缩放仍保留。

说明增加：

> During calibration, view the TCP from different angles. If its position stays fixed, calibration is complete.

## 验证

证据在 `Logs/CalibrationUpgrade/2026-11-08/`，包含修改前快照、输入路由检查及 Unity MCP 隔离场景验证。检查真实 +/- UnityEvent、角度更新、越过 360°、非校准锁定、坐标转换、键盘开关、文本排版、静态字体、不同开口的法兰基准、实际法兰环顶点，以及轨迹 ghost / Robot TCP ghost 两条加载路径。UI 图片见 `Images/Keyboard-Console.png` 和 `Images/Keyboard-Calibration-Active.png`。

此次只进行 Unity 脚本编译和编辑器内验证，不生成或部署 APK；没有头显按键或实体机械臂实测。

最终结果：23 项 Unity MCP 行为/几何检查全部通过，主场景引用及父级缩放核验通过；脚本编译完成，最终控制台错误为 0。全部既有 APK 的文件列表和 SHA-256 均与本次修改前相同。

### 后续显示比例调整

按用户最新决定恢复 0.25 倍显示。11 项比例/定位检查通过：在三组不同 TCP 姿态和开口下，实际 action chunk ghost 的根原点均与原轨迹点重合，几何顶点及法兰基准相对 TCP 的位移均为 1:1 显示时的 0.25 倍；Robot TCP ghost 使用相同规则。模型内 200 mm 在显示中为 50 mm，TCP 在夹爪内部的相对位置不变。验证结果：`Logs/CalibrationUpgrade/2026-11-08/quarter-scale/validation.json`。Unity 脚本编译通过，未构建 APK。
