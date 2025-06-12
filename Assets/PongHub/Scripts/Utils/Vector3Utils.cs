// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;

namespace PongHub.Utils
{
    /// <summary>
    /// Utility functions added to vector3 to easily change one of the components (x, y or z) of a vector3.
    /// </summary>
    public static class Vector3Utils
    {
        public static Vector3 SetX(this Vector3 vec, float value)
        {
            return new Vector3(value, vec.y, vec.z);
        }
        public static Vector3 SetY(this Vector3 vec, float value)
        {
            return new Vector3(vec.x, value, vec.z);
        }
        public static Vector3 SetZ(this Vector3 vec, float value)
        {
            return new Vector3(vec.x, vec.y, value);
        }
    }
}
