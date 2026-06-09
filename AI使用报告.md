# Virtual Vehicle 项目 AI 辅助开发使用报告

**小组**：五人  
**项目**：Unity 虚拟车辆碰撞演示（`Assets/_Project/`）  
**主场景**：`Scene_1.unity`，三辆车可切换（Car 1、Police 1、Taxi）

---

## 写在前面

这份报告是按我们实际写代码的过程整理的，不是事后编的故事。项目里能看到的 `FormerlySerializedAs("frictionStiffness")`、`DestroyLegacyHudElements()`、`MigrateLegacyCameraTransform()` 这些痕迹，基本都能对应到某次 AI 给错方向、我们改过来的经历。

我们主要用 Cursor 写代码，偶尔用 ChatGPT 查 Unity 物理相关的文档。流程大概是：一个人提需求 → AI 出第一版 → 自己 Play 一下 → 不对就改 prompt 或者干脆手改。

---

## 项目是怎么搭起来的

最开始没有想那么复杂，就是「能开车、撞了要有反应」。后来慢慢拆成五块：

- 开车（WheelCollider）
- 碰撞检测 + 事件分发
- 车身凹进去（顶点形变）
- 声音、粒子、镜头抖
- HUD、设置面板、树和路灯能倒

各块之间靠 `CollisionEventChannel`（一个 ScriptableObject）传消息，不互相直接拖引用。这个设计是成员二定的，之前 AI 给的是让 `CollisionManager` 维护一个 `List<ICollisionListener>`，场景一换就全丢了，后来才改成 SO 事件通道。

Play 的时候会自动跑 `CollisionSceneSetup`，补碰撞体、删装饰车、给环境分类。Editor 里有个菜单 `Tools/Virtual Vehicle/Setup Current Scene`，点一下做同样的事，不用每次手动挂组件。

---

## 成员一：车辆控制

**负责目录**：`Scripts/Vehicle/`

### 一开始

第一周的目标很简单：WASD 能开、Space 刹车、Shift 手刹。把需求丢给 AI，它出了 `CarController.cs` 的架子，四轮 `WheelCollider`、读输入、给轮子加力矩，这部分大体能用。

### 抓地和阻力搞混了

AI 第一版把「抓地」「滚动阻力」「空气阻力」都往 `WheelCollider.frictionStiffness` 里塞。开起来要么像冰面，要么松油门几乎不停。我们对着 Unity 文档查了一晚上，最后拆成三个字段：

- `gripCoefficient` — 写进 WheelCollider，管打滑
- `rollingResistanceCoeff` — `AddForce` 模拟滚动阻力
- `airDragCoefficient` — 速度平方阻力

字段改名后还留了 `[FormerlySerializedAs("frictionStiffness")]`，Prefab 才没丢数据。

### 高速发飘

`maxSteerAngle` 固定 30° 时，速度一上 60 km/h 车就甩。AI 建议加「速度越快转向越小」，我们试了几组系数，最后定在 50 km/h 以上舵角收到 45% 左右，手感才正常。

### 三车切换和摄像机

Car 1、Police 1 是完整 Prefab；Taxi 后来才加进来，只有模型没有轮子。成员一让 AI 写了 `DriveableVehicleBuilder`，运行时自动加 `Drive_Wheels`、四个 `WheelCollider`、`Rigidbody`、`CarController`。

Taxi 调参踩坑最多：`centerOfMassY` AI 给 0.5，急转弯必翻，改到 `-10` 才稳住（写在 `TaxiProfile` 里，看起来离谱但能用）。AI 还建议后驱，Taxi 后轴 mesh 命名对不上，最后还是前驱 + 把 `gripCoefficient` 调到 2.8。

摄像机也折腾过。AI 第一版是让 Main Camera 改 `parent` 跟着车走，Police 1 pivot 偏，镜头经常插进车顶。后来每辆车挂 `VehicleCameraRig`，下面分 `ThirdPersonAnchor` 和 `CockpitAnchor`，V 键切换。旧场景里还有乱挂的 Camera，加了 `MigrateLegacyCameraTransform()` 做迁移。

### 小结

成员一这边 AI 大概帮了六成初稿，但所有「好不好开」的参数都是手调的。AI 对 WheelCollider 的直觉经常不对，不能信默认值。

