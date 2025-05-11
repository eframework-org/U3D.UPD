# XUpdate.Prefs

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.upd)](https://www.npmjs.com/package/org.eframework.u3d.upd)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.UPD)

XUpdate.Prefs 管理更新系统的配置项，支持运行时读取和编辑器可视化配置。

## 功能特性

- 支持版本配置管理：提供版本号和更新检查等基础配置
- 实现更新服务配置：支持补丁包和安装包访问服务器地址和路径设置
- 提供发布选项管理：支持补丁包发布服务器地址和密钥配置
- 实现可视化配置界面：在 Unity 编辑器中提供直观的设置面板

## 使用手册

### 1. 版本控制

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| 版本号 | `Update/Version` | - | - `XPrefs.Local` 中的配置值为本地的版本号<br>- `XPrefs.Remote` 中的配置值为远端的版本号<br>- 基于上述两个版本号进行版本更新检查 |
| 跳过检查 | `Update/SkipCheck` | `false` | - 控制是否跳过更新检查<br>- 启用后将不进行更新检查<br>- 用于开发环境调试 |
| 跳过更新 | `Update/SkipVersion` | `false` | - 控制是否跳过版本更新<br>- 启用后将不进行版本更新<br>- 仅在开发环境下使用 |
| 更新白名单 | `Update/WhiteList` | - | - 更新白名单列表<br>- 控制允许更新的版本范围<br>- 用于版本更新的权限控制 |

### 2. 安装包配置

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| 安装包地址 | `Update/BinaryUrl` | - | 安装包文件的下载地址 |
| 安装包大小 | `Update/BinarySize` | - | 安装包文件的下载大小 |

### 3. 补丁包配置

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| 补丁包地址 | `Update/PatchHost` | `${Env.OssPublic}` | 补丁包访问服务器的地址 |
| 补丁包路径 | `Update/PatchUri` | `Builds/Patch/${Env.Solution}/`<br>`${Env.Author}/${Env.Channel}/`<br>`${Env.Platform}/${Env.Version}` | 补丁包访问服务器的路径 |

### 4. 发布配置

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| 发布地址 | `Update/Patch/Publish/Host@Editor` | `${Env.OssHost}` | - 补丁发布服务器的地址<br>- 仅在编辑器环境下可用 |
| 发布存储 | `Update/Patch/Publish/Bucket@Editor` | `${Env.OssBucket}` | - 补丁发布服务器的存储桶<br>- 仅在编辑器环境下可用 |
| 访问密钥 | `Update/Patch/Publish/Access@Editor` | `${Env.OssAccess}` | - 补丁发布服务器的访问密钥<br>- 仅在编辑器环境下可用 |
| 秘密密钥 | `Update/Patch/Publish/Secret@Editor` | `${Env.OssSecret}` | - 补丁发布服务器的秘密密钥<br>- 仅在编辑器环境下可用 |

以上配置项均可在 `Tools/EFramework/Preferences/Update` 首选项编辑器中进行可视化配置。

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)