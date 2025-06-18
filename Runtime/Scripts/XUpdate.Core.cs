// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EFramework.Utility;

namespace EFramework.Update
{
    /// <summary>
    /// XUpdate.Core 提供了更新流程控制和事件通知，实现了可扩展的业务处理接口。
    /// </summary>
    /// <remarks>
    /// <code>
    /// 功能特性
    /// - 流程控制：支持业务处理接口，提供版本检查和重试策略
    /// - 事件通知：覆盖更新全流程的事件回调，支持进度监控
    /// 
    /// 使用手册
    /// 1. 实现业务处理
    /// 
    /// public class MyHandler : XUpdate.IHandler
    /// {
    ///     private XUpdate.Binary binary;
    ///     private List&lt;XUpdate.Patch&gt; patches = new List&lt;XUpdate.Patch&gt;();
    ///     
    ///     // 获取安装包更新处理器
    ///     public XUpdate.Binary Binary =&gt; binary;
    ///     
    ///     // 获取补丁包更新处理器列表
    ///     public List&lt;XUpdate.Patch&gt; Patches =&gt; patches;
    ///     
    ///     // 检查更新
    ///     public bool OnCheck(out string version, out bool binary, out bool patch)
    ///     {
    ///         // 实现版本检查逻辑，例如：
    ///         version = "1.0.0";
    ///         binary = false;  // 是否进行安装包更新
    ///         patch = true;    // 是否进行补丁包更新
    ///         
    ///         // 如果需要更新补丁包，初始化补丁包处理器
    ///         if (patch)
    ///         {
    ///             var patcher = new XUpdate.Patch(
    ///                 "StreamingAssets/Patch.zip",  // 内置地址
    ///                 "Local/Patch/Manifest.db",              // 本地地址
    ///                 "https://example.com/Patch/Manifest.db"  // 远端地址
    ///             );
    ///             patches.Add(patcher);
    ///         }
    ///         
    ///         return binary || patch;  // 返回是否需要更新
    ///     }
    ///     
    ///     // 处理重试逻辑
    ///     public bool OnRetry(XUpdate.Phase phase, XUpdate.Patch patcher, int count, out float wait)
    ///     {
    ///         // 实现重试逻辑，例如：
    ///         wait = 1.0f;  // 重试等待时间（秒）
    ///         return count &lt; 3;  // 最多重试 3 次
    ///     }
    /// }
    /// 
    /// 2. 执行更新流程
    /// 
    /// // 创建流程处理器
    /// var handler = new MyHandler();
    /// 
    /// // 执行更新流程
    /// yield return XUpdate.Process(handler);
    /// 
    /// // 更新完成后的处理
    /// if (string.IsNullOrEmpty(handler.Patches[0].Error))
    /// {
    ///     Debug.Log("更新成功，可以继续游戏流程");
    /// }
    /// else
    /// {
    ///     Debug.LogError($"更新失败：{handler.Patches[0].Error}");
    /// }
    /// 
    /// 3. 监听更新事件
    /// 
    /// // 注册事件监听器
    /// XUpdate.Event.Reg(XUpdate.EventType.OnUpdateStart, OnUpdateStart);
    /// XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadUpdate, OnPatchDownloadUpdate);
    /// XUpdate.Event.Reg(XUpdate.EventType.OnUpdateFinish, OnUpdateFinish);
    /// 
    /// // 更新开始事件处理
    /// private void OnUpdateStart(object sender)
    /// {
    ///     Debug.Log("更新开始");
    /// }
    /// 
    /// // 补丁下载进度事件处理
    /// private void OnPatchDownloadUpdate(object sender)
    /// {
    ///     var patch = sender as XUpdate.Patch;
    ///     var progress = patch.Progress(XUpdate.Patch.Phase.Download);
    ///     var speed = patch.Speed(XUpdate.Patch.Phase.Download);
    ///     Debug.Log($"下载进度：{progress:P2}，速度：{speed / 1024} KB/s");
    /// }
    /// 
    /// // 更新完成事件处理
    /// private void OnUpdateFinish(object sender)
    /// {
    ///     Debug.Log("更新完成");
    /// }
    /// </code>
    /// 更多信息请参考模块文档。
    /// </remarks>
    public partial class XUpdate
    {
        /// <summary>
        /// 更新处理阶段枚举。
        /// </summary>
        public enum Phase
        {
            /// <summary>
            /// 预处理阶段，进行更新前的准备工作。
            /// </summary>
            Preprocess,

