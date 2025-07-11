# 更新记录

## [0.0.5] - 2025-07-08
### 修复
- 修复 Patch 模块多线程下载的日志输出异常

## [0.0.4] - 2025-07-08
### 变更
- 优化 Patch 模块中各阶段进度（Progress）的更新逻辑
- 优化 Patch 模块的解压和下载线程数量分配
- 优化 Patch 模块下载阶段的文件 IO，避免主线程卡顿

## [0.0.3] - 2025-06-18
### 变更
- 移除 XUpdate.Prefs 模块（业务层自行实现）
- 完善 XUpdate.Binary 的处理流程（Preprocess、Process、Postprocess）
- 修改 XUpdate.IHandler 的 OnCheck、OnRetry 函数签名，新增 IWorker 接口

### 新增
- 支持多引擎测试工作流
- 新增 [DeepWiki](https://deepwiki.com) 智能索引，方便开发者快速查找相关文档

## [0.0.2] - 2025-03-26
### 变更
- 更新依赖库版本

## [0.0.1] - 2025-03-23
### 新增
- 首次发布
