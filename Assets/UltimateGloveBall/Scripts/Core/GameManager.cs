using UnityEngine;
using System;

namespace PongHub.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver
        }

        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetGameState(GameState.MainMenu);
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.MainMenu:
                    HandleMainMenuState();
                    break;
                case GameState.Playing:
                    HandlePlayingState();
                    break;
                case GameState.Paused:
                    HandlePausedState();
                    break;
                case GameState.GameOver:
                    HandleGameOverState();
                    break;
            }
        }

        private void HandleMainMenuState()
        {
            Time.timeScale = 0f;
            // TODO: 显示主菜单UI
        }

        private void HandlePlayingState()
        {
            Time.timeScale = 1f;
            // TODO: 隐藏菜单UI，开始游戏
        }

        private void HandlePausedState()
        {
            Time.timeScale = 0f;
            // TODO: 显示暂停菜单
        }

        private void HandleGameOverState()
        {
            Time.timeScale = 0f;
            // TODO: 显示游戏结束UI
        }

        public void StartGame()
        {
            SetGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            SetGameState(GameState.Paused);
        }

        public void ResumeGame()
        {
            SetGameState(GameState.Playing);
        }

        public void EndGame()
        {
            SetGameState(GameState.GameOver);
        }

        public void ReturnToMainMenu()
        {
            SetGameState(GameState.MainMenu);
        }
    }
}