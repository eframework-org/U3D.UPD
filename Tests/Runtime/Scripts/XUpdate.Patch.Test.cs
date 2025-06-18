// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System.IO;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using EFramework.Utility;
using static EFramework.Update.XUpdate;
using UnityEngine;
using System.Text.RegularExpressions;
using EFramework.Editor;

public class TestXUpdatePatch
{
    private class MyPatch : Patch
    {
        public MyPatch() : base("", "", "http://localhost:9000/default") { }

        public float CurrentTime { get; set; } = 0f;

        public void SetSize(Phase phase, long size) { sizes[phase] = size; }

        public void SetProgress(Phase phase, float progress) { progresses[phase] = progress; }

        // 直接访问内部字典，用于测试设置
        public void SetSpeedTime(Phase phase, float time) { speedTimes[phase] = time; }

        public void SetLastSize(Phase phase, long size) { lastSizes[phase] = size; }

        public void SetSpeed(Phase phase, long speed) { speeds[phase] = speed; }

        // 重写Speed方法以模拟Time.realtimeSinceStartup
        public override long Speed(Phase phase)
        {
            speeds.TryGetValue(phase, out var speed);
            speedTimes.TryGetValue(phase, out var time);
            var deltaTime = CurrentTime - time;
            if (deltaTime > SpeedPeriod)
            {
                speedTimes[phase] = CurrentTime;
                lastSizes.TryGetValue(phase, out var last);
                var current = (long)(Size(phase) * Progress(phase));
                if (last > 0 && current > last) speed = (long)((current - last) / deltaTime);
                lastSizes[phase] = current;
                speeds[phase] = speed;
            }
            return speed;
        }
    }

    [UnityTest]
    public IEnumerator Process()
    {
        // 准备测试目录
        var testUri = $"TestXUpdatePatch/Patch-{XTime.GetMillisecond()}/Scripts";
        var localDir = XFile.PathJoin(XEnv.LocalPath, testUri);
        var remoteDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", testUri);

        // 准备本地文件
        {
            if (XFile.HasDirectory(localDir)) XFile.DeleteDirectory(localDir);
            XFile.CreateDirectory(localDir);

            var localManifest = new XMani.Manifest(XFile.PathJoin(localDir, XMani.Default));
            for (int i = 0; i < 100; i++)
            {
                var file = XFile.PathJoin(localDir, $"file{i}.txt");
                XFile.SaveText(file, $"test-local-{i}");
                localManifest.Files.Add(new XMani.FileInfo { Name = Path.GetFileName(file), MD5 = XFile.FileMD5(file), Size = XFile.FileSize(file) });
            }
            XFile.SaveText(localManifest.Uri, localManifest.ToString());

            XFile.Zip(localDir, XFile.PathJoin(XEnv.AssetPath, "Patch@Scripts.zip"));
            XFile.DeleteDirectory(localDir);
        }

        // 准备远端文件
        {
            if (XFile.HasDirectory(remoteDir)) XFile.DeleteDirectory(remoteDir);
            XFile.CreateDirectory(remoteDir);

            var remoteManifest = new XMani.Manifest(XFile.PathJoin(remoteDir, XMani.Default));
            for (int i = 50; i < 150; i++) // 索引从50-150开始，确保有50个删除，50个添加，50个修改文件
            {
                var content = $"test-remote-{i}";
                var file = XFile.PathJoin(remoteDir, $"file{i}.txt@{content.MD5()}");
                XFile.SaveText(file, content);
                remoteManifest.Files.Add(new XMani.FileInfo { Name = $"file{i}.txt", MD5 = content.MD5(), Size = content.Length });
            }
            XFile.SaveText(remoteManifest.Uri, remoteManifest.ToString());

            var oss = new XEditor.Oss
            {
                ID = "TestXUpdatePatch@my-upload",
                Host = "http://localhost:9000",
                Bucket = "default",
                Access = "admin",
                Secret = "adminadmin",
                Remote = testUri,
                Local = remoteDir
            };

            var report = XEditor.Tasks.Execute(oss);
            Assert.AreEqual(report.Result, XEditor.Tasks.Result.Succeeded, "远端文件上传失败");
        }

        // 执行更新流程
        try
        {
            var patchHost = "http://localhost:9000/default";
            var patch = new Patch(
                            XFile.PathJoin(XEnv.AssetPath, "Patch@Scripts.zip"),
                            XFile.PathJoin(localDir, XMani.Default),
                            $"{patchHost}/{testUri}/{XMani.Default}");

            // 测试 Preprocess 方法
            LogAssert.Expect(LogType.Error, new Regex(@"XMani\.Manifest\.Read: load and parse failed with error: Non exist file .* for reading mainfest\."));
            yield return patch.Preprocess();

            // 验证清单文件是否被正确处理
            Assert.IsTrue(string.IsNullOrEmpty(patch.Error), "预处理不应该有错误");
            Assert.IsNotNull(patch.LocalMani, "本地清单应该被初始化");
            Assert.IsNotNull(patch.RemoteMani, "远程清单应该被初始化");
            Assert.IsTrue(patch.DiffInfo.Added.Count == 50, "差异信息应该包含50个已添加的文件");
            Assert.IsTrue(patch.DiffInfo.Deleted.Count == 50, "差异信息应该包含50个已删除的文件");
            Assert.IsTrue(patch.DiffInfo.Modified.Count == 50, "差异信息应该包含50个已修改的文件");

            // 测试 Process 方法
            yield return patch.Process();
            yield return patch.Postprocess();

            foreach (var file in patch.RemoteMani.Files)
            {
                var path = XFile.PathJoin(localDir, file.Name);
                Assert.IsTrue(XFile.HasFile(path), "文件应当存在于本地：" + file.Name);
                Assert.IsTrue(XFile.FileMD5(path) == file.MD5, "文件应当与远程文件一致：" + file.Name);
            }
            Assert.IsTrue(patch.RemoteMani.Files.Count + 1 == Directory.GetFiles(localDir).Length, "本地文件数量应当等于远程文件数量加1");
        }
        finally
        {
            var remoteRootDir = XFile.PathJoin(XEnv.ProjectPath, "Temp/TestXUpdatePatch");
            if (XFile.HasDirectory(remoteRootDir)) XFile.DeleteDirectory(remoteRootDir);

            var localRootDir = XFile.PathJoin(XEnv.LocalPath, "TestXUpdatePatch");
            if (XFile.HasDirectory(localRootDir)) XFile.DeleteDirectory(localRootDir);

            var assetFile = XFile.PathJoin(XEnv.AssetPath, "Patch@Scripts.zip");
            if (XFile.HasFile(assetFile)) XFile.DeleteFile(assetFile);
        }
    }

