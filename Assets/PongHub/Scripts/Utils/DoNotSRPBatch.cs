// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;

namespace PongHub.Utils
{
    /// <summary>
    /// Add this monobehaviour on a gameobject with a renderer to disable SRP Batching
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class DoNotSRPBatch : MonoBehaviour
    {
        private void Start()
        {
            var r = GetComponent<Renderer>();
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            r.SetPropertyBlock(mpb);
        }
    }
}