// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using UnityEngine;
using PongHub.Arena.Gameplay;
using System.Collections.Generic;
using System.Linq;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 乒乓球生成点配置
    /// 管理球桌周围的玩家和观众生成点
    /// </summary>
    [System.Serializable]
    public class PongSpawnConfiguration : MonoBehaviour
    {
        [Header("A边生成点 (蓝队 - 负Z方向)")]
        public Transform teamA_Position1;
        public Transform teamA_Position2;

        [Header("B边生成点 (红队 - 正Z方向)")]
        public Transform teamB_Position1;
        public Transform teamB_Position2;

        [Header("观众区域")]
        public Transform[] spectatorA_Positions = Array.Empty<Transform>();
        public Transform[] spectatorB_Positions = Array.Empty<Transform>();

        [Header("安全区域配置")]
        public float playerSafeRadius = 1.5f;
        public float tableSafeDistance = 1.5f;

        [Header("球桌引用")]
        public Transform pongTable;

        private Dictionary<Transform, bool> occupiedSpawnPoints = new();

        private void Awake()
        {
            InitializeSpawnPoints();
        }

        private void InitializeSpawnPoints()
        {
            occupiedSpawnPoints.Clear();

            RegisterSpawnPoint(teamA_Position1);
            RegisterSpawnPoint(teamA_Position2);
            RegisterSpawnPoint(teamB_Position1);
            RegisterSpawnPoint(teamB_Position2);

            foreach (var seat in spectatorA_Positions)
                RegisterSpawnPoint(seat);
            foreach (var seat in spectatorB_Positions)
                RegisterSpawnPoint(seat);
        }

        private void RegisterSpawnPoint(Transform spawnPoint)
        {
            if (spawnPoint != null)
            {
                occupiedSpawnPoints[spawnPoint] = false;
            }
        }

        public Transform GetPlayerSpawnPoint(PongGameMode mode, NetworkedTeam.Team team, int teamPosition = 0)
        {
            return mode switch
            {
                PongGameMode.Singles => team == NetworkedTeam.Team.TeamA ? teamA_Position1 : teamB_Position1,
                PongGameMode.Doubles => team == NetworkedTeam.Team.TeamA
                    ? (teamPosition == 0 ? teamA_Position1 : teamA_Position2)
                    : (teamPosition == 0 ? teamB_Position1 : teamB_Position2),
                _ => null
            };
        }

        public Transform GetSpectatorSpawnPoint(NetworkedTeam.Team preferredSide = NetworkedTeam.Team.NoTeam)
        {
            var candidateSeats = preferredSide switch
            {
                NetworkedTeam.Team.TeamA => spectatorA_Positions.ToList(),
                NetworkedTeam.Team.TeamB => spectatorB_Positions.ToList(),
                _ => spectatorA_Positions.Concat(spectatorB_Positions).ToList()
            };

            candidateSeats.RemoveAll(seat => IsSpawnPointOccupied(seat));

            return candidateSeats.Count > 0 ? candidateSeats[0] : null;
        }

        public bool OccupySpawnPoint(Transform spawnPoint)
        {
            if (spawnPoint != null && occupiedSpawnPoints.ContainsKey(spawnPoint))
            {
                if (!occupiedSpawnPoints[spawnPoint])
                {
                    occupiedSpawnPoints[spawnPoint] = true;
                    return true;
                }
            }
            return false;
        }

        public void ReleaseSpawnPoint(Transform spawnPoint)
        {
            if (spawnPoint != null && occupiedSpawnPoints.ContainsKey(spawnPoint))
            {
                occupiedSpawnPoints[spawnPoint] = false;
            }
        }

        public bool IsSpawnPointOccupied(Transform spawnPoint)
        {
            return spawnPoint != null &&
                   occupiedSpawnPoints.TryGetValue(spawnPoint, out bool occupied) &&
                   occupied;
        }

        public void ResetAllSpawnPoints()
        {
            var keys = new List<Transform>(occupiedSpawnPoints.Keys);
            foreach (var key in keys)
            {
                occupiedSpawnPoints[key] = false;
            }
        }

        public bool IsPositionSafe(Vector3 position, float radius = 0f)
        {
            radius = radius > 0 ? radius : playerSafeRadius;

            // 检查与球桌的距离
            if (pongTable != null)
            {
                float distanceToTable = Vector3.Distance(position, pongTable.position);
                if (distanceToTable < tableSafeDistance + radius)
                {
                    return false;
                }
            }

            // 检查与其他玩家的距离
            foreach (var kvp in occupiedSpawnPoints)
            {
                if (kvp.Value) // 如果生成点被占用
                {
                    float distance = Vector3.Distance(position, kvp.Key.position);
                    if (distance < playerSafeRadius + radius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetAvailablePlayerSlots(PongGameMode mode)
        {
            return mode switch
            {
                PongGameMode.Singles => 2,  // A1, B1
                PongGameMode.Doubles => 4,  // A1, A2, B1, B2
                _ => 0
            };
        }

        public int GetAvailableSpectatorSlots()
        {
            var allSpectatorSeats = spectatorA_Positions.Concat(spectatorB_Positions).ToList();
            return allSpectatorSeats.Count - allSpectatorSeats.Count(seat => IsSpawnPointOccupied(seat));
        }

        public Vector3 GetTeamCenter(NetworkedTeam.Team team)
        {
            return team switch
            {
                NetworkedTeam.Team.TeamA => (teamA_Position1.position +
                    (teamA_Position2 != null ? teamA_Position2.position : teamA_Position1.position)) / 2f,
                NetworkedTeam.Team.TeamB => (teamB_Position1.position +
                    (teamB_Position2 != null ? teamB_Position2.position : teamB_Position1.position)) / 2f,
                _ => pongTable != null ? pongTable.position : Vector3.zero
            };
        }
    }
}