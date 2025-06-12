// Copyright (c) MagnusLab Inc. and affiliates.

namespace PongHub.Arena.Services
{
    /// <summary>
    /// Enum of the different team colors we use. Used by the TeamColorProfiles singleton.
    /// </summary>
    public enum TeamColor
    {
        // Each profile key should be in order A-B and it should be an even number
        Profile1TeamA,
        Profile1TeamB,
        Profile2TeamA,
        Profile2TeamB,
        Profile3TeamA,
        Profile3TeamB,
        Profile4TeamA,
        Profile4TeamB,

        Count,
    }
}