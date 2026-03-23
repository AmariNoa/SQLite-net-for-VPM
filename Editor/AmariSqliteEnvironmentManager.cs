using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace com.amari_noa.sqlite_net_vpm
{
    [InitializeOnLoad]
    internal static class AmariSqliteEnvironmentManager
    {
        private const string SqliteNativeLibraryFileName = "sqlite3.dll";
        private const string WarningStateSessionKey = "com.amari-noa.sqlite-net-vpm.duplicate-sqlite3-warning.signature";
        private const string DialogTitle = "SQLite-net VPM - Duplicate SQLite Libraries";

        static AmariSqliteEnvironmentManager()
        {
            EditorApplication.delayCall += WarnIfDuplicateSqliteLibrariesExist;
        }

        private static void WarnIfDuplicateSqliteLibrariesExist()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            var sqlitePaths = FindSqliteLibraryPaths();
            if (sqlitePaths.Count <= 1)
            {
                SessionState.EraseString(WarningStateSessionKey);
                return;
            }

            var signature = string.Join("|", sqlitePaths);
            var previousSignature = SessionState.GetString(WarningStateSessionKey, string.Empty);
            if (string.Equals(previousSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            SessionState.SetString(WarningStateSessionKey, signature);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Duplicate sqlite3.dll libraries were detected in this project.");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("This package only warns and does not delete files automatically.");
            messageBuilder.AppendLine("To avoid load-order issues, keep one active sqlite3.dll for Editor use.");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("Detected paths:");
            foreach (var path in sqlitePaths)
            {
                messageBuilder.Append("- ").AppendLine(path);
            }

            var message = messageBuilder.ToString();
            EditorUtility.DisplayDialog(DialogTitle, message, "OK");
            Debug.LogWarning($"[{AmariSqliteNetVpmInfo.PackageName}] {message}");
        }

        private static List<string> FindSqliteLibraryPaths()
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(SqliteNativeLibraryFileName);
            var guids = AssetDatabase.FindAssets(nameWithoutExtension);
            var paths = new List<string>(guids.Length);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(SqliteNativeLibraryFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                paths.Add(path);
            }

            return paths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
