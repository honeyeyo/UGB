// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// Setup of the different colors for all the profiles. Each profiled is paired for team A and B.
    /// This helps simplify getting a random profile for the teams color and synchronize the colors for all clients.
    /// </summary>
    public class TeamColorProfiles : Singleton<TeamColorProfiles>
    {
        [Serializable]
        private struct ColorProfile
        {
            public TeamColor ColorKey;
            public Color Color;
        }

        [SerializeField]
        [Tooltip("Color Profiles / 颜色配置 - List of color profiles for team A and B color pairing")]
        private List<ColorProfile> m_colorProfiles;

        private readonly Dictionary<TeamColor, Color> m_colors = new();
        protected override void InternalAwake()
        {
            foreach (var colorProfile in m_colorProfiles)
            {
                m_colors[colorProfile.ColorKey] = colorProfile.Color;
            }
        }

        public Color GetColorForKey(TeamColor teamColor)
        {
            return m_colors[teamColor];
        }

        public void GetRandomProfile(out TeamColor teamColorA, out TeamColor teamColorB)
        {
            var profileCount = (int)TeamColor.Count / 2;
            var selectedProfile = Random.Range(0, profileCount);
            teamColorA = (TeamColor)(selectedProfile * 2);
            teamColorB = teamColorA + 1;
        }
    }
}