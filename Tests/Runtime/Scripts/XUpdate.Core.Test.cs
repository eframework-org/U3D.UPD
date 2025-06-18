// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS

using System;
using System.Collections;
using System.Collections.Generic;
using EFramework.Update;
using EFramework.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static EFramework.Update.XUpdate;

public class TestXUpdateCore
{
    /// <summary>
    /// 自定义处理器。
    /// </summary>
    public class MyHandler : IHandler
    {
        public MyHandler()
        {
            patches = new List<Patch>
            {
                new MyPatch(),
                new MyPatch(),
            };
        }

        public bool EndOnError = false;
        public int RetriedCount = 0;
        public bool CheckResult = true;
        public bool IsBinary = false;

        private Binary binary;
        Binary IHandler.Binary
        {
            get
            {
                if (binary != null)
                {
                    binary = new Binary();
                }
                return binary;
            }
        }

        private List<Patch> patches;
        List<Patch> IHandler.Patches
        {
            get
            {
                patches ??= new List<Patch> { new MyPatch(), new MyPatch(), };
                return patches;
            }
        }

        bool IHandler.OnCheck(out string version, out bool binary, out bool patch)
        {
            version = "1.1.0";
            binary = IsBinary;
            patch = !IsBinary;
            return CheckResult;
        }

        bool IHandler.OnRetry(Phase phase, Patch patcher, int count, out float wait)
        {
            wait = 0.1f;
            if (EndOnError) return false;
            if (count > 3)
            {
                patcher.Error = string.Empty;
                if (phase == Phase.Preprocess) (patcher as MyPatch).ErrorOnPreprocess = false;
                else if (phase == Phase.Process) (patcher as MyPatch).ErrorOnProcess = false;
                else if (phase == Phase.Postprocess) (patcher as MyPatch).ErrorOnPostprocess = false;
                return true;
            }
            RetriedCount++;
            return true;
        }

        public void SetPreprocessError(int index)
        {
            (patches[index] as MyPatch).ErrorOnPreprocess = true;
        }

        public void SetProcessError(int index)
        {
            (patches[index] as MyPatch).ErrorOnProcess = true;
        }

        public void SetPostprocessError(int index)
        {
            (patches[index] as MyPatch).ErrorOnPostprocess = true;
        }
    }

    /// <summary>
    /// 自定义补丁。
    /// </summary>
    public class MyPatch : Patch
    {
        public bool ErrorOnPreprocess;
        public bool ErrorOnProcess;
        public bool ErrorOnPostprocess;

        public MyPatch() : base("", "", "http://localhost:9000/default/")
        {
            RemoteMani = new XMani.Manifest();
            DiffInfo = new XMani.DiffInfo();
        }

        protected override IEnumerator Extract()
        {
            XUpdate.Event.Notify(XUpdate.EventType.OnPatchExtractStart);
            XUpdate.Event.Notify(XUpdate.EventType.OnPatchExtractUpdate);
            if (string.IsNullOrEmpty(Error)) XUpdate.Event.Notify(XUpdate.EventType.OnPatchExtractSucceed);
            else XUpdate.Event.Notify(XUpdate.EventType.OnPatchExtractFailed);
            yield return null;
        }

        protected override Func<bool> Validate()
        {
            XUpdate.Event.Notify(XUpdate.EventType.OnPatchValidateStart);
            XUpdate.Event.Notify(XUpdate.EventType.OnPatchValidateUpdate);
            if (string.IsNullOrEmpty(Error)) XUpdate.Event.Notify(XUpdate.EventType.OnPatchValidateSucceed);
            else XUpdate.Event.Notify(XUpdate.EventType.OnPatchValidateFailed);
            return () => true;
        }

        protected override IEnumerator Download()
        {
            XUpdate.Event.Notify(XUpdate.EventType.OnPatchDownloadStart);
            XUpdate.Event.Notify(XUpdate.EventType.OnPatchDownloadUpdate);
            if (string.IsNullOrEmpty(Error)) XUpdate.Event.Notify(XUpdate.EventType.OnPatchDownloadSucceed);
            else XUpdate.Event.Notify(XUpdate.EventType.OnPatchDownloadFailed);
            yield return null;
        }

