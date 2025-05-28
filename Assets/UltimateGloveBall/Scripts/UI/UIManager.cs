using UnityEngine;
using System.Threading.Tasks;
using PongHub.Core;

namespace PongHub.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager s_instance;
        public static UIManager Instance => s_instance;

        [Header("UI面板")]
        [SerializeField] private MainMenuPanel m_mainMenuPanel;
        [SerializeField] private SettingsPanel m_settingsPanel;
        [SerializeField] private PauseMenuPanel m_pauseMenuPanel;
        [SerializeField] private ScoreboardPanel m_scoreboardPanel;

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            InitializePanels();
        }

        private void InitializePanels()
        {
            if (m_mainMenuPanel != null)
            {
                m_mainMenuPanel.gameObject.SetActive(true);
            }

            if (m_settingsPanel != null)
            {
                m_settingsPanel.gameObject.SetActive(false);
            }

            if (m_pauseMenuPanel != null)
            {
                m_pauseMenuPanel.gameObject.SetActive(false);
            }

            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.gameObject.SetActive(false);
            }
        }

        public void ShowMainMenu()
        {
            HideAllPanels();
            if (m_mainMenuPanel != null)
            {
                m_mainMenuPanel.gameObject.SetActive(true);
            }
        }

        public void ShowSettings()
        {
            HideAllPanels();
            if (m_settingsPanel != null)
            {
                m_settingsPanel.gameObject.SetActive(true);
            }
        }

        public void ShowPauseMenu()
        {
            if (m_pauseMenuPanel != null)
            {
                m_pauseMenuPanel.gameObject.SetActive(true);
            }
        }

        public void HidePauseMenu()
        {
            if (m_pauseMenuPanel != null)
            {
                m_pauseMenuPanel.gameObject.SetActive(false);
            }
        }

        public void ShowScoreboard()
        {
            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.gameObject.SetActive(true);
            }
        }

        public void HideScoreboard()
        {
            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.gameObject.SetActive(false);
            }
        }

        private void HideAllPanels()
        {
            if (m_mainMenuPanel != null)
            {
                m_mainMenuPanel.gameObject.SetActive(false);
            }

            if (m_settingsPanel != null)
            {
                m_settingsPanel.gameObject.SetActive(false);
            }

            if (m_pauseMenuPanel != null)
            {
                m_pauseMenuPanel.gameObject.SetActive(false);
            }

            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.gameObject.SetActive(false);
            }
        }
    }
} 