            /// <summary>
            /// 处理阶段，执行实际的更新操作。
            /// </summary>
            Process,

            /// <summary>
            /// 后处理阶段，完成更新后的清理工作。
            /// </summary>
            Postprocess,
        }

        /// <summary>
        /// 更新处理程序接口，定义了更新过程中需要实现的功能。
        /// </summary>
        public interface IHandler
        {
            /// <summary>
            /// 获取二进制更新处理器。
            /// </summary>
            Binary Binary { get; }

            /// <summary>
            /// 获取补丁更新处理器列表。
            /// </summary>
            List<Patch> Patches { get; }

            /// <summary>
            /// 检查更新的版本和状态。
            /// </summary>
            /// <param name="version">输出版本信息</param>
            /// <param name="binary">输出是否进行安装包更新</param>
            /// <param name="patch">输出是否进行补丁包更新</param>
            /// <returns>返回是否需要更新</returns>
            bool OnCheck(out string version, out bool binary, out bool patch);

            /// <summary>
            /// 处理更新失败时的重试逻辑。
            /// </summary>
            /// <param name="phase">当前处理阶段</param>
            /// <param name="patcher">当前补丁处理器</param>
            /// <param name="count">已重试次数</param>
            /// <param name="wait">输出重试等待时间（秒）</param>
            /// <returns>返回是否继续重试</returns>
            bool OnRetry(Phase phase, Patch patcher, int count, out float wait);
        }

        /// <summary>
        /// 更新事件类型枚举，定义了更新过程中的所有事件。
        /// </summary>
        public enum EventType
        {
            /// <summary>
            /// 更新流程开始。
            /// </summary>
            OnUpdateStart,

            /// <summary>
            /// 安装包更新流程开始。
            /// </summary>
            OnBinaryUpdateStart,

            /// <summary>
            /// 安装包文件下载开始。
            /// </summary>
            OnBinaryDownloadStart,

            /// <summary>
            /// 安装包文件下载进度更新。
            /// </summary>
            OnBinaryDownloadUpdate,

            /// <summary>
            /// 安装包文件下载成功。
            /// </summary>
            OnBinaryDownloadSucceed,

            /// <summary>
            /// 安装包文件下载失败。
            /// </summary>
            OnBinaryDownloadFailed,

            /// <summary>
            /// 安装包文件提取开始。
            /// </summary>
            OnBinaryExtractStart,

            /// <summary>
            /// 安装包文件提取进度更新。
            /// </summary>
            OnBinaryExtractUpdate,

            /// <summary>
            /// 安装包文件提取成功。
            /// </summary>
            OnBinaryExtractSucceed,

            /// <summary>
            /// 安装包文件提取失败。
            /// </summary>
            OnBinaryExtractFailed,

            /// <summary>
            /// 安装包文件安装开始。
            /// </summary>
            OnBinaryInstallStart,

            /// <summary>
            /// 安装包文件安装进度更新。
            /// </summary>
            OnBinaryInstallUpdate,

            /// <summary>
            /// 安装包文件安装成功。
            /// </summary>
            OnBinaryInstallSucceed,

            /// <summary>
            /// 安装包文件安装失败。
            /// </summary>
            OnBinaryInstallFailed,

            /// <summary>
            /// 安装包更新流程完成。
            /// </summary>
            OnBinaryUpdateFinish,

            /// <summary>
            /// 补丁包更新流程开始。
            /// </summary>
            OnPatchUpdateStart,

            /// <summary>
            /// 补丁包文件提取开始。
            /// </summary>
            OnPatchExtractStart,

            /// <summary>
            /// 补丁包文件提取进度更新。
            /// </summary>
            OnPatchExtractUpdate,

            /// <summary>
            /// 补丁包文件提取成功。
            /// </summary>
            OnPatchExtractSucceed,

            /// <summary>
            /// 补丁包文件提取失败。
            /// </summary>
            OnPatchExtractFailed,

            /// <summary>
            /// 补丁包文件验证开始。
            /// </summary>
            OnPatchValidateStart,

            /// <summary>
            /// 补丁包文件验证进度更新。
            /// </summary>
            OnPatchValidateUpdate,

            /// <summary>
            /// 补丁包文件验证成功。
            /// </summary>
            OnPatchValidateSucceed,

            /// <summary>
            /// 补丁包文件验证失败。
            /// </summary>
            OnPatchValidateFailed,

            /// <summary>
            /// 补丁包文件下载开始。
            /// </summary>
            OnPatchDownloadStart,

