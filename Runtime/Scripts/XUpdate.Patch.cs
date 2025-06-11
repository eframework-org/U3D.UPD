// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using EFramework.Utility;

namespace EFramework.Update
{
    public partial class XUpdate
    {
        /// <summary>
        /// XUpdate.Patch 实现了补丁包的提取、校验和下载功能，采用并发任务提升更新效率。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 并发校验：使用多线程进行 MD5 校验，基于文件大小负载均衡
        /// - 并行下载：支持多协程竞争下载，自动处理断点续传
        /// - 进度追踪：实时监控各阶段的处理进度和速度
        /// - 事件通知：提供覆盖补丁处理全流程的事件回调
        /// 
        /// 使用手册
        /// 1. 初始化
        ///     // 创建处理器实例
        ///     var patch = new XUpdate.Patch(
        ///         assetUrl,  // 内置地址，用于提取补丁文件
        ///         localUrl,  // 本地地址，用于存储补丁文件
        ///         remoteUrl  // 远端地址，用于下载补丁文件
        ///     );
        /// 
        /// 2. 处理流程
        ///     // 1. 预处理补丁，提取内置补丁包、读取清单和校验文件
        ///     yield return patch.Preprocess(true);  // true 表示进行远端比较
        ///     if (!string.IsNullOrEmpty(patch.Error)) 
        ///     {
        ///         Debug.LogError($"预处理失败：{patch.Error}");
        ///         yield break;
        ///     }
        /// 
        ///     // 2. 处理补丁，下载新增和修改的文件
        ///     yield return patch.Process();
        ///     if (!string.IsNullOrEmpty(patch.Error))
        ///     {
        ///         Debug.LogError($"处理失败：{patch.Error}");
        ///         yield break;
        ///     }
        /// 
        ///     // 3. 后处理补丁，清理已删除的文件
        ///     yield return patch.Postprocess();
        /// 
        /// 3. 进度追踪
        ///     // 获取指定阶段的文件总大小（字节）
        ///     long extractSize = patch.Size(XUpdate.Patch.Phase.Extract);
        ///     long validateSize = patch.Size(XUpdate.Patch.Phase.Validate);
        ///     long downloadSize = patch.Size(XUpdate.Patch.Phase.Download);
        /// 
        ///     // 获取指定阶段的处理进度（0-1）
        ///     float extractProgress = patch.Progress(XUpdate.Patch.Phase.Extract);
        ///     float validateProgress = patch.Progress(XUpdate.Patch.Phase.Validate);
        ///     float downloadProgress = patch.Progress(XUpdate.Patch.Phase.Download);
        /// 
        ///     // 获取指定阶段的处理速度（字节/秒）
        ///     long extractSpeed = patch.Speed(XUpdate.Patch.Phase.Extract);
        ///     long validateSpeed = patch.Speed(XUpdate.Patch.Phase.Validate);
        ///     long downloadSpeed = patch.Speed(XUpdate.Patch.Phase.Download);
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Patch
        {
            /// <summary>
            /// 补丁处理阶段枚举。
            /// </summary>
            public enum Phase
            {
                /// <summary>
                /// 提取阶段，从资源包中提取补丁文件。
                /// </summary>
                Extract,

                /// <summary>
                /// 校验阶段，校验本地文件的完整性。
                /// </summary>
                Validate,

                /// <summary>
                /// 下载阶段，从远端服务器下载补丁文件。
                /// </summary>
                Download,
            }

            /// <summary>
            /// 内置地址。
            /// </summary>
            protected string assetUrl;

            /// <summary>
            /// 本地地址。
            /// </summary>
            protected string localUrl;

            /// <summary>
            /// 远端地址。
            /// </summary>
            protected string remoteUrl;

            /// <summary>
            /// 本地清单，用于记录本地文件信息。
            /// </summary>
            public XMani.Manifest LocalMani;

            /// <summary>
            /// 远端清单，用于记录远端文件信息。
            /// </summary>
            public XMani.Manifest RemoteMani;

            /// <summary>
            /// 差异信息，记录本地和远端文件的差异。
            /// </summary>
            public XMani.DiffInfo DiffInfo;

            /// <summary>
            /// 待下载文件列表。
            /// </summary>
            protected List<XMani.FileInfo> downloads;

            /// <summary>
            /// 各阶段的文件大小记录。
            /// </summary>
            protected readonly Dictionary<Phase, long> sizes = new();

            /// <summary>
            /// 获取指定阶段的文件总大小。
            /// </summary>
            /// <param name="phase">处理阶段</param>
            /// <returns>返回指定阶段的文件总大小（字节）</returns>
            public virtual long Size(Phase phase) { sizes.TryGetValue(phase, out var size); return size; }

            /// <summary>
            /// 各阶段的处理进度记录。
            /// </summary>
            protected readonly Dictionary<Phase, float> progresses = new();

            /// <summary>
            /// 获取指定阶段的处理进度。
            /// </summary>
            /// <param name="phase">处理阶段</param>
            /// <returns>返回指定阶段的处理进度（0-1）</returns>
            public virtual float Progress(Phase phase) { progresses.TryGetValue(phase, out var progress); return progress; }

            /// <summary>
            /// 进度更新周期（秒）。
            /// </summary>
            protected const float UpdatePeriod = 0.5f;

            /// <summary>
            /// 速度更新周期（秒）。
            /// </summary>
            protected const float SpeedPeriod = 0.5f;

            /// <summary>
            /// 各阶段的速度计算时间记录。
            /// </summary>
            protected readonly Dictionary<Phase, float> speedTimes = new();

            /// <summary>
            /// 各阶段的上次处理大小记录。
            /// </summary>
            protected readonly Dictionary<Phase, long> lastSizes = new();

            /// <summary>
            /// 各阶段的处理速度记录。
            /// </summary>
            protected readonly Dictionary<Phase, long> speeds = new();

            /// <summary>
            /// 获取指定阶段的处理速度。
            /// </summary>
            /// <param name="phase">处理阶段</param>
            /// <returns>返回指定阶段的处理速度（字节/秒）</returns>
            public virtual long Speed(Phase phase)
            {
                speeds.TryGetValue(phase, out var speed);
                speedTimes.TryGetValue(phase, out var time);
                var deltaTime = Time.realtimeSinceStartup - time;
                if (deltaTime > SpeedPeriod)
                {
                    speedTimes[phase] = Time.realtimeSinceStartup;
                    lastSizes.TryGetValue(phase, out var last);
                    var current = (long)(Size(phase) * Progress(phase));
                    if (last > 0 && current > last) speed = (long)((current - last) / deltaTime);
                    lastSizes[phase] = current;
                    speeds[phase] = speed;
                }
                return speed;
            }

            /// <summary>
            /// 错误信息。
            /// </summary>
            public virtual string Error { get; set; }

            /// <summary>
            /// 初始化补丁处理类。
            /// </summary>
            /// <param name="assetUrl">内置地址，用于提取补丁文件</param>
            /// <param name="localUrl">本地地址，用于存储补丁文件</param>
            /// <param name="remoteUrl">远端地址，用于下载补丁文件</param>
            public Patch(string assetUrl, string localUrl, string remoteUrl)
            {
                this.assetUrl = XFile.NormalizePath(assetUrl);
                this.localUrl = XFile.NormalizePath(localUrl);
                this.remoteUrl = new Uri(remoteUrl).AbsoluteUri;
            }

            /// <summary>
            /// 预处理补丁，提取内置补丁包、读取清单和校验文件。
            /// </summary>
            /// <param name="remote">是否进行远端比较</param>
            /// <returns>返回一个协程</returns>
            public virtual IEnumerator Preprocess(bool remote)
            {
                Error = string.Empty;
                LocalMani = new XMani.Manifest(localUrl);
                yield return new WaitUntil(LocalMani.Read());
                if (string.IsNullOrEmpty(LocalMani.Error) == false)
                {
                    if (XFile.HasFile(assetUrl))
                    {
                        yield return Extract();
                        if (string.IsNullOrEmpty(Error) == false) yield break;
                        yield return new WaitUntil(LocalMani.Read());
                    }
                }

                if (remote == false)
                {
                    if (string.IsNullOrEmpty(LocalMani.Error) == false) // 如果不对比远端，且本地文件异常，则中断流程
                    {
                        Error = LocalMani.Error;
                        yield break;
                    }
                }
                else
                {
                    RemoteMani = new XMani.Manifest(remoteUrl);
                    yield return new WaitUntil(RemoteMani.Read());
                    if (string.IsNullOrEmpty(RemoteMani.Error) == false)
                    {
                        Error = RemoteMani.Error;
                        yield break;
                    }

                    DiffInfo = LocalMani.Compare(RemoteMani);
                    yield return new WaitUntil(Validate());
                    if (string.IsNullOrEmpty(Error) == false) yield break;

                    if (DiffInfo.Added.Count > 0 || DiffInfo.Modified.Count > 0)
                    {
                        downloads = new List<XMani.FileInfo>();
                        downloads.AddRange(DiffInfo.Added);
                        downloads.AddRange(DiffInfo.Modified);
                    }
                }
            }

            /// <summary>
            /// 处理补丁，主要是下载新增和修改的文件。
            /// </summary>
            /// <returns>返回一个协程</returns>
            public virtual IEnumerator Process()
            {
                Error = string.Empty;
                if (downloads != null && downloads.Count > 0) yield return Download();
            }

            /// <summary>
            /// 后处理补丁，主要是清理已删除的文件。
            /// </summary>
            /// <returns>返回一个协程</returns>
            public virtual IEnumerator Postprocess()
            {
                Error = string.Empty;
                if (DiffInfo != null && DiffInfo.Deleted.Count > 0) yield return new WaitUntil(Cleanup());
            }

            /// <summary>
            /// 提取补丁，从内置补丁包中提取补丁文件。
            /// </summary>
            /// <returns>返回一个协程</returns>
            protected virtual IEnumerator Extract()
            {
                Error = string.Empty;
                var time = XTime.GetMillisecond();
                var done = false;

                sizes[Phase.Extract] = XFile.FileSize(assetUrl);

                try
                {
                    var localRoot = Path.GetDirectoryName(localUrl);
                    XLog.Notice("XUpdate.Patch.Extract: start to extract <a href=\"file:///{0}\">{1}</a> into <a href=\"file:///{2}\">{3}</a>.", Path.GetFullPath(assetUrl), assetUrl, Path.GetFullPath(localRoot), localRoot);
                    if (XFile.HasDirectory(localRoot) == false) XFile.CreateDirectory(localRoot);
                    Event.Notify(EventType.OnPatchExtractStart, this);

                    var ptime = 0f;
                    XFile.Unzip(assetUrl, localRoot, () => done = true, (err) =>
                    {
                        XLog.Error("XUpdate.Patch.Extract: extract with error: {0}.", err);
                        Error = err;
                        done = true;
                    }, (progress) => XLoom.RunInMain(() =>
                    {
                        if (done) return; // 避免解压完成后再次回调，导致事件时序异常
                        progresses[Phase.Extract] = progress;
                        if (Time.realtimeSinceStartup - ptime > UpdatePeriod || progress >= 1f) // 避免频繁回调（解压的回调频次不高，也可以不作处理）
                        {
                            ptime = Time.realtimeSinceStartup;
                            Event.Notify(EventType.OnPatchExtractUpdate, this);
                        }
                    }));
                }
                catch (Exception e) { XLog.Panic(e); Error = e.Message; done = true; }

                yield return new WaitUntil(() => done);

                if (string.IsNullOrEmpty(Error))
                {
                    if (Progress(Phase.Extract) < 1f)
                    {
                        progresses[Phase.Extract] = 1f;
                        Event.Notify(EventType.OnPatchExtractUpdate, this);
                    }

                    Event.Notify(EventType.OnPatchExtractSucceed, this);
                    XLog.Notice("XUpdate.Patch.Extract: finsh to extract, elapsed: {0}ms.", XTime.GetMillisecond() - time);
                }
                else
                {
                    Event.Notify(EventType.OnPatchExtractFailed, this);
                    XLog.Error("XUpdate.Patch.Extract: finsh to extract with error: {0}, elapsed: {1}ms.", Error, XTime.GetMillisecond() - time);
                }
            }

            /// <summary>
            /// 校验补丁，校验本地文件的完整性。
            /// </summary>
            /// <returns>返回一个校验函数</returns>
            protected virtual Func<bool> Validate()
            {
                Error = string.Empty;
                var time = XTime.GetMillisecond();

                var localRoot = Path.GetDirectoryName(localUrl);

                XLog.Notice("XUpdate.Patch.Validate: start to validate <a href=\"file:///{0}\">{1}</a>.", Path.GetFullPath(localRoot), localRoot);

                var ptime = 0f;
                long current = 0;
                long record = 0;
                long total = 0;
                var tasks = 0;
                var dones = new List<bool>();
                var md5s = new Dictionary<string, string>();
                try
                {
                    if (XFile.HasDirectory(localRoot) == false) XLog.Notice("XUpdate.Patch.Validate: <a href=\"file:///{0}\">{1}</a> doesn't exist.", Path.GetFullPath(localRoot), localRoot);
                    else
                    {
                        var files = Directory.GetFiles(localRoot).ToList();
                        total = files.Count;
                        sizes[Phase.Validate] = total;
                        if (files.Count > 0)
                        {
                            var thread = 20; // 过多线程无益，资源竞态效应（可以根据不同的平台设置不同的值，如Windows/Android/iOS）
                            var avg = files.Count / thread;
                            var last = avg <= 1 ? files.Count : files.Count % avg;
                            tasks = avg <= 1 ? 1 : files.Count / avg;
                            ThreadPool.QueueUserWorkItem((_) => // 避免从主线程衍生过多子线程引起的卡顿
                            {
                                var stime = XTime.GetMillisecond();
                                var sizes = new Dictionary<string, long>();
                                long getFileSize(string file)
                                {
                                    if (sizes.TryGetValue(file, out var size)) return size;
                                    var name = Path.GetFileName(file);
                                    if (LocalMani != null) // 使用manifest缓存值进行优化
                                    {
                                        foreach (var fi in LocalMani.Files)
                                        {
                                            if (fi.Name == name)
                                            {
                                                size = fi.Size;
                                                break;
                                            }
                                        }
                                    }
                                    if (size == 0) size = new FileInfo(file).Length; // 使用文件系统记录值
                                    sizes.Add(file, size);
                                    return size;
                                }
                                //foreach (var file in files) getFileSize(file); // for debug

                                // 文件大小排序
                                files.Sort((e1, e2) =>
                                {
                                    var s1 = getFileSize(e1);
                                    var s2 = getFileSize(e2);
                                    return s2.CompareTo(s1);
                                });
                                XLog.Notice("XUpdate.Patch.Validate: sort {0} local file(s), elapsed: {1}ms.", total, XTime.GetMillisecond() - stime);

                                var workers = new List<List<string>>();
                                for (var i = 0; i < tasks; i++) workers.Add(new List<string>());
                                for (var i = 0; i < files.Count; i++) workers[i % tasks].Add(files[i]); // 线程平均分布，负载均衡

                                for (var i = 0; i < workers.Count; i++)
                                {
                                    var num = i;
                                    var worker = workers[i];
                                    ThreadPool.QueueUserWorkItem((_) =>  // FileMD5高耗时，异步避免ANR，多线程提高对比速度
                                    {
                                        // 大小文件交错，各线程错峰执行
                                        var rnd = new System.Random();
                                        int n = worker.Count;
                                        while (n > 1)
                                        {
                                            n--;
                                            int k = rnd.Next(n + 1);
                                            var value = worker[k];
                                            worker[k] = worker[n];
                                            worker[n] = value;
                                        }

                                        //long fsize = 0; // for debug
                                        //var wtime = XTime.GetMillisecond();
                                        var tmps = new Dictionary<string, string>();
                                        foreach (var file in worker)
                                        {
                                            //sizes.TryGetValue(file, out var size);
                                            //fsize += size;
                                            var md5 = XFile.FileMD5(file);
                                            Interlocked.Increment(ref current); // 减少lock的次数
                                            tmps[Path.GetFileName(file)] = md5;
                                        }
                                        lock (dones)
                                        {
                                            foreach (var kvp in tmps) md5s[kvp.Key] = kvp.Value;
                                            dones.Add(true);
                                            //XLog.Info("Patcher.Validate: worker-{0} process {1} byte(s) of {2} file(s), elapsed {2}ms", num, fsize, worker.Count, XTime.GetMillisecond() - wtime);
                                        }
                                    });
                                }
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    XLog.Panic(e);
                }

                Event.Notify(EventType.OnPatchValidateStart, this);

                return new Func<bool>(() =>
                {
                    if (total > 0 && record != current)
                    {
                        record = current;
                        var progress = current * 1f / total;
                        progresses[Phase.Validate] = progress;

                        if (Time.realtimeSinceStartup - ptime > UpdatePeriod || progress >= 1f) // 避免频繁回调
                        {
                            ptime = Time.realtimeSinceStartup;
                            Event.Notify(EventType.OnPatchValidateUpdate, this);
                        }
                    }
                    if (dones.Count < tasks && string.IsNullOrEmpty(Error)) return false;

                    if (string.IsNullOrEmpty(Error))
                    {
                        for (var i = DiffInfo.Added.Count - 1; i >= 0; i--)
                        {
                            var fi = DiffInfo.Added[i];
                            if (md5s.TryGetValue(fi.Name, out var md5) && md5 == fi.MD5)
                            {
                                DiffInfo.Added.RemoveAt(i);
                                XLog.Info("XUpdate.Patch.Validate: local file's md5 equals to remote: {0}", fi.Name);
                            }
                        }
                        for (var i = DiffInfo.Modified.Count - 1; i >= 0; i--)
                        {
                            var fi = DiffInfo.Modified[i];
                            if (md5s.TryGetValue(fi.Name, out var md5) && md5 == fi.MD5)
                            {
                                DiffInfo.Modified.RemoveAt(i);
                                XLog.Info("XUpdate.Patch.Validate: local file's md5 equals to remote: {0}", fi.Name);
                            }
                        }
                        foreach (var fi in LocalMani.Files)
                        {
                            md5s.TryGetValue(fi.Name, out var md5);
                            var delete = false;
                            foreach (var dfi in DiffInfo.Deleted)
                            {
                                if (dfi.Name == fi.Name) { delete = true; break; } // 忽略即将删除的文件
                            }
                            if (delete == false && md5 != fi.MD5) // 本地文件不存在
                            {
                                foreach (var rfi in RemoteMani.Files)
                                {
                                    if (rfi.Name == fi.Name) // 远端存在该文件
                                    {
                                        DiffInfo.Added.Add(rfi);
                                        XLog.Notice("XUpdate.Patch.Validate: local file's md5 doesn't equals to manifest: {0}", rfi.Name);
                                        break;
                                    }
                                }
                            }
                        }

                        long dsize = 0;
                        foreach (var fi in DiffInfo.Added) dsize += fi.Size;
                        foreach (var fi in DiffInfo.Modified) dsize += fi.Size;
                        sizes[Phase.Download] = dsize;

                        if (Progress(Phase.Validate) < 1f)
                        {
                            progresses[Phase.Validate] = 1f;
                            Event.Notify(EventType.OnPatchValidateUpdate, this);
                        }

                        Event.Notify(EventType.OnPatchValidateSucceed, this);
                        XLog.Notice("XUpdate.Patch.Validate: validated {0} local file(s)' {1} md5(s) by {2} task(s), elapsed: {3}ms.", total, md5s.Count, tasks, XTime.GetMillisecond() - time);
                    }
                    else
                    {
                        Event.Notify(EventType.OnPatchValidateFailed, this);
                        XLog.Error("XUpdate.Patch.Validate: validate with error: {0}, elapsed: {1}ms.", Error, XTime.GetMillisecond() - time);
                    }
                    return true;
                });
            }

            /// <summary>
            /// 下载补丁，从远端服务器下载补丁文件。
            /// </summary>
            /// <returns>返回一个协程</returns>
            protected virtual IEnumerator Download()
            {
                Error = string.Empty;
                var thread = 5; // 过多协程无益，考虑文件大小、网络带宽、内存溢出等因素

                var time = XTime.GetTimestamp();
                var localRoot = Path.GetDirectoryName(localUrl);
                var remoteRoot = remoteUrl.Replace(Path.GetFileName(remoteUrl), "");
                XLog.Notice("XUpdate.Patch.Download: start to download from <a href=\"{0}\">{1}</a> into <a href=\"file:///{2}\">{3}</a>.", remoteRoot, remoteRoot, Path.GetFullPath(localRoot), localRoot);
                if (!XFile.HasDirectory(localRoot)) XFile.CreateDirectory(localRoot);

                Event.Notify(EventType.OnPatchDownloadStart, this);

                if (Size(Phase.Download) > 0) // 进度回档
                {
                    var tsize = Size(Phase.Download);
                    long usize = 0;
                    foreach (var fi in downloads) usize += fi.Size;
                    var progress = Progress(Phase.Download);
                    var nprogress = (tsize - usize) * 1f / tsize;
                    if (progress > nprogress)
                    {
                        progresses[Phase.Download] = nprogress;
                        XLog.Notice("XUpdate.Patch.Download: revert progress from {0} to {1}", progress, nprogress);
                        Event.Notify(EventType.OnPatchDownloadUpdate, this);
                    }
                }

                var done = 0;
                var total = downloads.Count;
                var reqs = new Dictionary<XMani.FileInfo, UnityWebRequest>();
                var times = new Dictionary<XMani.FileInfo, long>();
                var succeeded = new List<XMani.FileInfo>();
                var csize = (long)(Size(Phase.Download) * Progress(Phase.Download));
                var lsize = csize;
                var ptime = 0f;
                while (done < total && string.IsNullOrEmpty(Error))
                {
                    long tsize = csize;
                    try
                    {
                        succeeded.Clear();
                        foreach (var kvp in reqs)
                        {
                            var fi = kvp.Key;
                            var req = kvp.Value;
                            if (req.isDone || !string.IsNullOrEmpty(req.error))
                            {
                                if (!string.IsNullOrEmpty(req.error))
                                {
                                    if (string.IsNullOrEmpty(Error) == false) Error += "\n";
                                    Error += $"Download {req.uri} error: {req.error}";
                                }
                                else
                                {
                                    done++;
                                    succeeded.Add(fi);
                                    csize += fi.Size;
                                    tsize += fi.Size;
                                    var file = Path.Join(localRoot, fi.Name);
                                    XFile.SaveFile(file, req.downloadHandler.data);
                                    if (XLog.Able(XLog.LevelType.Notice))
                                    {
                                        var ttime = XTime.GetMillisecond() - times[fi];
                                        //var etime = ttime < 0 ? "NaN" : (ttime < 1000 ? $"{ttime}ms" : (ttime < 60000 ? $"{ttime / 60000}s" : $"{ttime / 60000}min {ttime % 60000}s"));
                                        var etime = ttime < 0 ? "NaN" : (ttime < 1000 ? $"{ttime}ms" : (ttime < 60000 ? $"{ttime / 1000.0:0.00}s" : $"{Math.Floor(ttime / 60000.0)}min {ttime % 60000 / 1000}s"));
                                        var dsize = req.downloadHandler.data.Length;
                                        var ssize = dsize < 1024 * 1024 ? $"{dsize / 1024}kb" : $"{dsize / (1024 * 1024f):0.00}mb";
                                        XLog.Notice("XUpdate.Patch.Download: download {0} into {1}, elapsed {2}.", ssize, fi.Name, etime);
                                    }
                                }
                            }
                            else tsize += (long)(fi.Size * req.downloadProgress);
                        }

                        if (tsize > lsize) // 避免进度回档
                        {
                            lsize = tsize;
                            var progress = 0f;
                            var ttsize = Size(Phase.Download);
                            if (ttsize > 0) progress = tsize * 1f / ttsize;
                            progresses[Phase.Download] = progress;
                            if (Time.realtimeSinceStartup - ptime > UpdatePeriod || progress >= 1f) // 避免频繁回调
                            {
                                ptime = Time.realtimeSinceStartup;
                                Event.Notify(EventType.OnPatchDownloadUpdate, this);
                            }
                        }

                        if (succeeded.Count > 0)
                        {
                            foreach (var key in succeeded)
                            {
                                var req = reqs[key];
                                reqs.Remove(key);
                                times.Remove(key);
                                try { req.Dispose(); } catch (Exception e) { XLog.Panic(e); }
                            }
                        }

                        if (string.IsNullOrEmpty(Error))
                        {
                            if (reqs.Count < thread && downloads.Count > 0)
                            {
                                var rnd = new System.Random();
                                var idx = rnd.Next(downloads.Count);
                                var fi = downloads[idx]; // 随机下载文件，使得大小负载均衡
                                downloads.RemoveAt(idx);
                                var url = $"{remoteRoot}{fi.Name}@{fi.MD5}";
                                var req = UnityWebRequest.Get(url);
                                //req.timeout = 10; // 不可以设置超时时间，否则大文件无法下载
                                req.SendWebRequest();
                                reqs.Add(fi, req);
                                times.Add(fi, XTime.GetMillisecond());
                                if (XLog.Able(XLog.LevelType.Info))
                                {
                                    var ssize = fi.Size < 1024 * 1024 ? $"{fi.Size / 1024}kb" : $"{fi.Size / (1024 * 1024f):0.00}mb";
                                    XLog.Info("XUpdate.Patch.Download: download {0} from {1}.", ssize, fi.Name);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        XLog.Panic(e);
                    }
                    finally
                    {
                        if (string.IsNullOrEmpty(Error) == false)
                        {
                            if (reqs.Count > 0) // 释放资源
                            {
                                foreach (var kvp in reqs)
                                {
                                    downloads.Add(kvp.Key); // 重新下载
                                    XLog.Notice("XUpdate.Patch.Download: dispose request of <a href=\"{0}\">{1}</a>.", kvp.Value.uri, kvp.Value.uri);
                                    try { kvp.Value.Dispose(); } catch (Exception e) { XLog.Panic(e); }
                                }
                            }
                        }
                    }
                    yield return 0;
                }

                var rtime = XTime.GetTimestamp() - time;
                var stime = rtime < 0 ? "NaN" : (rtime < 60 ? $"{rtime}s" : $"{rtime / 60}min {rtime % 60}s");
                if (string.IsNullOrEmpty(Error))
                {
                    if (Progress(Phase.Download) < 1f)
                    {
                        progresses[Phase.Download] = 1f;
                        Event.Notify(EventType.OnPatchDownloadUpdate, this);
                    }

                    XFile.SaveText(localUrl, RemoteMani.ToString());
                    Event.Notify(EventType.OnPatchDownloadSucceed, this);
                    XLog.Notice("XUpdate.Patch.Download: download {0} file(s) done, elapsed: {1}.", total, stime);
                }
                else
                {
                    Event.Notify(EventType.OnPatchDownloadFailed, this);
                    XLog.Error("XUpdate.Patch.Download: download with error: {0}, elapsed: {1}.", Error, stime);
                }
            }

            /// <summary>
            /// 清理补丁，删除已标记为删除的文件。
            /// </summary>
            /// <returns>返回一个清理函数</returns>
            protected virtual Func<bool> Cleanup()
            {
                Error = string.Empty;

                var localRoot = Path.GetDirectoryName(localUrl);
                XLog.Notice("XUpdate.Patch.Cleanup: start to cleanup deleted file(s) at <a href=\"file:///{0}\">{1}</a>.", Path.GetFullPath(localRoot), localRoot);
                var done = false;
                ThreadPool.QueueUserWorkItem((_) =>
                {
                    try
                    {
                        foreach (var fi in DiffInfo.Deleted)
                        {
                            var fs = XFile.PathJoin(localRoot, fi.Name);
                            if (XFile.HasFile(fs))
                            {
                                XLog.Notice("XUpdate.Patch.Cleanup: delete {0}.", fs);
                                XFile.DeleteFile(fs);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        XLog.Panic(e);
                    }
                    finally { done = true; }

                    if (string.IsNullOrEmpty(Error)) XLog.Notice("XUpdate.Patch.Cleanup: finsh to cleanup deleted file(s).");
                    else XLog.Error("XUpdate.Patch.Cleanup: finsh to cleanup deleted file(s) with error: {0}", Error);
                });

                return new Func<bool>(() => done);
            }
        }
    }
}
