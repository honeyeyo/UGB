// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEditor;

namespace PongHub.Editor
{
    /// <summary>
    /// This class helps us track the usage of this showcase
    /// </summary>
    [InitializeOnLoad]
    public static class PongHubTelemetry
    {
        // This is the name of this showcase
        private const string PROJECT_NAME = "Unity-PongHub";

        private const string SESSION_KEY = "OculusTelemetry-module_loaded-" + PROJECT_NAME;
        static PongHubTelemetry() => Collect();

        private static void Collect(bool force = false)
        {
            if (SessionState.GetBool(SESSION_KEY, false) == false)
            {
                _ = OVRPlugin.SetDeveloperMode(OVRPlugin.Bool.True);
                _ = OVRPlugin.SendEvent("module_loaded", PROJECT_NAME, "integration");
                SessionState.SetBool(SESSION_KEY, true);
            }
        }
    }
}