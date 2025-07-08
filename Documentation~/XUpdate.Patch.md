# XUpdate.Patch

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.UPD)

XUpdate.Patch 实现了补丁包的提取、校验和下载功能，采用并发任务提升更新效率。

## 功能特性

- 并发校验：使用多线程进行 MD5 校验，基于文件大小负载均衡
- 并行下载：支持多协程竞争下载，自动处理断点续传
- 进度追踪：实时监控各阶段的处理进度和速度
- 事件通知：提供覆盖补丁处理全流程的事件回调

## 使用手册

### 1. 初始化

```csharp
// 创建处理器实例
var patch = new XUpdate.Patch(
    assetUrl,  // 内置地址，用于提取补丁文件
    localUrl,  // 本地地址，用于存储补丁文件
    remoteUrl  // 远端地址，用于下载补丁文件
);
```

### 2. 处理流程

```csharp
// 1. 预处理补丁，提取内置补丁包、读取清单和校验文件
yield return patch.Preprocess(true);  // true 表示进行远端比较
if (!string.IsNullOrEmpty(patch.Error)) 
{
    Debug.LogError($"预处理失败：{patch.Error}");
    yield break;
}

// 2. 处理补丁，下载新增和修改的文件
yield return patch.Process();
if (!string.IsNullOrEmpty(patch.Error))
{
    Debug.LogError($"处理失败：{patch.Error}");
    yield break;
}

// 3. 后处理补丁，清理已删除的文件
yield return patch.Postprocess();
```

### 3. 进度追踪

```csharp
// 获取指定阶段的文件总大小（字节）
long extractSize = patch.Size(XUpdate.Patch.Phase.Extract);
long validateSize = patch.Size(XUpdate.Patch.Phase.Validate);
long downloadSize = patch.Size(XUpdate.Patch.Phase.Download);

// 获取指定阶段的处理进度（0-1）
float extractProgress = patch.Progress(XUpdate.Patch.Phase.Extract);
float validateProgress = patch.Progress(XUpdate.Patch.Phase.Validate);
float downloadProgress = patch.Progress(XUpdate.Patch.Phase.Download);

// 获取指定阶段的处理速度（字节/秒）
long extractSpeed = patch.Speed(XUpdate.Patch.Phase.Extract);
long validateSpeed = patch.Speed(XUpdate.Patch.Phase.Validate);
long downloadSpeed = patch.Speed(XUpdate.Patch.Phase.Download);
```

## 常见问题

### 1. 文件校验性能如何？

使用并行计算 MD5 并按照文件大小负载均衡，校验速度极快，
并发数量为 `Math.Max(2, SystemInfo.processorCount / 4)`，过多线程可能导致资源竞态。

### 2. 文件下载性能如何？

使用竞争下载策略且异步写入磁盘，理论下载速度可以达到网络环境的上限，
并发数量为 `Math.Max(2, SystemInfo.processorCount / 4)`，需要考虑文件大小、网络带宽、内存溢出等因素。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
