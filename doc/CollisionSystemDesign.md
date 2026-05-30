# 碰撞系统（精简版）

## 脚本结构

```
Assets/Scripts/Collision/
  CollisionTypes.cs           枚举 + 冲量计算 + 车辆识别
  CollisionSceneSetup.cs      场景自动配置 + Play 时初始化
  CollisionProfile.cs         挂环境物体（Inspector）
  DestructibleProp.cs           路灯/树倾倒（Inspector）
  VehicleCollisionHandler.cs    挂车辆（Inspector）

Assets/Editor/
  VirtualVehicleEditor.cs       唯一菜单入口
```

## 菜单

**Tools → Virtual Vehicle → Setup Current Scene**

一键：删装饰车、补 Collider、配置碰撞、设置 Car 1 / Police 1。

## 使用

1. 打开 Demo 场景  
2. **Setup Current Scene** → Ctrl+S  
3. 用 Car 1 / Police 1 测试  

## 调参

- 车辆反冲/损伤：`VehicleCollisionHandler`  
- 路灯难倒程度：`DestructibleProp` 的 `mass`、`toppleImpulseThreshold`
