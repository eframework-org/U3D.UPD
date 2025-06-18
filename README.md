# EFramework Update for Unity

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.UPD)

EFramework Update for Unity 提供了一套完整的资源更新解决方案，包含补丁包和安装包的更新管理功能，通过业务处理接口控制更新流程，并采用事件机制驱动状态更新。

## 功能特性

- [XUpdate.Core](Documentation~/XUpdate.Core.md) 提供了更新流程控制和事件通知，实现了可扩展的业务处理接口
- [XUpdate.Patch](Documentation~/XUpdate.Patch.md) 实现了补丁包的提取、校验和下载功能，采用并发任务提升更新效率
- [XUpdate.Binary](Documentation~/XUpdate.Binary.md) 提供了安装包的更新功能，支持自动下载并解压安装

## 常见问题

更多问题，请查阅[问题反馈](CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](CHANGELOG.md)
- [贡献指南](CONTRIBUTING.md)
- [许可证](LICENSE.md)