            /// <summary>
            /// 补丁包文件下载进度更新。
            /// </summary>
            OnPatchDownloadUpdate,

            /// <summary>
            /// 补丁包文件下载成功。
            /// </summary>
            OnPatchDownloadSucceed,

            /// <summary>
            /// 补丁包文件下载失败。
            /// </summary>
            OnPatchDownloadFailed,

            /// <summary>
            /// 补丁包更新流程完成。
            /// </summary>
            OnPatchUpdateFinish,

            /// <summary>
            /// 更新流程完成。
            /// </summary>
            OnUpdateFinish,
        }

        /// <summary>
        /// 事件管理器实例，用于处理更新过程中的事件通知。
        /// </summary>
        public static readonly XEvent.Manager Event = new();

        /// <summary>
        /// 处理更新的主方法，协调整个更新流程的执行。
        /// </summary>
        /// <param name="handler">更新处理程序实例</param>
        /// <returns>返回一个协程</returns>
        /// <exception cref="ArgumentNullException">处理程序为空时抛出</exception>
        public static IEnumerator Process(IHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Event.Notify(EventType.OnUpdateStart);

            if (handler.OnCheck(out string version, out var binary, out var patch))
            {
                XLog.Notice("XUpdate.Process: start to process update binary: {0}, patch: {1}.", binary, patch);
                if (binary)
                {
                    Event.Notify(EventType.OnBinaryUpdateStart);
                    // TODO
                    Event.Notify(EventType.OnBinaryUpdateFinish);
                    XLog.Notice("XUpdate.Process: process update binary finish.");
                }
                else if (patch)
                {
                    Event.Notify(EventType.OnPatchUpdateStart);
                    if (handler.Patches == null || handler.Patches.Count == 0)
                    {
                        Event.Notify(EventType.OnPatchUpdateFinish);
                        Event.Notify(EventType.OnUpdateFinish);
                        XLog.Notice("XUpdate.Process: process update patch finish with no patcher.");
                    }
                    else
                    {
                        #region Preprocess
                        var executeCount = 0;
                        Patch lpatcher = null;
                        for (var i = 0; i < handler.Patches.Count;)
                        {
                            var patcher = handler.Patches[i];
                            if (lpatcher != patcher)
                            {
                                lpatcher = patcher;
                                executeCount = 1;
                            }
                            else executeCount++;
                            yield return patcher.Preprocess(patch);
                            if (string.IsNullOrEmpty(patcher.Error) == false)
                            {
                                if (handler.OnRetry(Phase.Preprocess, patcher, executeCount, out var wait)) yield return new WaitForSeconds(wait);
                                else yield break;
                            }
                            else i++;
                        }
                        #endregion

                        #region Process
                        executeCount = 0;
                        lpatcher = null;
                        for (var i = 0; i < handler.Patches.Count;)
                        {
                            var patcher = handler.Patches[i];
                            if (lpatcher != patcher)
                            {
                                lpatcher = patcher;
                                executeCount = 1;
                            }
                            else executeCount++;
                            yield return patcher.Process();
                            if (string.IsNullOrEmpty(patcher.Error) == false)
                            {
                                if (handler.OnRetry(Phase.Process, patcher, executeCount, out var wait)) yield return new WaitForSeconds(wait);
                                else yield break;
                            }
                            else i++;
                        }
                        #endregion

                        #region Postprocess
                        executeCount = 0;
                        lpatcher = null;
                        for (var i = 0; i < handler.Patches.Count;)
                        {
                            var patcher = handler.Patches[i];
                            if (lpatcher != patcher)
                            {
                                lpatcher = patcher;
                                executeCount = 1;
                            }
                            else executeCount++;
                            yield return patcher.Postprocess();
                            if (string.IsNullOrEmpty(patcher.Error) == false)
                            {
                                if (handler.OnRetry(Phase.Postprocess, patcher, executeCount, out var wait)) yield return new WaitForSeconds(wait);
                                else yield break;
                            }
                            else i++;
                        }
                        #endregion

                        var manis = new Dictionary<XMani.Manifest, XMani.DiffInfo>();
                        foreach (var tpatch in handler.Patches) manis.Add(tpatch.RemoteMani, tpatch.DiffInfo);
                        Event.Notify(EventType.OnPatchUpdateFinish, manis);
                        XLog.Notice("XUpdate.Process: process update patch finish.");
                    }
                }

                Event.Notify(EventType.OnUpdateFinish);
                XLog.Notice("XUpdate.Process: finish to process update binary: {0}, patch: {1}.", binary, patch);
            }
        }
    }
}
