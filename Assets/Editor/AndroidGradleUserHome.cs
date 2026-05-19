using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class AndroidGradleUserHome : IPreprocessBuildWithReport
{
    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }

        var gradleUserHome = Path.GetFullPath(Path.Combine("Library", "GradleUserHome"));
        Directory.CreateDirectory(gradleUserHome);
        Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleUserHome, EnvironmentVariableTarget.Process);
        Debug.Log($"Android Gradle user home: {gradleUserHome}");
    }
}
