// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections.Generic;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Player;
using PongHub.Arena.Spectator;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 竞技场玩家生成管理器
    /// 负责处理玩家连接到竞技场的生成逻辑。管理玩家在正确的队伍和位置生成。
    /// 当玩家请求在游戏中生成时,该控制器将评估要生成的玩家或观众预制体、
    /// 他们的位置、队伍和队伍颜色。
    /// </summary>
    public class ArenaPlayerSpawningManager : SpawningManagerBase
    {
        /// <summary>
        /// 玩家预制体
        /// </summary>
        [SerializeField] private NetworkObject m_playerPrefab;

        /// <summary>
        /// 手套骨骼预制体
        /// </summary>
        [SerializeField] private NetworkObject m_gloveArmaturePrefab;

        /// <summary>
        /// 手套手部预制体
        /// </summary>
        [SerializeField] private NetworkObject m_gloveHandPrefab;

        /// <summary>
        /// 观众预制体
        /// </summary>
        [SerializeField] private NetworkObject m_spectatorPrefab;

        /// <summary>
        /// 游戏管理器引用
        /// </summary>
        [SerializeField] private GameManager m_gameManager;

        /// <summary>
        /// A队出生点数组
        /// </summary>
        [SerializeField] private Transform[] m_teamASpawnPoints = Array.Empty<Transform>();

        /// <summary>
        /// B队出生点数组
        /// </summary>
        [SerializeField] private Transform[] m_teamBSpawnPoints = Array.Empty<Transform>();

        /// <summary>
        /// A队观众出生点服务
        /// </summary>
        [SerializeField] private SpawnPointReservingService m_spectatorASpawnPoints;

        /// <summary>
        /// B队观众出生点服务
        /// </summary>
        [SerializeField] private SpawnPointReservingService m_spectatorBSpawnPoints;

        /// <summary>
        /// 胜利方出生点服务
        /// </summary>
        [SerializeField] private SpawnPointReservingService m_winnerSpawnPoints;

        /// <summary>
        /// 失败方出生点服务
        /// </summary>
        [SerializeField] private SpawnPointReservingService m_loserSpawnPoints;

        /// <summary>
        /// 平局时是否轮流分配到胜利方出生点
        /// </summary>
        private bool m_tieAlternateToWin = true;

        // 出生点随机化队列
        /// <summary>
        /// A队随机出生点顺序队列
        /// </summary>
        private Queue<int> m_teamARandomSpawnOrder = new();

        /// <summary>
        /// B队随机出生点顺序队列
        /// </summary>
        private Queue<int> m_teamBRandomSpawnOrder = new();

        /// <summary>
        /// 临时存储出生点索引的列表
        /// </summary>
        private readonly List<int> m_tempListForSpawnPoints = new();

        /// <summary>
        /// 初始化时随机化出生点顺序
        /// </summary>
        protected override void Awake()
        {
            RandomizeSpawnPoints(m_teamASpawnPoints.Length, ref m_teamARandomSpawnOrder);
            RandomizeSpawnPoints(m_teamBSpawnPoints.Length, ref m_teamBRandomSpawnOrder);
            base.Awake();
        }

        /// <summary>
        /// 随机化出生点顺序
        /// </summary>
        /// <param name="length">出生点数量</param>
        /// <param name="randomQueue">存储随机顺序的队列</param>
        private void RandomizeSpawnPoints(int length, ref Queue<int> randomQueue)
        {
            m_tempListForSpawnPoints.Clear();
            for (var i = 0; i < length; ++i)
            {
                m_tempListForSpawnPoints.Add(i);
            }

            var n = length;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n);
                var value = m_tempListForSpawnPoints[k];
                m_tempListForSpawnPoints[k] = m_tempListForSpawnPoints[n];
                m_tempListForSpawnPoints[n] = value;
            }

            randomQueue.Clear();
            for (var i = 0; i < length; ++i)
            {
                randomQueue.Enqueue(m_tempListForSpawnPoints[i]);
            }

            m_tempListForSpawnPoints.Clear();
        }

        /// <summary>
        /// 生成玩家或观众
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="isSpectator">是否为观众</param>
        /// <param name="playerPos">玩家位置</param>
        /// <returns>生成的网络对象</returns>
        public override NetworkObject SpawnPlayer(ulong clientId, string playerId, bool isSpectator, Vector3 playerPos)
        {
            if (isSpectator)
            {
                var spectator = SpawnSpectator(clientId, playerId, playerPos);
                return spectator;
            }

            ArenaSessionManager.Instance.SetupPlayerData(clientId, playerId, new ArenaPlayerData(clientId, playerId));
            var playerData = ArenaSessionManager.Instance.GetPlayerData(playerId).Value;
            // 根据游戏阶段设置生成数据
            GetSpawnData(ref playerData, playerPos, out var position, out var rotation, out var team,
                out var color, out var spawnTeam);

            var player = Instantiate(m_playerPrefab, position, rotation);
            player.SpawnAsPlayerObject(clientId);
            player.GetComponent<NetworkedTeam>().MyTeam = team;

            // 生成左手手套
            var leftArmatureNet = Instantiate(m_gloveArmaturePrefab, Vector3.down, Quaternion.identity);
            var leftArmature = leftArmatureNet.GetComponent<GloveArmatureNetworking>();
            leftArmature.Side = Glove.GloveSide.Left;
            leftArmatureNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;
            var leftHandNet = Instantiate(m_gloveHandPrefab, Vector3.down, Quaternion.identity);
            var leftHand = leftHandNet.GetComponent<GloveNetworking>();
            leftHand.Side = Glove.GloveSide.Left;
            leftHandNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;

            // 生成右手手套
            var rightArmatureNet = Instantiate(m_gloveArmaturePrefab, Vector3.down, Quaternion.identity);
            var rightArmature = rightArmatureNet.GetComponent<GloveArmatureNetworking>();
            rightArmature.Side = Glove.GloveSide.Right;
            rightArmatureNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;
            var rightHandNet = Instantiate(m_gloveHandPrefab, Vector3.down, Quaternion.identity);
            var rightHand = rightHandNet.GetComponent<GloveNetworking>();
            rightHand.Side = Glove.GloveSide.Right;
            rightHandNet.GetComponent<TeamColoringNetComponent>().TeamColor = color;
            rightArmatureNet.SpawnWithOwnership(clientId);
            rightHandNet.SpawnWithOwnership(clientId);
            leftArmatureNet.SpawnWithOwnership(clientId);
            leftHandNet.SpawnWithOwnership(clientId);

            // 设置玩家控制器的手套引用
            player.GetComponent<PlayerControllerNetwork>().ArmatureLeft = leftArmature;
            player.GetComponent<PlayerControllerNetwork>().ArmatureRight = rightArmature;
            player.GetComponent<PlayerControllerNetwork>().GloveLeft = leftHand;
            player.GetComponent<PlayerControllerNetwork>().GloveRight = rightHand;

            playerData.SelectedTeam = team;
            m_gameManager.UpdatePlayerTeam(clientId, spawnTeam);
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);

            return player;
        }

        /// <summary>
        /// 重置游戏结束后的出生点
        /// </summary>
        public void ResetPostGameSpawnPoints()
        {
            m_winnerSpawnPoints.Reset();
            m_loserSpawnPoints.Reset();
        }

        /// <summary>
        /// 重置游戏中的出生点
        /// </summary>
        public void ResetInGameSpawnPoints()
        {
            RandomizeSpawnPoints(m_teamASpawnPoints.Length, ref m_teamARandomSpawnOrder);
            RandomizeSpawnPoints(m_teamBSpawnPoints.Length, ref m_teamBRandomSpawnOrder);
        }

        /// <summary>
        /// 获取重生点位置和旋转
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="team">队伍</param>
        /// <param name="position">输出位置</param>
        /// <param name="rotation">输出旋转</param>
        public override void GetRespawnPoint(ulong clientId, NetworkedTeam.Team team,
            out Vector3 position, out Quaternion rotation)
        {
            var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId).Value;
            GetSpawnPositionForTeam(m_gameManager.CurrentPhase, team, ref playerData, out position, out rotation);
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
        }

        /// <summary>
        /// 切换观众视角的队伍
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="spectator">观众网络对象</param>
        /// <returns>新的出生点Transform</returns>
        public Transform SwitchSpectatorSide(ulong clientId, SpectatorNetwork spectator)
        {
            var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId).Value;
            if (!playerData.IsSpectator)
            {
                return null;
            }

            var spawnPoints = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_spectatorASpawnPoints
                : m_spectatorBSpawnPoints;
            spawnPoints.ReleaseSpawnPoint(playerData.SpawnPointIndex);

            // 切换队伍
            playerData.SelectedTeam = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? NetworkedTeam.Team.TeamB
                : NetworkedTeam.Team.TeamA;
            spectator.TeamSideColor = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_gameManager.TeamAColor
                : m_gameManager.TeamBColor;
            spawnPoints = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                ? m_spectatorASpawnPoints
                : m_spectatorBSpawnPoints;
            var newLocation = spawnPoints.ReserveRandomSpawnPoint(out var spawnIndex);
            playerData.SpawnPointIndex = spawnIndex;
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
            return newLocation;
        }

        /// <summary>
        /// 处理客户端断开连接
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        protected override void OnClientDisconnected(ulong clientId)
        {
            var playerData = ArenaSessionManager.Instance.GetPlayerData(clientId);
            if (playerData.HasValue)
            {
                var data = playerData.Value;
                data.IsConnected = false;
                if (m_gameManager.CurrentPhase == GameManager.GamePhase.PostGame)
                {
                    if (data.SpawnPointIndex > 0)
                    {
                        if (data.PostGameWinnerSide)
                        {
                            m_winnerSpawnPoints.ReleaseSpawnPoint(data.SpawnPointIndex);
                        }
                        else
                        {
                            m_loserSpawnPoints.ReleaseSpawnPoint(data.SpawnPointIndex);
                        }
                    }
                }

                ArenaSessionManager.Instance.SetPlayerData(clientId, data);
            }
        }

        /// <summary>
        /// 生成观众
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="playerPos">玩家位置</param>
        /// <returns>生成的观众网络对象</returns>
        private NetworkObject SpawnSpectator(ulong clientId, string playerId, Vector3 playerPos)
        {
            ArenaSessionManager.Instance.SetupPlayerData(clientId, playerId,
                new ArenaPlayerData(clientId, playerId, true));
            var playerData = ArenaSessionManager.Instance.GetPlayerData(playerId).Value;
            Transform spawnPoint;
            if (playerData.SelectedTeam == NetworkedTeam.Team.NoTeam)
            {
                bool useA;
                var findClosestSpawnPoint = false;
                if (playerPos == Vector3.zero)
                {
                    useA = Random.Range(0, 2) == 0;
                }
                else
                {
                    useA = playerPos.z < 0;
                    findClosestSpawnPoint = true;
                }

                var spawnPoints = useA ? m_spectatorASpawnPoints : m_spectatorBSpawnPoints;
                spawnPoint = findClosestSpawnPoint
                    ? spawnPoints.ReserveClosestSpawnPoint(playerPos, out var spawnIndex)
                    : spawnPoints.ReserveRandomSpawnPoint(out spawnIndex);

                if (spawnPoint == null)
                {
                    useA = !useA;
                    spawnPoints = useA ? m_spectatorASpawnPoints : m_spectatorBSpawnPoints;
                    spawnPoint = spawnPoints.ReserveRandomSpawnPoint(out spawnIndex);
                }

                playerData.SelectedTeam = useA ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
                playerData.SpawnPointIndex = spawnIndex;
            }
            else
            {
                var spawnPoints = playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                    ? m_spectatorASpawnPoints
                    : m_spectatorBSpawnPoints;
                if (playerData.SpawnPointIndex < 0)
                {
                    spawnPoint = spawnPoints.ReserveRandomSpawnPoint(out var spawnIndex);
                    playerData.SpawnPointIndex = spawnIndex;
                }
                else
                {
                    spawnPoint = spawnPoints.GetSpawnPoint(playerData.SpawnPointIndex, true);
                }
            }

            var position = spawnPoint.position;
            var rotation = spawnPoint.rotation;
            var spectator = Instantiate(m_spectatorPrefab, position, rotation);
            spectator.GetComponent<SpectatorNetwork>().TeamSideColor =
                playerData.SelectedTeam == NetworkedTeam.Team.TeamA
                    ? m_gameManager.TeamAColor
                    : m_gameManager.TeamBColor;
            spectator.SpawnAsPlayerObject(clientId);
            ArenaSessionManager.Instance.SetPlayerData(clientId, playerData);
            return spectator;
        }

        /// <summary>
        /// 获取生成数据
        /// </summary>
        /// <param name="playerData">玩家数据引用</param>
        /// <param name="currentPos">当前位置</param>
        /// <param name="position">输出位置</param>
        /// <param name="rotation">输出旋转</param>
        /// <param name="team">输出队伍</param>
        /// <param name="teamColor">输出队伍颜色</param>
        /// <param name="spawnTeam">输出生成队伍</param>
        private void GetSpawnData(ref ArenaPlayerData playerData, Vector3 currentPos, out Vector3 position,
            out Quaternion rotation, out NetworkedTeam.Team team, out TeamColor teamColor,
            out NetworkedTeam.Team spawnTeam)
        {
            var currentPhase = m_gameManager.CurrentPhase;
            team = currentPhase switch
            {
                GameManager.GamePhase.InGame or GameManager.GamePhase.CountDown => GetTeam(playerData, currentPos),
                GameManager.GamePhase.PostGame => GetTeam(playerData, currentPos),
                GameManager.GamePhase.PreGame => NetworkedTeam.Team.NoTeam,
                _ => NetworkedTeam.Team.NoTeam,
            };
            spawnTeam = team;
            if (spawnTeam == NetworkedTeam.Team.NoTeam)
            {
                spawnTeam = GetTeam(playerData, currentPos);
            }

            GetSpawnPositionForTeam(currentPhase, spawnTeam, ref playerData, out position, out rotation);

            teamColor = GetTeamColor(spawnTeam);
        }

        /// <summary>
        /// 获取玩家队伍
        /// </summary>
        /// <param name="playerData">玩家数据</param>
        /// <param name="currentPos">当前位置</param>
        /// <returns>分配的队伍</returns>
        private NetworkedTeam.Team GetTeam(ArenaPlayerData playerData, Vector3 currentPos)
        {
            if (playerData.SelectedTeam != NetworkedTeam.Team.NoTeam)
            {
                return playerData.SelectedTeam;
            }

            NetworkedTeam.Team team;
            if (currentPos == Vector3.zero)
            {
                team = m_gameManager.GetTeamWithLeastPlayers();
            }
            else
            {
                if (m_gameManager.CurrentPhase == GameManager.GamePhase.PostGame)
                {
                    var winningTeam = GameState.Instance.Score.GetWinningTeam();
                    if (winningTeam == NetworkedTeam.Team.NoTeam)
                    {
                        winningTeam = NetworkedTeam.Team.TeamA;
                    }

                    var losingTeam = winningTeam == NetworkedTeam.Team.TeamA
                        ? NetworkedTeam.Team.TeamB
                        : NetworkedTeam.Team.TeamA;
                    team = Mathf.Abs(currentPos.z - m_winnerSpawnPoints.transform.position.z) >=
                           Mathf.Abs(currentPos.z - m_loserSpawnPoints.transform.position.z)
                        ? winningTeam
                        : losingTeam;
                }
                else
                {
                    team = currentPos.z < 0 ? NetworkedTeam.Team.TeamA : NetworkedTeam.Team.TeamB;
                }
            }

            return team;
        }

        /// <summary>
        /// 根据队伍获取生成位置
        /// </summary>
        /// <param name="gamePhase">游戏阶段</param>
        /// <param name="team">队伍</param>
        /// <param name="playerData">玩家数据引用</param>
        /// <param name="position">输出位置</param>
        /// <param name="rotation">输出旋转</param>
        private void GetSpawnPositionForTeam(GameManager.GamePhase gamePhase, NetworkedTeam.Team team,
            ref ArenaPlayerData playerData,
            out Vector3 position, out Quaternion rotation)
        {
            if (gamePhase == GameManager.GamePhase.PostGame)
            {
                var winningTeam = GameState.Instance.Score.GetWinningTeam();
                var useWin = winningTeam == team;
                if (winningTeam == NetworkedTeam.Team.NoTeam)
                {
                    useWin = m_tieAlternateToWin;
                    m_tieAlternateToWin = !m_tieAlternateToWin;
                }

                Transform trans = null;
                if (useWin)
                {
                    trans = m_winnerSpawnPoints.ReserveRandomSpawnPoint(out var index);
                    playerData.PostGameWinnerSide = true;
                    playerData.SpawnPointIndex = index;
                }

                if (trans == null)
                {
                    trans = m_loserSpawnPoints.ReserveRandomSpawnPoint(out var index);
                    playerData.PostGameWinnerSide = false;
                    playerData.SpawnPointIndex = index;
                }

                position = trans.position;
                rotation = trans.rotation;

                return;
            }

            if (team == NetworkedTeam.Team.TeamA)
            {
                if (m_teamARandomSpawnOrder.Count <= 0)
                {
                    RandomizeSpawnPoints(m_teamASpawnPoints.Length, ref m_teamARandomSpawnOrder);
                }

                var point = m_teamASpawnPoints[m_teamARandomSpawnOrder.Dequeue()];
                position = point.position;
                rotation = point.rotation;
            }
            else
            {
                if (m_teamBRandomSpawnOrder.Count <= 0)
                {
                    RandomizeSpawnPoints(m_teamBSpawnPoints.Length, ref m_teamBRandomSpawnOrder);
                }

                var point = m_teamBSpawnPoints[m_teamBRandomSpawnOrder.Dequeue()];
                position = point.position;
                rotation = point.rotation;
            }
        }

        /// <summary>
        /// 获取队伍颜色
        /// </summary>
        /// <param name="team">队伍</param>
        /// <returns>队伍颜色</returns>
        private TeamColor GetTeamColor(NetworkedTeam.Team team)
        {
            var useTeamA = team == NetworkedTeam.Team.TeamA;
            var color = useTeamA ? m_gameManager.TeamAColor : m_gameManager.TeamBColor;
            return color;
        }
    }
}