---

## 成员二：碰撞检测与事件

**负责目录**：`Scripts/Collision/`、`Scripts/Core/`

### 事件怎么传

成员二的工作是把「撞了」这件事变成结构化数据，别人订阅就行。AI 生成的 `CollisionReporter`（挂碰撞体，`OnCollisionEnter` 上报）和 `CollisionEventData`（时间、冲量、接触点、材质类型等）结构基本保留。

`CollisionManager` 是单例，`DefaultExecutionOrder(-100)` 保证比 Reporter 先起来。收到上报后：过滤 → 算冲量 → 调形变 → 更新 `VehicleState` → 写 `CollisionEventRecorder` → `eventChannel.Raise(evt)`。

### 冲量算错是最头疼的

这块 AI 帮倒忙最多。它一开始就用 `relativeVelocity.magnitude * mass`，结果是：

- 慢慢蹭护栏，冲量虚高，音效形变全触发
- 高速正撞，反而算低了

我们对着 PhysX 的 contact impulse 试了很多次，最后写成两套：

- 车：`ComputeVehicleImpulse()`，接触冲量和动量估算取大的，低速再衰减
- 环境：`ComputeImpulse()`，优先用接触冲量
- 撞固定建筑：有效质量当 10000 kg

还加了 `minImpulse = 300` 过滤轻碰，这个值后来接到成员五的设置面板里，调参方便很多。

### 场景里一堆乱七八糟的物体

SimplePoly City 资产包物体命名不统一，不可能一个个手动挂脚本。AI 写了按名字关键词分类的逻辑（在 `CollisionSceneSetup` 里）：

- 名字带 tree、lamp、traffic → 可倾倒，挂 `DestructibleProp`
- bush、trash、stone → 动态道具，Kinematic 刚体
- building、road → 静态环境

AI 还建议给所有东西加 `MeshCollider`，很多物体根本没 Mesh，Console 全红。改成有 Renderer 才补 `BoxCollider`，可倾倒的用 Convex MeshCollider。

### 装饰车误删

场景里有一批 `Vehicle_*` 开头的静态装饰车，和可玩的车混在一起。Play 时会删掉它们。有一版 AI 写的判断条件太宽，差点把 Taxi 也删了（名字里带 vehicle）。后来加了排除：已经有 `CarController` 的不删。

### 损伤百分比

`DamageEvaluator` 是 AI 起草、成员二手改的。早期线性公式导致「轻蹭一下损伤条跳 30%」。加了 `ScaleImpulseForDamage()`，低速用 cubic 缩放，和成员三的形变深度一起算，才比较像样。

### 小结

事件通道和 Reporter 模式 AI 写得还行；冲量、Layer、环境分类这几块基本靠人肉试。成员二说：「AI 给的物理公式能跑，但不像游戏。」

---

## 成员三：网格形变

**负责目录**：`Scripts/Deformation/`、`Shaders/VehicleBodyWear.shader`

### 绝对不能改 sharedMesh

第一次 AI 生成的形变代码直接改 `meshFilter.sharedMesh.vertices`。在 Editor 里试了两下，FBX 资产被改脏了，队友骂了一顿。正确做法是 `Instantiate` 一份运行时 Mesh，标记 `MarkDynamic()`，只改副本。`DeformablePart` 里还有 Read/Write 检查，没勾 import 设置会直接报错。

这块是全员教训最深的：**AI 不懂 Unity 资产管线**。

### 车门撞穿

早期算法对所有顶点沿法线内推。薄板车门会出现「正面凹、背面鼓」的穿模。成员三自己加了 `TryGetImpactSurface()`，用三角形最近点找撞击面，只推一侧；`ResolveInwardNormal()` 结合 mesh 中心校正方向。注释里写的「设计文档 §6.2」是开工前小组一起定的形变规范，AI 按规范改了好几轮才对。

### Collider 跟着变

AI 建议形变后重建 `MeshCollider`，Play 一下 FPS 直接掉。最后 `ColliderSyncService` 用 `BoxCollider` 粗略压扁，精度差一截但跑得动。

### 部件怎么认

