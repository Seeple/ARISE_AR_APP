# 项目完整性检查：2026-10-26

以 `~/Documents/ARToolHand_APP_COPY` 为用户提供的最后成功编译备份，对比当前 `~/Documents/ARISE_AR_APP`。本次检查保留两份工程源文件，不恢复文件、不改挂载、不构建 APK、不进入 Play Mode。Unity MCP 连接目标已核实为当前 ARISE_AR_APP。

检查结论：当前项目的业务代码基本一致，Android 构建配置保留；但缺少运行时加载的幽灵夹爪 Prefab，因此还不能视作备份的完整恢复版。已定位到所需原始文件和依赖 GUID，无需凭名称重新制作资源。

## 文件对比范围与结果

对 `Assets/`、`Packages/`、`ProjectSettings/` 所有文件逐个比较内容，排除 Finder 的 `.DS_Store`。原始备份 479 个文件，当前 481 个文件；450 个完全一致、26 个内容不同、3 个仅在备份中、5 个仅在当前工程中。另对 `Docs/`、`Image/`、`OculusPackages/` 比较完整文件内容：备份原有内容均存在且一致；当前额外有两份本机配置文档。OculusPackages 中 5,151 个文件一致。

业务和示例 C# 共 45 个文件，两份均存在；唯一代码差异为 `Assets/Scripts/Mykeyboard.cs` 中的操作提示文字，未发现控制、轨迹可视化或网络处理代码遗漏。

## 需要处理的差异

1. **缺少幽灵夹爪 Prefab 及其元数据。** 备份包含 `Assets/Resources/Prefab/GhostGripper.prefab` 和 `.meta`，当前两者缺失。`ChunkVisualizer` 与 `VisualizationServer` 仍以 `Prefab/GhostGripper` 为 Resources 路径加载。通过当前 Unity 实测 `Resources.Load<GameObject>("Prefab/GhostGripper")` 返回 null。主场景 `showGhostGrippers=1`，所以这不是一个完全未使用的资源：选中点、采样点的幽灵夹爪显示会缺失；Robot TCP ghost 功能开启时同样受影响。

   直接复制 Prefab 尚不足以恢复，以下依赖的 GUID 已在本机重新生成：

   | 依赖 | 备份 GUID | 当前 GUID |
   |---|---|---|
   | `GripperController.cs`，实际类为 `GripperJawController` | `ca23d4081badc4bd5be208e07a05a2fc` | `4c57a1e27575c47c39f9bcd7b5f827bd` |
   | `BlackMaterial.mat` | `921b1fd840a468719933a12bc7ccc23f` | `03beda88861da4cc184032d71c45c00b` |
   | `OrangeMaterial.mat` | `99bde63f1866bc86093a55ed500cadd9` | `b9d8c1d3ebe4949d49c1e93c730df654` |

   恢复时应连同 `.meta` 和依赖处理：可保留原始 GUID，或精确重映射 Prefab 引用，避免仅复制后留下失效脚本、材质。原始 Prefab 自身 GUID 为 `e560002366ef8ee4fbfa225d1f3436bd`。

2. **版本控制仍无法完整保存项目。** 当前 `.gitignore` 广泛忽略 `*.meta`、`*.prefab`、`*.asset`、`*.unity`、`*.json`。核心目录有 19 个未跟踪且被忽略的本地文件，其中包含六个脚本、六个材质及其目录的元数据。GhostGripper 的两个文件也命中忽略规则，这与迁移后遗漏相符。已跟踪的场景和设置仍被 Git 跟踪；风险集中在新增和从未提交的文件。恢复后需修正忽略规则、纳入必要源资源和元数据，才能验证重新克隆的完整性。

3. **手动重挂后的显示参数与备份不同。** 当前 Main 的可视化组件引用有效，但数值不是原备份值：

   | 字段 | 成功编译备份 | 当前 |
   |---|---:|---:|
   | `pointSize` | 0.005 | 0.0005 |
   | `ghostScale` | 0.25 | 0.5 |
   | `ghostAlpha` | 0.05 | 0.35 |

   当前轨迹点是原来的十分之一；幽灵夹爪缩放两倍，alpha 更高。它们未在本次检查中被回退，是否沿用应按用户希望的显示效果决定。

4. **另缺一份 Linux Burst 设置。** `ProjectSettings/BurstAotSettings_StandaloneLinux64.json` 仅存在于备份中。其目标为 Standalone Linux，不是当前 Quest Android 构建阻碍；若希望保留原项目的跨平台配置，可以恢复该文件。

## 已确认正常或应保留的差异

