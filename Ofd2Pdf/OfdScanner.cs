using System;
using System.Collections.Generic;
using System.IO;

namespace Ofd2Pdf
{
    /// <summary>
    /// 将用户输入的文件 / 文件夹路径展开为 OFD 文件列表。
    /// 文件夹会被递归扫描，无权限访问的子目录会被跳过。
    /// </summary>
    public static class OfdScanner
    {
        public const string OfdExtension = ".ofd";

        public static bool IsOfdFile(string path)
        {
            return string.Equals(Path.GetExtension(path), OfdExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// PDF 输出路径：与 OFD 同目录、同名，仅扩展名改为 .pdf。
        /// </summary>
        public static string GetPdfPath(string ofdPath)
        {
            return Path.ChangeExtension(ofdPath, ".pdf");
        }

        /// <summary>
        /// 展开路径：文件直接保留（仅 .ofd），文件夹递归扫描其中所有 .ofd 文件。
        /// 结果已去重（不区分大小写），保持输入顺序，文件夹内按路径排序。
        /// </summary>
        public static List<string> Expand(IEnumerable<string> paths)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in paths)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                string path;
                try
                {
                    path = Path.GetFullPath(raw.Trim().Trim('"'));
                }
                catch (Exception)
                {
                    continue;
                }

                if (Directory.Exists(path))
                {
                    var found = new List<string>();
                    ScanDirectory(path, found);
                    found.Sort(StringComparer.OrdinalIgnoreCase);
                    foreach (var file in found)
                    {
                        if (seen.Add(file))
                        {
                            result.Add(file);
                        }
                    }
                }
                else if (File.Exists(path) && IsOfdFile(path))
                {
                    if (seen.Add(path))
                    {
                        result.Add(path);
                    }
                }
            }

            return result;
        }

        private static void ScanDirectory(string root, List<string> found)
        {
            // 手动迭代而非 SearchOption.AllDirectories：后者遇到一个无权限目录就会整体抛异常
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var dir = pending.Pop();

                try
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*" + OfdExtension))
                    {
                        // "*.ofd" 在 Windows 上也可能匹配 .ofdx 之类的扩展名，再精确判断一次
                        if (IsOfdFile(file))
                        {
                            found.Add(file);
                        }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }

                try
                {
                    foreach (var sub in Directory.EnumerateDirectories(dir))
                    {
                        // 跳过符号链接 / 目录联接，避免循环
                        if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) == 0)
                        {
                            pending.Push(sub);
                        }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
    }
}
