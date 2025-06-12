// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Utilities;
using UnityEngine;

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// Keeps track of specific game state.
    /// </summary>
    public class GameState : Singleton<GameState>
    {
        [SerializeField, AutoSet] public NetworkedScore Score;
    }
}