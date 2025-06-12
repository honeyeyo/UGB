// Copyright (c) MagnusLab Inc. and affiliates.

namespace PongHub.Arena.Gameplay
{
    /// <summary>
    /// Constants of the different Unity Layers used in the game. Centralizes the values and keeps it easy to read.
    /// It also setup some masks used for different physics checks.
    /// </summary>
    public static class ObjectLayers
    {
        public const int DEFAULT = 0;
        public const int PLAYER = 3;
        public const int BALL = 8;
        public const int SPAWN_BALL = 9;
        public const int FIRE_BALL = 10;
        public const int FIRE_TRIGGERS = 11;
        public const int HITABLE = 12;

        public const int DEFAULT_MASK = 1 << DEFAULT;
        public const int DEFAULT_AND_BALL_SPAWN_MASK = (1 << DEFAULT) | (1 << SPAWN_BALL);
    }
}