AI 提议「按 Bounds 自动分引擎盖、车门」——在 Police 1 上把警灯识别成 Hood。行不通。改成硬编码子物体名字：`Car_hood`、`Police_door_FL`、`Taxi_bumper` 等，`VehicleDeformationSetup` 里三张映射表。

磨损是后面加的：`PartWearApplicator` 往 `VehicleBodyWear.shader` 写 stamp，最多 32 个。Shader 第一版 AI 混了 Built-in 和 URP 语法，成员四帮忙改了一版能用的。

### 小结

形变是 AI 帮最少的一块，大概四成代码直接用，算法和几何都要人盯。成员三原话：「形变这块 AI 只能当计算器，不能当设计师。」

---

## 成员四：视听反馈

**负责目录**：`Scripts/Feedback/`

### 碰撞音效

订阅 `CollisionEventChannel`，按 `SurfaceA/B`（metalConcrete、metalPlant 等）和冲量档位选 clip。8 个 `AudioSource` 轮着播，避免互相掐断。

AI 一开始把刮擦和撞击都写在 `OnCollisionEnter` 里，蹭一下护栏播重撞声。刮擦改走 `OnCollisionStay` → `ReportScrape()`，再在 `LateUpdate` 里 `EndScrapeFrame()` 做衰减，和 Enter 分开。

资源在 `Assets/_Project/Audio/`，Editor 脚本 `ProjectAudioSetup` 自动绑到 ScriptableObject。缺 clip 时 AI 写了个程序化生成的 Fallback（正弦+噪声），第一版全是 440Hz，听着像同一声，后来按冲量改频率。

### 引擎声

`VehicleAudioController` 五档 RPM 切换 idle 到 maxRpm，只有玩家当前控制的车才播。AI 建议上 AudioMixer Snapshot，我们嫌重，直接调 `AudioSource.volume`。

### 粒子和镜头

`CollisionVFXManager` 按冲量分级：300 冒烟、4000 火、10000 大火；车身损伤超 50% 在身上挂持续火焰。预制体 `smoke`、`fire`、`big fire` 在 `Prefabs/` 下。

`CameraShakeController` 只响应玩家开的那辆车。AI 第一版统一用 `position += Random`，第一人称必晕。改成第三人称抖位置、第一人称抖旋转。

树倒下时的音效在 `DestructibleProp.Topple()` 里直接调 `CollisionAudioManager.PlayOneShotAt()`，没走事件通道，图省事。

### 小结

视听这块 AI 出力最大，结构基本能采纳，主要是阈值和 clip 要试。成员四花最多时间的是找免费音效和调 `lightImpulseThreshold`。

---

## 成员五：UI 与可破坏环境

**负责目录**：`Scripts/UI/`、`DestructibleProp`、`SceneResetService`

### UI 推翻重来

AI 第一版做的是 UGUI Prefab：Canvas 下挂一堆 Slider、Text。设置项越来越多（五个 Tab：驾驶、形变、反馈、环境、会话），改一个布局要动 Prefab，Merge 经常冲突。

第二版干脆 **运行时用代码生成 UI**——`VehicleHudController.Build()` 画左下角 HUD，`SettingsUIController` 动态建面板。代码一千多行，丑但好改。`GameUIManager` 里 `DestroyLegacyHudElements()` 就是删第一版 AI 生成的 `Text_`、`Slider_` 残留。

`GameSettingsManager` 是配置中枢：Slider 一改，`ApplyAll()` 同步到 `CarController`、`DeformationConfig`、`CollisionManager.minImpulse`、音量、VFX、可倾倒物阈值、`Time.timeScale`。配置存 `PlayerPrefs`，JSON 序列化。Economy / Sport / Police 三套预设是 AI 生成的数据类，数值我们调的。

HUD 读 `CarController` 的车速档位、`VehicleState` 的损伤、`CollisionEventRecorder` 的最近冲量。

### 树和路灯怎么「破坏」

`Prefabs/Damaged/` 里有损坏版树、路灯，AI 建议撞了 `Instantiate` 替换预制体。试过后两个问题：切换时机对不准、没有倒下过程，像切模型。

最后做 **Kinematic 倾倒**：平时固定，冲量够了 `Topple()` 解除 Kinematic，改 Layer 到 `Debris`，在接触点加力矩。倒下来是 PhysX 算的，比换 Prefab 有戏。`CollisionManager` 会忽略已倒物体（`IsFallenEnvironmentObject`），避免反复触发形变。