        protected override Func<bool> Cleanup()
        {
            if (string.IsNullOrEmpty(Error)) Debug.Log("MyPatch.Cleanup Success");
            else
            {
                Error = string.Empty;
                Debug.LogError("MyPatch.Cleanup Failed");
            }
            return () => true;
        }

        public override IEnumerator Preprocess(bool remote)
        {
            if (ErrorOnPreprocess) Error = "Preprocess error";
            yield return Extract();
            yield return new WaitUntil(Validate());
        }

        public override IEnumerator Process()
        {
            if (ErrorOnProcess) Error = "Process error";
            yield return Download();
        }

        public override IEnumerator Postprocess()
        {
            if (ErrorOnPostprocess) Error = "Postprocess error";
            yield return Cleanup();
        }
    }

    [UnityTest]
    public IEnumerator Process()
    {
        bool[] isBinaries = new bool[] { true, false };
        foreach (var isBinary in isBinaries)
        {
            var isUpdateStart = false;
            var isPatchUpdateStart = false;
            var isPatchUpdateFinish = false;
            var isUpdateFinish = false;
            var isBinaryUpdateStart = false;
            var isBinaryUpdateFinish = false;
            var isPatchExtractStart = false;
            var isPatchExtractUpdate = false;
            var isPatchExtractSucceed = false;
            var isPatchExtractFailed = false;
            var isPatchValidateStart = false;
            var isPatchValidateUpdate = false;
            var isPatchValidateSucceed = false;
            var isPatchValidateFailed = false;
            var isPatchDownloadStart = false;
            var isPatchDownloadUpdate = false;
            var isPatchDownloadSucceed = false;
            var isPatchDownloadFailed = false;

            var handler = new MyHandler();
            handler.IsBinary = isBinary;
            XUpdate.Event.Reg(XUpdate.EventType.OnUpdateStart, () => isUpdateStart = true);
            XUpdate.Event.Reg(XUpdate.EventType.OnPatchUpdateStart, () => isPatchUpdateStart = true);
            XUpdate.Event.Reg(XUpdate.EventType.OnPatchUpdateFinish, () => isPatchUpdateFinish = true);
            XUpdate.Event.Reg(XUpdate.EventType.OnUpdateFinish, () => isUpdateFinish = true);
            XUpdate.Event.Reg(XUpdate.EventType.OnBinaryUpdateStart, () => isBinaryUpdateStart = true);
            XUpdate.Event.Reg(XUpdate.EventType.OnBinaryUpdateFinish, () => isBinaryUpdateFinish = true);
            if (!isBinary)
            {
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchExtractStart, () => isPatchExtractStart = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchExtractUpdate, () => isPatchExtractUpdate = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchExtractSucceed, () => isPatchExtractSucceed = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchExtractFailed, () => isPatchExtractFailed = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchValidateStart, () => isPatchValidateStart = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchValidateUpdate, () => isPatchValidateUpdate = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchValidateSucceed, () => isPatchValidateSucceed = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchValidateFailed, () => isPatchValidateFailed = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadStart, () => isPatchDownloadStart = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadUpdate, () => isPatchDownloadUpdate = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadSucceed, () => isPatchDownloadSucceed = true);
                XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadFailed, () => isPatchDownloadFailed = true);
                LogAssert.Expect(LogType.Log, "MyPatch.Cleanup Success");
            }

            yield return XUpdate.Process(handler);

