using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildScript
{
    public static void BuildiOS()
    {
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
        options.locationPathName = "build/iOS";
        options.target = BuildTarget.iOS;
        options.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception("Build failed");
        }
    }
}
