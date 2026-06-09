# Virtual Vehicle — 车辆碰撞虚拟仿真系统

基于 **Unity 2022.3 LTS** 与 **PhysX** 的车辆驾驶与碰撞仿真项目，面向虚拟现实课程实验。系统支持多辆可驾驶车辆、车辆间/车辆与环境碰撞、**运行时顶点形变**、碰撞视听反馈，以及运行时参数调节与 HUD 可视化。

---

## 目录

- [功能概览](#功能概览)
- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [详细启动步骤](#详细启动步骤)
- [操作说明](#操作说明)
- [项目结构](#项目结构)
- [核心系统说明](#核心系统说明)
- [编辑器工具菜单](#编辑器工具菜单)
- [场景与预制体](#场景与预制体)
- [第三方资源](#第三方资源)
- [常见问题](#常见问题)
- [相关文档](#相关文档)

---

## 功能概览

| 模块 | 说明 |
|------|------|
| **车辆驾驶** | `WheelCollider` 驱动，支持加速、转向、刹车、手刹；抓地系数、滚动阻力、空气阻力可配置 |
| **多车切换** | 场景中预设 **Car 1**、**Police 1**、**Taxi** 三辆可驾驶车辆，运行时按数字键切换 |
| **碰撞检测** | PhysX 刚体碰撞，连续动态检测（Continuous Dynamic），按冲量/速度过滤无效碰撞 |
| **车身形变** | 运行时顶点形变（方案 A），按部件（前杠、引擎盖、车门等）独立形变并同步 Collider |
| **损伤系统** | 累积损伤、做旧贴图、车辆状态（完好 → 受损 → 报废） |
| **环境交互** | 静态建筑/道路、可推动道具、可倾倒道具（树木、路灯、交通信号等） |
| **视听反馈** | 碰撞音效分级、粒子特效（烟雾/火焰/大火）、镜头抖动 |
| **UI / HUD** | 左下角实时 HUD（车速、档位、损伤、物理数据）；Tab 键打开配置面板 |
| **场景控制** | 慢动作、重置车辆、防卡死、参数预设、PlayerPrefs 持久化 |

---

## 环境要求

### 必需软件

| 项目 | 版本 / 说明 |
|------|-------------|
| **Unity Editor** | **2022.3.62f2c1**（LTS，与 `ProjectSettings/ProjectVersion.txt` 一致） |
| **Unity Hub** | 推荐安装，用于管理 Editor 版本 |
| **Git** | 用于克隆仓库（可选 LFS，视资源体积而定） |

> **版本提示：** 请使用与项目完全一致的 Unity 版本（`2022.3.62f2c1`）。版本不一致可能导致 Library 重建、脚本编译警告或资源兼容问题。

### 推荐硬件

| 项目 | 建议 |
|------|------|
| 操作系统 | Windows 10/11（64 位） |
| CPU | 四核及以上 |
| 内存 | 8 GB 及以上（首次导入建议 16 GB） |
| 显卡 | 支持 DirectX 11 的独立显卡 |
| 磁盘空间 | 至少 5 GB 可用空间（含 Library 缓存） |

### 渲染管线

本项目使用 **Built-in Render Pipeline**（内置渲染管线），**不是 URP/HDRP**。`Assets/download` 中部分第三方资源包含 URP 示例，不影响主场景运行。

---

## 快速开始

若你已安装 Unity Hub 与正确版本的 Editor，可按以下最短路径启动：

```bash
# 1. 克隆仓库
git clone git@github.com:lesentt/VirtualCar.git
cd VirtualCar

# 2. 用 Unity Hub 添加项目目录，选择 Unity 2022.3.62f2c1 打开

# 3. 在 Unity 中打开主场景
#    Assets/_Project/Scenes/Scene_1.unity

# 4. 点击 Editor 顶部的 Play ▶ 按钮进入运行模式
```

首次打开或场景异常时，建议额外执行一次编辑器菜单：**Tools → Virtual Vehicle → Setup Current Scene**（详见下文）。

---

## 详细启动步骤

### 第一步：获取项目

**方式 A — Git 克隆（推荐）**

```bash
git clone git@github.com:lesentt/VirtualCar.git Virtual_vehicle
cd Virtual_vehicle
```

HTTPS 地址：

```bash
git clone https://github.com/lesentt/VirtualCar.git Virtual_vehicle
```

**方式 B — 直接下载 ZIP**

从 GitHub 下载并解压到本地目录，例如 `D:\myUnity\Virtual_vehicle`。

### 第二步：安装 Unity Editor

1. 打开 **Unity Hub**
2. 进入 **Installs（安装）** → **Install Editor**
3. 选择 **Unity 2022.3 LTS**，并确保版本号为 **`2022.3.62f2c1`**
   - 若 Hub 中无此精确版本，可在 [Unity 下载归档页](https://unity.com/releases/editor/archive) 查找对应补丁版本
4. 安装模块建议勾选：
   - **Windows Build Support (IL2CPP)** 或 **Mono**（如需打包）
   - **Documentation**（可选）

### 第三步：在 Unity Hub 中添加并打开项目

1. Unity Hub → **Projects（项目）** → **Add（添加）**
2. 选择项目根目录（包含 `Assets`、`Packages`、`ProjectSettings` 的文件夹）
3. 确认 Hub 识别到 Unity 版本为 **2022.3.62f2c1**
4. 点击项目进入 Unity Editor

**首次打开说明：**

- Unity 会自动生成 `Library/`、`Temp/` 等目录（已在 `.gitignore` 中忽略，不会提交到 Git）
- 首次导入可能需要 **5–20 分钟**，取决于机器性能与磁盘速度
- 等待右下角进度条完成，Console 窗口无红色编译错误后再继续

### 第四步：打开主场景

主场景路径：

```
Assets/_Project/Scenes/Scene_1.unity
```

打开方式：

- **Project 窗口** 中双击 `Scene_1.unity`
- 或菜单 **File → Open Scene**，导航到上述路径

> **注意：** 当前 `EditorBuild Settings` 中未预置 Build 场景列表，**不会**自动加载 Scene_1。每次新开 Unity 后需手动打开该场景，或自行将其加入 Build Settings（File → Build Settings → Add Open Scenes）。

场景中已包含：

- 低多边形城市环境（SimplePoly City 等资产）
- 三辆可驾驶车辆：**Car 1**、**Police 1**、**Taxi**
- **Ui** 预制体实例（HUD + 配置面板）
- **CameraManager** 预制体实例（多车摄像机切换）

### 第五步（推荐）：一键配置当前场景

若是**第一次运行**、刚拉取最新代码、或遇到碰撞/形变/摄像机异常，请在 **Edit Mode（非 Play 模式）** 下执行：

```
Tools → Virtual Vehicle → Setup Current Scene
```

该菜单会自动完成：

1. 清理车辆上误加的 MeshCollider
2. 删除装饰用静态车辆（`Vehicle_*` 等）
3. 为环境物体补全 Collider 与碰撞分类
4. 升级并配置 **Car 1 / Police 1 / Taxi** 预制体（形变、碰撞、摄像机 Rig）
5. 创建/更新碰撞配置 ScriptableObject 资产
6. 启用车辆 Mesh 的 **Read/Write**
7. 烘焙车辆摄像机 Rig

完成后按提示 **Ctrl+S 保存场景**，再进入 Play 模式。

### 第六步：进入 Play 模式

1. 确认当前打开的是 `Scene_1.unity`
2. 点击 Unity Editor 顶部 **Play ▶** 按钮（或快捷键 **Ctrl+P**）
3. 游戏视图（Game 窗口）中应出现城市街道、可驾驶车辆与左下角 HUD

**运行时自动初始化（无需手动挂载）：**

项目通过 `[RuntimeInitializeOnLoadMethod]` 在场景加载后自动执行 `CollisionSceneSetup`，包括：

- 创建 `CollisionSystem` 根节点（碰撞管理、音效、特效、镜头抖动、场景重置）
- 配置车辆形变组件
- 清理装饰车辆、配置环境碰撞 Reporter

### 第七步：开始驾驶

Play 模式启动后：

1. 默认控制 **第一辆车（Car 1）**
2. 使用 **W/A/S/D** 或 **方向键** 驾驶
3. 按 **1 / 2 / 3** 切换三辆车
4. 按 **V** 切换第三人称 / 驾驶室第一人称视角
5. 按 **Tab** 打开系统配置面板

---

## 操作说明

### 键盘控制

| 按键 | 功能 |
|------|------|
| **W / ↑** | 加速 |
| **S / ↓** | 倒车 / 减速 |
| **A / ←** | 左转 |
| **D / →** | 右转 |
| **Space** | 刹车 |
| **Left Shift** | 手刹 / 急停 |
| **H** | 喇叭 |
| **1 / 2 / 3 / 4** | 切换当前控制车辆（按场景内车辆数量） |
| **V** | 切换第三人称 ↔ 驾驶室第一人称 |
| **Tab** | 打开 / 关闭系统配置面板 |

### HUD 信息（左下角）

运行时自动构建，显示内容包括：

- 当前车辆名称与视角模式
- 车速（km/h）、档位、油门开度
- 加速度（G）、接地轮数
- 损伤百分比与状态文字
- 驱动力、地面阻力、抓地系数、质量
- 碰撞次数与最近碰撞冲量

### 配置面板（Tab 键）

面板分为五个分类：

| 分类 | 可调节内容 |
|------|------------|
| **驾驶物理** | 驱动力矩、转向角、刹车/手刹力矩、抓地系数、滚动阻力、空气阻力、质量、重心等 |
| **形变损伤** | 形变阈值、深度、半径、衰减、损伤灵敏度、报废阈值等 |
| **视听反馈** | 镜头抖动、碰撞音效阈值与音量、粒子特效强度与火焰分级 |
| **环境道具** | 可倾倒道具（路灯/树木）的阈值与力度倍率 |
| **场景控制** | 慢动作时间缩放、HUD 显示开关、车辆切换按钮 |

面板底部按钮：

| 按钮 | 功能 |
|------|------|
| **恢复默认** | 将所有参数恢复为场景默认值 |
| **保存配置** | 写入 PlayerPrefs，下次启动自动加载 |
| **重置车辆** | 重置当前车辆形变、损伤与状态 |
| **防卡死** | 将当前车辆轻微抬起并后移，解决卡住问题 |

快速预设（驾驶物理页）：**经济型** / **运动型** / **警用型**。

---

## 项目结构

```
Virtual_vehicle/
├── Assets/
│   ├── _Project/                    # 项目自有代码与资源（核心）
│   │   ├── Scenes/
│   │   │   └── Scene_1.unity        # ★ 主场景（推荐从此启动）
│   │   ├── Scripts/
│   │   │   ├── Vehicle/             # 车辆驾驶、摄像机、切换
│   │   │   ├── Collision/           # 碰撞检测、损伤评估、环境分类
│   │   │   ├── Deformation/         # 顶点形变、Collider 同步、做旧
│   │   │   ├── Feedback/            # 音效、粒子、镜头抖动
│   │   │   ├── UI/                  # HUD、配置面板、运行时设置
│   │   │   └── Core/                # 引导、配置提供、场景重置
│   │   ├── Prefabs/                 # 车辆、UI、CameraManager、特效
│   │   ├── Resources/
│   │   │   └── CollisionConfig/     # 碰撞/形变/音效 ScriptableObject
│   │   ├── Materials/               # PBR 金属材质等
│   │   ├── Audio/                   # 引擎、碰撞、刹车、喇叭音效
│   │   └── Editor/                  # 编辑器菜单与一键配置工具
│   └── download/                    # 第三方下载资产（城市、道路、车辆包等）
├── doc/                             # 需求说明书与设计文档
├── Tools/                           # 辅助 Python 脚本（批量加 Collider 等）
├── Packages/
│   └── manifest.json                # Unity 包依赖
├── ProjectSettings/                 # Unity 项目设置
└── README.md                        # 本文件
```

### 主要脚本职责

| 脚本 | 职责 |
|------|------|
| `CarController` | 车辆输入、WheelCollider 驱动、阻力计算 |
| `VehicleState` | 损伤累积、状态管理、重置 |
| `CollisionManager` | 碰撞事件分发与记录 |
| `VehicleDeformationController` | 协调各部件形变 |
| `DeformablePart` / `VertexDeformer` | 单部件 Mesh 顶点位移 |
| `CollisionSceneSetup` | 场景加载后自动配置环境与碰撞 |
| `GameUIManager` | 车辆切换、HUD 刷新 |
| `SettingsUIController` | 运行时构建配置面板 |
| `GameSettingsManager` | 参数读写、预设、持久化 |
| `VehicleCameraController` | 多车摄像机与视角切换 |

---

## 核心系统说明

### 车辆列表与切换顺序

运行时按以下优先级自动发现车辆：

1. **Car 1** → 显示名 `car`
2. **Police 1** → 显示名 `police`
3. **Taxi** → 显示名 `taxi`

其余带 `CarController` 的对象按名称排序追加。切换车辆时会同步切换摄像机与配置面板中的当前车辆参数。

### 形变方案

采用 **运行时顶点形变**（非 Prefab 切换、非 Blend Shape）：

- 每辆车按部件拆分 Mesh（前杠、引擎盖、后备箱、四门等）
- 碰撞冲量超过阈值时，在命中区域对顶点做局部凹陷
- 形变后通过 `ColliderSyncService` 同步 BoxCollider
- 支持损伤做旧（Metal 材质贴图混合）

相关配置资产位于 `Assets/_Project/Resources/CollisionConfig/`。

### 环境碰撞分类

| 类别 | 典型对象 | 行为 |
|------|----------|------|
| **StaticImmovable** | 建筑、道路、地面 | 不可移动，高刚度 |
| **DynamicProp** | 灌木、垃圾桶、锥桶 | 可推动 |
| **DestructibleProp** | 树木、路灯、交通信号 | 可倾倒/破坏 |

分类由 `CollisionSceneSetup` 根据物体名称关键字自动完成。

---

## 编辑器工具菜单

所有自定义工具位于 Unity 顶部菜单 **Tools → Virtual Vehicle**：

| 菜单项 | 说明 |
|--------|------|
| **Setup Current Scene** | 一键配置当前场景（**首次运行强烈推荐**） |
| **Setup Taxi Prefab** | 单独生成/更新 Taxi 可驾驶预制体 |
| **Migrate Taxi Model (Scene + Prefab)** | 迁移 Taxi 模型到项目 Prefab |
| **Setup Camera Rigs (All Vehicle Prefabs)** | 为全部车辆 Prefab 烘焙摄像机 Rig |
| **Setup Camera Rigs (Current Scene)** | 仅为当前场景内车辆烘焙 Rig |

另有 **Virtual Vehicle → Wire Project Audio Clips** 用于绑定项目音频资源。

### 辅助 Python 工具

`Tools/add_simplepoly_colliders.py` — 批量为 SimplePoly City 预制体添加 MeshCollider（开发期使用，需 Python 3）。

---

## 场景与预制体

### 主场景

| 场景 | 路径 | 说明 |
|------|------|------|
| **Scene_1** ★ | `Assets/_Project/Scenes/Scene_1.unity` | 主实验场景，含完整城市与三辆车 |
| SampleScene | `Assets/_Project/Scenes/SampleScene.unity` | 备用/测试场景 |
| demo | `Assets/_Project/Scenes/demo.unity` | 演示场景 |

### 关键预制体

| 预制体 | 路径 |
|--------|------|
| Car 1 | `Assets/_Project/Prefabs/Car 1.prefab` |
| Police 1 | `Assets/_Project/Prefabs/Police 1.prefab` |
| Taxi | `Assets/_Project/Prefabs/Taxi.prefab` |
| UI | `Assets/_Project/Prefabs/Ui.prefab` |
| CameraManager | `Assets/_Project/Prefabs/CameraManager.prefab` |

---

## 第三方资源

项目位于 `Assets/download/`，主要包括：

| 资源包 | 用途 |
|--------|------|
| SimplePoly City - Low Poly Assets | 城市建筑、道路、环境道具 |
| BrokenVector LowPolyRoadPack | 道路预制体 |
| Unity Technologies CarsAssetPack | 车辆模型参考 |
| Bitgem StylisedWater | 水体着色器（Built-in 适配版在 `_Project/Shaders`） |
| VFXPACK_FIRE_WALLCOEUR | 火焰粒子特效 |
| High Matters Free American Sedans | 车辆材质参考 |

请遵守各 Asset Store / 资源作者的许可协议，勿将受版权保护的资源单独再分发。

---

## 常见问题

### Q1：打开项目后 Console 报编译错误

1. 确认 Unity 版本为 **2022.3.62f2c1**
2. 关闭 Unity，删除项目根目录下的 `Library/` 文件夹，重新打开项目让其重建
3. 等待 Package Manager 完成解析

### Q2：点击 Play 后没有 HUD / 无法切换车辆

1. 确认打开的是 **Scene_1.unity**（而非空场景或第三方 Demo 场景）
2. 检查 Hierarchy 中是否存在 **Ui** 与 **CameraManager** 实例
3. 执行 **Tools → Virtual Vehicle → Setup Current Scene** 并保存场景

### Q3：碰撞没有形变或形变异常

1. 执行 **Setup Current Scene** 以确保 Mesh Read/Write 已启用
2. 确认车辆 Prefab 上已挂载 `VehicleDeformationController` 与各 `DeformablePart`
3. 在配置面板中降低 **形变阈值（deformThreshold）** 以便更容易触发

### Q4：车辆穿透地面或卡住

- 按 **Tab** 打开配置面板，点击 **防卡死**
- 或点击 **重置车辆** 恢复形变与物理状态
- 检查场景中地面 Collider 是否完整（Setup Current Scene 会补环境 Collider）

### Q5：Camera 视角不对或切换车辆后黑屏

1. 执行 **Setup Camera Rigs (Current Scene)**
2. 确认 `CameraManager` 上的 `VehicleCameraController` 已关联车辆
3. 按 **V** 尝试切换第三人称 / 第一人称

### Q6：Play 模式很卡

- 在配置面板 → 场景控制 中降低时间缩放以外的负载：关闭部分 VFX、降低形变 `maxVerticesPerFrame`
- 在 Unity **Edit → Project Settings → Quality** 中降低画质等级

### Q7：如何打包发布？

1. **File → Build Settings**
2. 点击 **Add Open Scenes** 将 `Scene_1.unity` 加入列表
3. 选择目标平台（Windows 等）→ **Build**

---

## 相关文档

项目 `doc/` 目录包含更详细的设计说明：

| 文档 | 说明 |
|------|------|
| `doc/碰撞系统需求说明书.md` | 功能需求、验收标准、术语定义 |
| `doc/碰撞系统详细设计文档-Unity.md` | 架构设计、模块接口、形变方案、数据流 |

---

## 仓库信息

| 项目 | 内容 |
|------|------|
| 产品名称 | Virtual_vehicle |
| Unity 版本 | 2022.3.62f2c1 |
| 远程仓库 | [github.com/lesentt/VirtualCar](https://github.com/lesentt/VirtualCar) |
| 物理引擎 | Unity PhysX（内置） |
| 渲染管线 | Built-in Render Pipeline |

---

## 开发提示

- **不要提交** `Library/`、`Temp/`、`Logs/`、`UserSettings/` 等本地缓存（已在 `.gitignore` 中配置）
- 修改车辆 Prefab 或碰撞配置后，建议重新执行 **Setup Current Scene** 并保存
- 运行时配置可通过配置面板 **保存配置** 写入 PlayerPrefs（键名：`VirtualVehicle.RuntimeSettings`）
- 仅修改 `_Project` 下代码与资源即可；`Assets/download` 为第三方资产，尽量避免直接改动

---

如有问题，请先查看 Console 窗口日志（以 `[CollisionSceneSetup]`、`[GameUIManager]`、`[VirtualVehicleEditor]` 为前缀的条目），并对照本文「常见问题」章节排查。