- 当前 `Main` 仅有一个 `VRController`，以及 `VisualizationServer`、`UnityMainThreadDispatcher`、`ChunkVisualizer`。额外的禁用 VRController 已不存在；控制器的 `chunkVisualizer` 指向正确组件，`ovrhead` 指向 CenterEyeAnchor。
- 主场景已保存，无编译、导入或构建任务。控制台当前 0 条 Error。当前加载的 983 个 GameObject 中，仅三个 Eye Anchor 各有一个已知的 URP 失效组件；Main 无 Missing Script。
- 92 个 Renderer 的 94 个材质槽没有空材质或错误着色器。Resources/Material 中检测到的材质能加载，当前 Editor 支持其着色器。四种轨迹材质引用有效。
- 直接包依赖仅新增 Unity MCP；备份已有的所有锁定包版本均未变化，当前所需 Registry/Git 包均有缓存且 Registry 版本匹配。Meta XR 保持 62.0.0，Oculus XR Plugin 保持 4.5.0。Registry 地址从 unity.com 改为 unity.cn。
- 当前 Editor 为 `2022.3.34f1c1`，原始备份为 `2022.3.34f1`。当前 Android、IL2CPP、ARM64、APK、最低 API 29 配置有效；PlayerSettings 文件相对备份仅 Active Input Handling 从 Both(2) 改为 Input System(1)。此项为此前 Android 构建配置，不应整体覆盖回备份。
- AndroidManifest 当前移除了 application 上无法在 unityLibrary 阶段解析的 label/icon 资源引用，由 launcher 提供；这是此前已验证构建所需修正，不应随恢复 Prefab 一并回退。
- 原场景与当前的 Eye Anchor 均引用缺失的 URP `UniversalAdditionalCameraData` GUID `a79441f348de89743a2939f4d699eac1`。两份都未声明 URP 包，当前使用 Built-in 管线。这是备份已有状态，非本次迁移新引入。保留 Anchor 对象；CenterEyeAnchor 的相机和业务头部位姿引用正在使用。
- 控制台仍保留一条 Meta Required 历史提示；直接执行当前 SDK 的实时必需检查，Android 32 项、Standalone 29 项均没有未完成项，不能将该旧日志单独视为当前配置失败。
- `Keyboard.prefab` 的全部文本行及出现次数一致，仅字段排列顺序不同；动态字体 `Anton SDF.asset` 的纹理/字形序列化结果变化；ProBuilder 编辑设置有差异；XR Simulation 新增临时资产且两个设置 GUID 变化。它们与缺失的 GhostGripper 分开记录，未作覆盖。

## 引用扫描的范围与限制

扫描两份工程的 Unity 文本资产和 ProjectSettings 中 GUID 引用，使用各自 Assets、Packages、Library/PackageCache 的 `.meta` 索引解析；不使用缓存替换源文件。当前扫描 371 个文本文件，两份均有 22 个无法在相应索引中解析的 GUID、312 处引用。未发现相对备份新增的未解析序列化引用，场景及 Prefab 内部 fileID 引用也未发现缺失目标。Resources 字符串加载不包含 GUID，所以 GhostGripper 的遗漏仍需通过文件清单和 Unity 加载结果发现，不能被“无新增 GUID 错误”掩盖。

上述未解析引用还包括两份共有的旧光照数据、URP 设置以及 TextMesh Pro 示例资源/材质编辑器引用。它们不等于当前业务场景有 312 个 Missing Script。本次针对已加载主场景的实测结果优先于文本候选；当前 Assets 内未发现重复 GUID。PackageCache 的 Samples~ 中存在重复示例 GUID，未将其算作 Assets 冲突。

本次完成的是完整性对比和当前 Editor 状态检查，没有重新验证 APK 构建、Quest 安装、UDP 数据收发及头显交互，因此“当前 0 条编译/控制台错误”不代表运行功能已与备份完全一致。

## 恢复顺序建议

先恢复 GhostGripper 及其 `.meta`，协调夹爪脚本和黑橙材质 GUID；再处理其余原始元数据与当前手动重绑引用的一致性、修正 Git 忽略规则。保留本机 SDK、MCP、AndroidManifest 和输入配置，保留用户当前显示参数，除非明确要求回到备份数值。随后检查夹爪 Prefab 的脚本、材质、左右夹爪 Transform，以及 Resources 加载；最后重新构建 APK 并进行真机验证。

完整证据位于 [Logs/ProjectAudit/2026-10-26](../Logs/ProjectAudit/2026-10-26)：文件清单及 SHA-256、文件差异、GUID 扫描、GUID 变化影响、包检查，以及 Unity 实时结果。恢复时可据此选择必要文件，无需覆盖整个工程。
