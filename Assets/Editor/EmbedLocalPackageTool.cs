using UnityEditor;
using UnityEngine;
using System.IO;

public class EmbedLocalPackageTool
{
    [MenuItem("Tools/Embed Local Package")]
    public static void EmbedPackage()
    {
        string sourcePath = EditorUtility.OpenFolderPanel(
            "选择本地 Package 文件夹",
            "",
            ""
        );

        if (string.IsNullOrEmpty(sourcePath))
            return;

        // 校验 package.json
        string packageJsonPath = Path.Combine(sourcePath, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            EditorUtility.DisplayDialog("错误", "该目录不是有效的 Unity Package（缺少 package.json）", "OK");
            return;
        }

        // 读取 package name
        string json = File.ReadAllText(packageJsonPath);
        string packageName = ExtractPackageName(json);

        if (string.IsNullOrEmpty(packageName))
        {
            EditorUtility.DisplayDialog("错误", "无法解析 package.json 中的 name 字段", "OK");
            return;
        }

        string targetPath = Path.Combine("Packages", packageName);

        // 已存在处理
        if (Directory.Exists(targetPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "已存在",
                $"包 {packageName} 已存在，是否覆盖？",
                "覆盖",
                "取消"
            );

            if (!overwrite)
                return;

            Directory.Delete(targetPath, true);
        }

        // 拷贝目录
        CopyDirectory(sourcePath, targetPath);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("成功", $"已嵌入包：{packageName}", "OK");
    }

    static string ExtractPackageName(string json)
    {
        // 简单解析（避免引入JSON库）
        string key = "\"name\"";
        int index = json.IndexOf(key);
        if (index == -1) return null;

        int colon = json.IndexOf(":", index);
        int startQuote = json.IndexOf("\"", colon + 1);
        int endQuote = json.IndexOf("\"", startQuote + 1);

        if (startQuote == -1 || endQuote == -1) return null;

        return json.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, destDir);
        }
    }
}