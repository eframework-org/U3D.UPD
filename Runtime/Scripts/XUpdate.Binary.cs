// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Collections;

namespace EFramework.Update
{
    public partial class XUpdate
    {
        /// <summary>
        /// XUpdate.Binary 提供了安装包的更新功能，支持自动下载并解压安装。
        /// </summary>
        /// <remarks>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public class Binary : IWorker
        {
            /// <summary>
            /// Error 表示错误的信息。
            /// </summary>
            public virtual string Error { get; set; }

            /// <summary>
            /// Preprocess 是流程的预处理函数。
            /// </summary>
            /// <returns></returns>
            public virtual IEnumerator Preprocess() { yield return null; }

            /// <summary>
            /// Process 是流程的处理函数。
            /// </summary>
            /// <returns></returns>
            public virtual IEnumerator Process() { yield return null; }

            /// <summary>
            /// Postprocess 是流程的后处理函数。
            /// </summary>
            /// <returns></returns>
            public virtual IEnumerator Postprocess() { yield return null; }
        }
    }
}
