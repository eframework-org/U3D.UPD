// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS

using NUnit.Framework;
using static EFramework.Update.XUpdate;

public class TestXUpdatePrefs
{
    [Test]
    public void Keys()
    {
        Assert.AreEqual(Prefs.Version, "Update/Version");
        Assert.AreEqual(Prefs.SkipCheck, "Update/SkipCheck");
        Assert.AreEqual(Prefs.SkipVersion, "Update/SkipVersion");
        Assert.AreEqual(Prefs.BinaryUrl, "Update/BinaryUrl");
        Assert.AreEqual(Prefs.BinarySize, "Update/BinarySize");
        Assert.AreEqual(Prefs.WhiteList, "Update/WhiteList");
        Assert.AreEqual(Prefs.PatchHost, "Update/PatchHost");
        Assert.AreEqual(Prefs.PatchUri, "Update/PatchUri");
        Assert.AreEqual(Prefs.PatchPublishHost, "Update/Patch/Publish/Host@Editor");
        Assert.AreEqual(Prefs.PatchPublishBucket, "Update/Patch/Publish/Bucket@Editor");
        Assert.AreEqual(Prefs.PatchPublishAccess, "Update/Patch/Publish/Access@Editor");
        Assert.AreEqual(Prefs.PatchPublishSecret, "Update/Patch/Publish/Secret@Editor");
    }

    [Test]
    public void Defaults()
    {
        Assert.AreEqual(Prefs.SkipCheckDefault, false);
        Assert.AreEqual(Prefs.SkipVersionDefault, false);
        Assert.AreEqual(Prefs.PatchHostDefault, "${Env.OssPublic}");
        Assert.AreEqual(Prefs.PatchUriDefault, "Builds/Patch/${Env.Solution}/${Env.Author}/${Env.Channel}/${Env.Platform}/${Env.Version}");
        Assert.AreEqual(Prefs.PatchPublishHostDefault, "${Env.OssHost}");
        Assert.AreEqual(Prefs.PatchPublishBucketDefault, "${Env.OssBucket}");
        Assert.AreEqual(Prefs.PatchPublishAccessDefault, "${Env.OssAccess}");
        Assert.AreEqual(Prefs.PatchPublishSecretDefault, "${Env.OssSecret}");
    }
}
#endif