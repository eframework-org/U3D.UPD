# XUpdate.Core

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)

XUpdate.Core 提供了更新流程控制和事件通知，实现了可扩展的业务处理接口。

## 功能特性

- 流程控制：支持业务处理接口，提供版本检查和重试策略
- 事件通知：覆盖更新全流程的事件回调，支持进度监控

## 使用手册

### 1. 实现业务处理

更新流程需要提供业务处理器，该处理器实现自 `XUpdate.IHandler` 接口：

```csharp
public class MyHandler : XUpdate.IHandler
{
    private XUpdate.Binary binary;
    private List<XUpdate.Patch> patches = new List<XUpdate.Patch>();
    
    // 获取安装包更新处理器
    public XUpdate.Binary Binary => binary;
    
    // 获取补丁包更新处理器列表
    public List<XUpdate.Patch> Patches => patches;
    
    // 检查更新
    public bool OnCheck(out string version, out bool binary, out bool patch)
    {
        // 实现版本检查逻辑，例如：
        version = "1.0.0";
        binary = false;  // 是否进行安装包更新
        patch = true;    // 是否进行补丁包更新
        
        // 如果需要更新补丁包，初始化补丁包处理器
        if (patch)
        {
            var patcher = new XUpdate.Patch(
                "StreamingAssets/Patch.zip",  // 内置地址
                "Local/Patch/Manifest.md5",              // 本地地址
                "https://example.com/Patch/Manifest.md5"  // 远端地址
            );
            patches.Add(patcher);
        }
        
        return binary || patch;  // 返回是否需要更新
    }
    
    // 处理重试逻辑
    public bool OnRetry(XUpdate.Phase phase, XUpdate.Patch patcher, int count, out float wait)
    {
        // 实现重试逻辑，例如：
        wait = 1.0f;  // 重试等待时间（秒）
        return count < 3;  // 最多重试 3 次
    }
}
```

### 2. 执行更新流程

在游戏启动或需要更新的地方，调用 `XUpdate.Process` 方法执行更新流程：

```csharp
// 创建流程处理器
var handler = new MyHandler();

// 执行更新流程
yield return XUpdate.Process(handler);

// 更新完成后的处理
if (string.IsNullOrEmpty(handler.Patches[0].Error))
{
    Debug.Log("更新成功，可以继续游戏流程");
}
else
{
    Debug.LogError($"更新失败：{handler.Patches[0].Error}");
}
```

更新流程的状态转换如下图所示：

```mermaid
stateDiagram-v2
    direction LR
    [*] --> 开始更新: XUpdate.Process
    开始更新 --> 版本检查: handler.OnCheck
    版本检查 --> 无需更新: 返回false
    版本检查 --> 补丁包更新: patch=true
    版本检查 --> 安装包更新: binary=true
    
    state 补丁包更新 {
        direction LR

        state 预处理 {
            [*] --> 读取本地清单
            读取本地清单 --> 提取内置文件: 本地清单异常
            读取本地清单 --> 读取远端清单: 本地清单正常
            提取内置文件 --> 读取远端清单: 成功
            读取远端清单 --> 比较差异: 成功
            比较差异 --> 校验文件: 成功
            校验文件 --> 准备下载列表: 有更新
            校验文件 --> [*]: 无更新
            准备下载列表 --> [*]
        }
        
        state 处理 {
            [*] --> 检查下载列表
            检查下载列表 --> 并行下载文件: 有文件
            检查下载列表 --> [*]: 无文件
            并行下载文件 --> 更新本地清单: 成功
            更新本地清单 --> [*]
        }
        
        state 后处理 {
            [*] --> 检查删除列表
            检查删除列表 --> 清理文件: 有删除
            检查删除列表 --> [*]: 无删除
            清理文件 --> [*]: 成功
        }

        预处理 --> 处理: 成功
        处理 --> 后处理: 成功
        后处理 --> [*]: 成功

        预处理 --> 重试判断: 失败
        处理 --> 重试判断: 失败
        后处理 --> 重试判断: 失败
        
        重试判断 --> 等待重试: handler.OnRetry返回true
        重试判断 --> 更新失败: handler.OnRetry返回false
        
        等待重试 --> 预处理
        等待重试 --> 处理
        等待重试 --> 后处理
    }

    state 安装包更新 {
        direction LR
        [*] --> 下载文件
        下载文件 --> 提取文件: 成功
        提取文件 --> 安装文件: 成功
        安装文件 --> [*]: 成功
    }
    
    补丁包更新 --> 更新完成: OnPatchUpdateFinish
    安装包更新 --> 更新完成: OnBinaryUpdateFinish
    无需更新 --> [*]
    更新完成 --> [*]: OnUpdateFinish
    更新失败 --> [*]
```

### 3. 监听更新事件

可以通过 `XUpdate.Event` 注册事件监听器，获取更新进度和状态：

```csharp
// 注册事件监听器
XUpdate.Event.Reg(XUpdate.EventType.OnUpdateStart, OnUpdateStart);
XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadUpdate, OnPatchDownloadUpdate);
XUpdate.Event.Reg(XUpdate.EventType.OnUpdateFinish, OnUpdateFinish);

// 更新开始事件处理
private void OnUpdateStart(object sender)
{
    Debug.Log("更新开始");
}

// 补丁下载进度事件处理
private void OnPatchDownloadUpdate(object sender)
{
    var patch = sender as XUpdate.Patch;
    var progress = patch.Progress(XUpdate.Patch.Phase.Download);
    var speed = patch.Speed(XUpdate.Patch.Phase.Download);
    Debug.Log($"下载进度：{progress:P2}，速度：{speed / 1024} KB/s");
}

// 更新完成事件处理
private void OnUpdateFinish(object sender)
{
    Debug.Log("更新完成");
}
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)