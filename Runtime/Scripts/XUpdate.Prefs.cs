// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using UnityEngine;
using EFramework.Utility;

namespace EFramework.Update
{
    public partial class XUpdate
    {
        /// <summary>
        /// XUpdate.Prefs 管理更新系统的配置项，支持运行时读取和编辑器可视化配置。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持版本配置管理：提供版本号和更新检查等基础配置
        /// - 实现更新服务配置：支持补丁包和安装包访问服务器地址和路径设置
        /// - 提供发布选项管理：支持补丁包发布服务器地址和密钥配置
        /// - 实现可视化配置界面：在 Unity 编辑器中提供直观的设置面板
        /// 
        /// 使用手册
        /// 1. 版本控制
        /// - 版本号 (Update/Version)
        ///   本地版本号存储在 XPrefs.Local，远端版本号存储在 XPrefs.Remote
        ///   基于这两个版本号进行版本更新检查
        /// 
        /// - 跳过检查 (Update/SkipCheck)
        ///   控制是否跳过更新检查，用于开发环境调试
        /// 
        /// - 跳过更新 (Update/SkipVersion)
        ///   控制是否跳过版本更新，仅在开发环境下使用
        /// 
        /// - 更新白名单 (Update/WhiteList)
        ///   控制允许更新的版本范围，用于版本更新的权限控制
        /// 
        /// 2. 安装包配置
        /// - 安装包地址 (Update/BinaryUrl)
        ///   安装包文件的下载地址
        /// 
        /// - 安装包大小 (Update/BinarySize)
        ///   安装包文件的下载大小
        /// 
        /// 3. 补丁包配置
        /// - 补丁包地址 (Update/PatchHost)
        ///   补丁包访问服务器的地址，默认值：${Env.OssPublic}
        /// 
        /// - 补丁包路径 (Update/PatchUri)
        ///   补丁包访问服务器的路径，默认值：
        ///   Builds/Patch/${Env.Solution}/${Env.Author}/${Env.Channel}/${Env.Platform}/${Env.Version}
        /// 
        /// 4. 发布配置
        /// - 发布地址 (Update/Patch/Publish/Host@Editor)
        ///   补丁发布服务器的地址，默认值：${Env.OssHost}，仅在编辑器环境下可用
        /// 
        /// - 发布存储 (Update/Patch/Publish/Bucket@Editor)
        ///   补丁发布服务器的存储桶，默认值：${Env.OssBucket}，仅在编辑器环境下可用
        /// 
        /// - 访问密钥 (Update/Patch/Publish/Access@Editor)
        ///   补丁发布服务器的访问密钥，默认值：${Env.OssAccess}，仅在编辑器环境下可用
        /// 
        /// - 秘密密钥 (Update/Patch/Publish/Secret@Editor)
        ///   补丁发布服务器的秘密密钥，默认值：${Env.OssSecret}，仅在编辑器环境下可用
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Prefs : XPrefs.Panel
        {
            /// <summary>
            /// 当前版本号的配置键。
            /// </summary>
            public const string Version = "Update/Version";

            /// <summary>
            /// 是否跳过更新检查的配置键。
            /// </summary>
            public const string SkipCheck = "Update/SkipCheck";

            /// <summary>
            /// 跳过更新检查的默认值。
            /// </summary>
            public const bool SkipCheckDefault = false;

            /// <summary>
            /// 是否跳过版本更新的配置键。
            /// </summary>
            public const string SkipVersion = "Update/SkipVersion";

            /// <summary>
            /// 跳过版本更新的默认值。
            /// </summary>
            public const bool SkipVersionDefault = false;

            /// <summary>
            /// 安装包文件下载地址的配置键。
            /// </summary>
            public const string BinaryUrl = "Update/BinaryUrl";

            /// <summary>
            /// 安装包文件大小的配置键。
            /// </summary>
            public const string BinarySize = "Update/BinarySize";

            /// <summary>
            /// 更新白名单的配置键。
            /// </summary>
            public const string WhiteList = "Update/WhiteList";

            /// <summary>
            /// 补丁包访问服务器主机地址的配置键。
            /// </summary>
            public const string PatchHost = "Update/PatchHost";

            /// <summary>
            /// 补丁包访问服务器主机地址的默认值。
            /// 使用环境变量 ${Env.OssPublic} 作为默认值。
            /// </summary>
            public const string PatchHostDefault = "${Env.OssPublic}";

            /// <summary>
            /// 补丁包文件 URI 的配置键。
            /// </summary>
            public const string PatchUri = "Update/PatchUri";

            /// <summary>
            /// 补丁包文件 URI 的默认值。
            /// 使用环境变量组合构建补丁包路径。
            /// </summary>
            public const string PatchUriDefault = "Builds/Patch/${Env.Solution}/${Env.Author}/${Env.Channel}/${Env.Platform}/${Env.Version}";

            /// <summary>
            /// 补丁包发布服务器主机地址的配置键（仅编辑器环境）。
            /// </summary>
            public const string PatchPublishHost = "Update/Patch/Publish/Host@Editor";

            /// <summary>
            /// 补丁包发布服务器主机地址的默认值（仅编辑器环境）。
            /// 使用环境变量 ${Env.OssHost} 作为默认值。
            /// </summary>
            public const string PatchPublishHostDefault = "${Env.OssHost}";

            /// <summary>
            /// 补丁包发布服务器存储桶的配置键（仅编辑器环境）。
            /// </summary>
            public const string PatchPublishBucket = "Update/Patch/Publish/Bucket@Editor";

            /// <summary>
            /// 补丁包发布服务器存储桶的默认值（仅编辑器环境）。
            /// 使用环境变量 ${Env.OssBucket} 作为默认值。
            /// </summary>
            public const string PatchPublishBucketDefault = "${Env.OssBucket}";

            /// <summary>
            /// 补丁包发布服务器访问密钥的配置键（仅编辑器环境）。
            /// </summary>
            public const string PatchPublishAccess = "Update/Patch/Publish/Access@Editor";

            /// <summary>
            /// 补丁包发布服务器访问密钥的默认值（仅编辑器环境）。
            /// 使用环境变量 ${Env.OssAccess} 作为默认值。
            /// </summary>
            public const string PatchPublishAccessDefault = "${Env.OssAccess}";

            /// <summary>
            /// 补丁包发布服务器秘密密钥的配置键（仅编辑器环境）。
            /// </summary>
            public const string PatchPublishSecret = "Update/Patch/Publish/Secret@Editor";

            /// <summary>
            /// 补丁包发布服务器秘密密钥的默认值（仅编辑器环境）。
            /// 使用环境变量 ${Env.OssSecret} 作为默认值。
            /// </summary>
            public const string PatchPublishSecretDefault = "${Env.OssSecret}";

#if UNITY_EDITOR
            /// <summary>
            /// 获取设置面板的节名称。
            /// </summary>
            public override string Section => "Update";

            /// <summary>
            /// 获取设置面板的显示优先级。
            /// </summary>
            public override int Priority => 10;

            /// <summary>
            /// 获取设置面板的工具提示。
            /// </summary>
            public override string Tooltip => "Preferences of Update.";

            /// <summary>
            /// 发布选项的折叠状态。
            /// </summary>
            [SerializeField] protected bool foldout;

            /// <summary>
            /// 绘制设置面板的可视化界面。
            /// </summary>
            /// <param name="searchContext">搜索上下文字符串</param>
            public override void OnVisualize(string searchContext)
            {
                UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Skip");
                Title("Check", "Check/Skip Update.");
                Target.Set(SkipCheck, UnityEditor.EditorGUILayout.Toggle(Target.GetBool(SkipCheck, SkipCheckDefault)));

                Title("Version", "Check/Skip Version.");
                Target.Set(SkipVersion, UnityEditor.EditorGUILayout.Toggle(Target.GetBool(SkipVersion, SkipVersionDefault)));
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Patch", "Patch Host and Uri.");
                Target.Set(PatchHost, UnityEditor.EditorGUILayout.TextField(Target.GetString(PatchHost, PatchHostDefault)));
                Target.Set(PatchUri, UnityEditor.EditorGUILayout.TextField(Target.GetString(PatchUri, PatchUriDefault)));
                UnityEditor.EditorGUILayout.EndHorizontal();
                UnityEditor.EditorGUILayout.EndVertical();

                UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                foldout = UnityEditor.EditorGUILayout.Foldout(foldout, new GUIContent("Publish", "Patch Publish Options."));
                if (foldout)
                {
                    UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                    UnityEditor.EditorGUILayout.BeginHorizontal();
                    Title("Host", "Oss Host Name");
                    Target.Set(PatchPublishHost, UnityEditor.EditorGUILayout.TextField("", Target.GetString(PatchPublishHost, PatchPublishHostDefault)));
                    UnityEditor.EditorGUILayout.EndHorizontal();

                    UnityEditor.EditorGUILayout.BeginHorizontal();
                    Title("Bucket", "Oss Bucket Name");
                    Target.Set(PatchPublishBucket, UnityEditor.EditorGUILayout.TextField("", Target.GetString(PatchPublishBucket, PatchPublishBucketDefault)));
                    UnityEditor.EditorGUILayout.EndHorizontal();

                    UnityEditor.EditorGUILayout.BeginHorizontal();
                    Title("Access", "Oss Access Key");
                    Target.Set(PatchPublishAccess, UnityEditor.EditorGUILayout.TextField("", Target.GetString(PatchPublishAccess, PatchPublishAccessDefault)));
                    UnityEditor.EditorGUILayout.EndHorizontal();

                    UnityEditor.EditorGUILayout.BeginHorizontal();
                    Title("Secret", "Oss Secret Key");
                    Target.Set(PatchPublishSecret, UnityEditor.EditorGUILayout.TextField("", Target.GetString(PatchPublishSecret, PatchPublishSecretDefault)));
                    UnityEditor.EditorGUILayout.EndHorizontal();
                    UnityEditor.EditorGUILayout.EndVertical();
                }
                UnityEditor.EditorGUILayout.EndVertical();
            }
#endif
        }
    }
}