    [TestCase(1.0f, 0.8f, 1000, 1024, 1024, "当时间间隔小于SpeedPeriod时应返回当前记录的速度值")]
    [TestCase(2.0f, 1.0f, 1000, 1024, 4000, "当时间间隔大于SpeedPeriod时应计算新速度")]
    [TestCase(3.0f, 2.0f, 0, 0, 0, "当上次大小为0时应返回0")]
    [TestCase(4.0f, 6000, 10000, 1024, 1024, "当前大小小于上次大小时应保持当前速度不变")]
    public void Speed(float currentTime, float speedTime, long lastSize, long speed, float expected, string _)
    {
        // 创建测试实例
        var patch = new MyPatch();
        var phase = Patch.Phase.Download;

        patch.CurrentTime = currentTime;
        patch.SetSpeedTime(phase, speedTime);
        patch.SetLastSize(phase, lastSize);
        patch.SetSpeed(phase, speed);
        patch.SetSize(phase, 10000);    // 设置总大小10000字节
        patch.SetProgress(phase, 0.5f); // 当前进度50%，处理了5000字节
        var result = patch.Speed(phase);
        Assert.AreEqual(expected, result);
    }

    [TestCase(Patch.Phase.Download, 0.5f, "下载阶段")]
    [TestCase(Patch.Phase.Extract, 0.6f, "解压阶段")]
    [TestCase(Patch.Phase.Validate, 0.7f, "验证阶段")]
    public void Progress(Patch.Phase phase, float expected, string _)
    {
        var patch = new MyPatch();

        patch.SetProgress(Patch.Phase.Download, 0.5f);
        patch.SetProgress(Patch.Phase.Extract, 0.6f);
        patch.SetProgress(Patch.Phase.Validate, 0.7f);
        Assert.AreEqual(expected, patch.Progress(phase));
    }

    [TestCase(Patch.Phase.Download, 1024, "下载阶段")]
    [TestCase(Patch.Phase.Extract, 512, "解压阶段")]
    [TestCase(Patch.Phase.Validate, 256, "验证阶段")]
    public void Size(Patch.Phase phase, long expected, string _)
    {
        var patch = new MyPatch();

        patch.SetSize(Patch.Phase.Download, 1024);
        patch.SetSize(Patch.Phase.Extract, 512);
        patch.SetSize(Patch.Phase.Validate, 256);
        Assert.AreEqual(expected, patch.Size(phase));
    }
}
#endif
