// Copyright (c) MagnusLab Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine.XR;

namespace PongHub.Input
{
    /// <summary>
    /// Override of the PointableCanvasModule so that we can use the mouse pointer in editor when not headset is
    /// destected instead of using the pointable canvas module.
    /// </summary>
    public class CustomPointableCanvasModule : PointableCanvasModule
    {
        public override bool IsModuleSupported()
        {
            return XRSettings.isDeviceActive && base.IsModuleSupported();
        }
    }
}