AI 给的 `toppleImpulseThreshold = 500`，树几乎不倒，按 `PropKind` 分了 Tree、Lamp、TrafficSignal 不同默认值。

### 场景重置

Session Tab 里重置按钮调 `SceneResetService`：车辆状态、形变 Mesh、倒下道具、碰撞记录清掉。第一版 AI 漏了 mesh 顶点恢复，联调时成员三补的。

### 小结

UI 架构是「AI 提方案 → 我们否决 → 再让 AI 按新方案写」的典型。环境破坏是小组集体拍板 PhysX 倾倒，不是 AI 推荐的方案。

---

## 联调时几件印象深的事

**「一蹭就凹」**  
20 km/h 擦护栏就有坑。五个人轮流改：成员二升 `minImpulse`、改冲量衰减；成员三升 `deformThreshold`；成员二再改 `DamageEvaluator` 低速缩放。最后成员五在设置面板里边开边调，才定下来一组能接受的默认值。

**EventChannel 空引用**  
成员四说 Play 没声音。`CollisionAudioManager` 在 `Start` 订阅时 `CollisionManager.Instance` 偶尔还没好。加了 Bootstrap 启动顺序和 `CollisionConfigProvider` 懒加载 SO 才好。

**MeshCollider 误加**  
AI 或早期脚本给车身子物体加了 MeshCollider，和 WheelCollider 打架，车会弹飞。`VirtualVehicleEditor` 在 Play 前自动清，运行时 `CleanupAllPlayerVehicles()` 也清。Console 里「已清理车辆误加碰撞体 N 个」我们见很多次。

**Taxi 迁移**  
Taxi 模型换过好几版，`TaxiModelMigrator` 是 Editor 菜单，专门替换场景里的旧 Taxi 并跑 `DriveableVehicleBuilder.EnsureTaxi()`。这部分完全是迭代产物，不是一开始就设计好的。

---

## 用下来的一些体会

1. **先定 `CollisionEventData` 里有哪些字段，再分头让 AI 写。** 不然成员四的 `SurfaceA/B` 和成员二的解析对不上，联调浪费半天。

2. **涉及 PhysX 和 mesh 的，AI 代码默认只能当草稿。** 形变、冲量、COM 都要 Play 了才知道。

3. **Prompt 里写死路径和约束有用。** 比如「代码放 `Assets/_Project/Scripts/`，Inspector 用 `[LabelText]` 中文，别直接改 sharedMesh」——能少踩一半坑。

4. **Editor 一键脚本 worth it。** `Setup Current Scene` 六步（清碰撞体、删装饰车、补环境 Collider、配车辆形变、建 SO、开 Read/Write） saves 大量重复劳动，这菜单本身是 AI 帮写后我们改的。

5. **Legacy 清理函数不要删。** 它们就是开发记录，说明方案换过。

---

## 附录：谁写了哪块（文件对照）

| 成员 | 主要文件 |
|------|----------|
| 成员一 | `CarController.cs`、`DriveableVehicleBuilder.cs`、`VehicleCameraRig.cs` |
| 成员二 | `CollisionManager.cs`、`CollisionTypes.cs`、`CollisionSceneSetup.cs`、`CollisionEventChannel.cs` |
| 成员三 | `DeformablePart.cs`、`VertexDeformer.cs`、`VehicleBodyWear.shader` |
| 成员四 | `CollisionAudioManager.cs`、`VehicleAudioController.cs`、`CollisionVFXManager.cs` |
| 成员五 | `GameSettingsManager.cs`、`SettingsUIController.cs`、`VehicleHudController.cs`、`DestructibleProp.cs` |
| 共用 | `VirtualVehicleEditor.cs`、`CollisionSystemBootstrap.cs` |

---

## 结语

整个项目大概两三个月（含调参和换 Taxi 模型），AI 大概承担了 half 左右的「第一遍代码」，但能不能玩、会不会穿模、手感好不好，还是靠五人反复 Play、互相甩锅、改 prompt 或手改。

如果只做演示、不上生产，这种「AI 起草 + 人工纠偏」够用了。形变和物理两块别指望 AI 一次做对，留时间联调比什么都要紧。
