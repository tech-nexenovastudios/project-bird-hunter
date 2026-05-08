#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

// Single owner of CODE_SIGN_ENTITLEMENTS for iOS builds.
//
// The Apple plugin (com.apple.unityplugin.core) and UGS Push Notifications both
// shipped post-processors that called PBXProject.AddBuildProperty for
// CODE_SIGN_ENTITLEMENTS — and AddBuildProperty appends rather than replaces.
// Result: the build setting ended up as "<a>.entitlements <b>.entitlements"
// and Xcode failed at code signing.
//
// The fix: AppleBuildProfile.AutomateEntitlements is set to 0, which short-circuits
// Apple.Core's entire entitlements pipeline (including AppleGameKitBuildStep that
// would add com.apple.developer.game-center). UGS then becomes the sole writer of
// the entitlements file and the only caller of AddBuildProperty.
//
// This post-processor (running after UGS at order 0) reopens UGS's entitlements
// file via ProjectCapabilityManager and adds Game Center — the one capability we
// lost by disabling AutomateEntitlements.
public static class GameCenterEntitlementsPostProcess
{
    [PostProcessBuild(50)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        if (!File.Exists(pbxPath))
            return;

        var pbx = new PBXProject();
        pbx.ReadFromFile(pbxPath);

        string mainTargetGuid = pbx.GetUnityMainTargetGuid();
        string entitlementsFile = pbx.GetBuildPropertyForAnyConfig(mainTargetGuid, "CODE_SIGN_ENTITLEMENTS");

        if (string.IsNullOrEmpty(entitlementsFile))
        {
            Debug.LogWarning("[GameCenterEntitlements] CODE_SIGN_ENTITLEMENTS not set — UGS Push post-processor likely didn't run. Skipping Game Center entitlement.");
            return;
        }

        var capManager = new ProjectCapabilityManager(pbxPath, entitlementsFile, "Unity-iPhone");
        capManager.AddGameCenter();
        capManager.WriteToFile();

        Debug.Log($"[GameCenterEntitlements] Added Game Center capability to {entitlementsFile}.");
    }
}
#endif