            Assert.IsTrue(isUpdateStart, "更新开始事件应当被触发");
            Assert.IsTrue(isUpdateFinish, "更新完成事件应当被触发");
            Assert.AreNotEqual(isBinary, isPatchUpdateStart, "补丁更新开始事件是否触发应当与isBinary相反");
            Assert.AreNotEqual(isBinary, isPatchUpdateFinish, "补丁更新完成事件是否触发应当与isBinary相反");
            Assert.AreEqual(isBinary, isBinaryUpdateStart, "包更新开始是否触发应当与isBinary一致");
            Assert.AreEqual(isBinary, isBinaryUpdateFinish, "包更新完成是否触发应当与isBinary一致");
            if (!isBinary)
            {
                Assert.IsTrue(isPatchExtractStart, "补丁提取开始事件应当被触发");
                Assert.IsTrue(isPatchExtractUpdate, "补丁提取更新事件应当被触发");
                Assert.IsTrue(isPatchExtractSucceed, "补丁提取成功事件应当被触发");
                Assert.IsFalse(isPatchExtractFailed, "补丁提取失败事件应当不被触发");
                Assert.IsTrue(isPatchValidateStart, "补丁校验开始事件应当被触发");
                Assert.IsTrue(isPatchValidateUpdate, "补丁校验更新事件应当被触发");
                Assert.IsTrue(isPatchValidateSucceed, "补丁校验成功事件应当被触发");
                Assert.IsFalse(isPatchValidateFailed, "补丁校验失败事件应当不被触发");
                Assert.IsTrue(isPatchDownloadStart, "补丁下载开始事件应当被触发");
                Assert.IsTrue(isPatchDownloadUpdate, "补丁下载更新事件应当被触发");
                Assert.IsTrue(isPatchDownloadSucceed, "补丁下载成功事件应当被触发");
                Assert.IsFalse(isPatchDownloadFailed, "补丁下载失败事件应当不被触发");
            }
        }
    }

    [UnityTest]
    public IEnumerator OnCheck()
    {
        bool[] results = new bool[] { true, false };
        foreach (var result in results)
        {
            var isUpdateStart = false;
            var isUpdateFinish = false;
            XUpdate.Event.Reg(XUpdate.EventType.OnUpdateStart, () => isUpdateStart = true);
            XUpdate.Event.Reg(XUpdate.EventType.OnUpdateFinish, () => isUpdateFinish = true);
            var handler = new MyHandler();
            handler.CheckResult = result;
            yield return XUpdate.Process(handler);
            Assert.AreEqual(true, isUpdateStart, "更新开始事件应当始终被触发");
            Assert.AreEqual(result, isUpdateFinish, "更新完成事件是否触发应当与checkResult一致");
        }
    }

    [UnityTest]
    public IEnumerator OnRetry()
    {
        var handler = new MyHandler();

        handler.SetPreprocessError(0);
        yield return XUpdate.Process(handler);
        Assert.AreEqual(3, handler.RetriedCount, "重试次数应当为3");

        handler.RetriedCount = 0;
        handler.SetPreprocessError(0);
        handler.SetProcessError(0);
        yield return XUpdate.Process(handler);
        Assert.AreEqual(6, handler.RetriedCount, "重试次数应当为6");

        handler.RetriedCount = 0;
        handler.EndOnError = true;
        handler.SetPreprocessError(0);
        yield return XUpdate.Process(handler);
        Assert.AreEqual(0, handler.RetriedCount, "重试次数应当为0");
    }

    [UnityTest]
    public IEnumerator OnEvent()
    {
        var handler = new MyHandler();

        var isExtractFailed = false;
        var isValidateFailed = false;
        var isDownloadFailed = false;
        XUpdate.Event.Reg(XUpdate.EventType.OnPatchExtractFailed, () => isExtractFailed = true);
        XUpdate.Event.Reg(XUpdate.EventType.OnPatchValidateFailed, () => isValidateFailed = true);
        XUpdate.Event.Reg(XUpdate.EventType.OnPatchDownloadFailed, () => isDownloadFailed = true);
        handler.SetPreprocessError(0);
        handler.SetProcessError(0);
        LogAssert.Expect(LogType.Error, "MyPatch.Cleanup Failed");
        handler.SetPostprocessError(0);
        yield return XUpdate.Process(handler);
        Assert.IsTrue(isExtractFailed, "补丁提取失败事件应当被触发");
        Assert.IsTrue(isValidateFailed, "补丁校验失败事件应当被触发");
        Assert.IsTrue(isDownloadFailed, "补丁下载失败事件应当被触发");
    }
}
#endif
