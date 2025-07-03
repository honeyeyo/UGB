using UnityEngine;
using PongHub.UI;
using PongHub.Core;

namespace PongHub.Core.Tests
{
    /// <summary>
    /// Table menu system test script
    /// Used to test and verify table menu system functionality
    /// </summary>
    public class TableMenuSystemTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField]
        [Tooltip("Enable keyboard shortcuts for testing menu functionality")]
        private bool enableKeyboardTesting = true;

        [SerializeField]
        [Tooltip("Enable automatic testing sequence that cycles through menu functions")]
        private bool enableAutoTesting = false;

        [SerializeField]
        [Tooltip("Time interval between automatic test steps in seconds")]
        private float autoTestInterval = 3f;

        [Header("References")]
        [SerializeField]
        [Tooltip("Table menu system component to test")]
        private TableMenuSystem tableMenuSystem;

        [SerializeField]
        [Tooltip("VR menu interaction component to test")]
        private VRMenuInteraction vrMenuInteraction;

        [SerializeField]
        [Tooltip("Game mode manager for testing mode switching")]
        private GameModeManager gameModeManager;

        // Test state
        private float lastAutoTestTime;
        private int currentTestStep = 0;
        private bool isTestingInProgress = false;

        private void Start()
        {
            // Find components
            if (tableMenuSystem == null)
                tableMenuSystem = FindObjectOfType<TableMenuSystem>();

            if (vrMenuInteraction == null)
                vrMenuInteraction = FindObjectOfType<VRMenuInteraction>();

            if (gameModeManager == null)
                gameModeManager = FindObjectOfType<GameModeManager>();

            // Setup event listeners
            SetupEventListeners();

            Debug.Log("TableMenuSystemTest: Test script started");
            LogTestInstructions();
        }

        private void Update()
        {
            // Keyboard testing
            if (enableKeyboardTesting)
            {
                HandleKeyboardInput();
            }

            // Auto testing
            if (enableAutoTesting && !isTestingInProgress)
            {
                HandleAutoTesting();
            }
        }

        private void SetupEventListeners()
        {
            if (tableMenuSystem != null)
            {
                tableMenuSystem.OnMenuVisibilityChanged += OnMenuVisibilityChanged;
                tableMenuSystem.OnPanelChanged += OnPanelChanged;
            }

            if (gameModeManager != null)
            {
                gameModeManager.OnModeChanged += OnGameModeChanged;
            }
        }

        private void HandleKeyboardInput()
        {
            // M key - Toggle menu display
            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                TestToggleMenu();
            }

            // Number keys - Switch panels
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                TestShowPanel(MenuPanel.Main);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                TestShowPanel(MenuPanel.GameMode);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                TestShowPanel(MenuPanel.Settings);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4))
            {
                TestShowPanel(MenuPanel.Audio);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5))
            {
                TestShowPanel(MenuPanel.Exit);
            }

            // L key - Test switch to Local mode
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                TestSwitchGameMode(GameMode.Local);
            }

            // N key - Test switch to Network mode
            if (UnityEngine.Input.GetKeyDown(KeyCode.N))
            {
                TestSwitchGameMode(GameMode.Network);
            }

            // T key - Run full test sequence
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                StartCoroutine(RunFullTestSequence());
            }
        }

        private void HandleAutoTesting()
        {
            if (Time.time - lastAutoTestTime >= autoTestInterval)
            {
                lastAutoTestTime = Time.time;
                StartCoroutine(RunAutoTestStep());
            }
        }

        private void TestToggleMenu()
        {
            if (tableMenuSystem != null)
            {
                Debug.Log("Test: Toggle menu display");
                tableMenuSystem.ToggleMenu();
            }
            else
            {
                Debug.LogError("Test: TableMenuSystem not found");
            }
        }

        private void TestShowPanel(MenuPanel panel)
        {
            if (tableMenuSystem != null)
            {
                Debug.Log($"Test: Show panel {panel}");

                // If menu is not visible, show menu first
                if (!tableMenuSystem.IsMenuVisible)
                {
                    tableMenuSystem.ShowMenu();
                }

                tableMenuSystem.ShowPanel(panel);
            }
            else
            {
                Debug.LogError("Test: TableMenuSystem not found");
            }
        }

        private void TestSwitchGameMode(GameMode mode)
        {
            if (gameModeManager != null)
            {
                Debug.Log($"Test: Switch game mode to {mode}");
                gameModeManager.SwitchToMode(mode);
            }
            else
            {
                Debug.LogError("Test: GameModeManager not found");
            }
        }

        private System.Collections.IEnumerator RunFullTestSequence()
        {
            if (isTestingInProgress)
            {
                Debug.LogWarning("Test: Test sequence in progress, skipping");
                yield break;
            }

            isTestingInProgress = true;
            Debug.Log("Test: Start full test sequence");

            // 1. Show menu
            TestToggleMenu();
            yield return new WaitForSeconds(1f);

            // 2. Test all panels
            MenuPanel[] panels = { MenuPanel.Main, MenuPanel.Settings, MenuPanel.Audio, MenuPanel.Exit };
            foreach (var panel in panels)
            {
                TestShowPanel(panel);
                yield return new WaitForSeconds(1f);
            }

            // 3. Test game mode switching
            TestSwitchGameMode(GameMode.Local);
            yield return new WaitForSeconds(1f);

            TestSwitchGameMode(GameMode.Network);
            yield return new WaitForSeconds(1f);

            // 4. Hide menu
            if (tableMenuSystem != null && tableMenuSystem.IsMenuVisible)
            {
                tableMenuSystem.HideMenu();
            }

            Debug.Log("Test: Full test sequence completed");
            isTestingInProgress = false;
        }

        private System.Collections.IEnumerator RunAutoTestStep()
        {
            if (isTestingInProgress) yield break;

            isTestingInProgress = true;

            switch (currentTestStep)
            {
                case 0:
                    Debug.Log("AutoTest: Step 1 - Show menu");
                    TestToggleMenu();
                    break;

                case 1:
                    Debug.Log("AutoTest: Step 2 - Switch to settings panel");
                    TestShowPanel(MenuPanel.Settings);
                    break;

                case 2:
                    Debug.Log("AutoTest: Step 3 - Switch to main panel");
                    TestShowPanel(MenuPanel.Main);
                    break;

                case 3:
                    Debug.Log("AutoTest: Step 4 - Hide menu");
                    if (tableMenuSystem != null && tableMenuSystem.IsMenuVisible)
                    {
                        tableMenuSystem.HideMenu();
                    }
                    break;

                default:
                    currentTestStep = -1; // Reset
                    break;
            }

            currentTestStep++;
            yield return new WaitForSeconds(0.1f);
            isTestingInProgress = false;
        }

        private void OnMenuVisibilityChanged(bool visible)
        {
            Debug.Log($"Event: Menu visibility changed - {(visible ? "Visible" : "Hidden")}");
        }

        private void OnPanelChanged(MenuPanel panel)
        {
            Debug.Log($"Event: Panel changed - {panel}");
        }

        private void OnGameModeChanged(GameMode newMode, GameMode oldMode)
        {
            Debug.Log($"Event: Game mode changed - {oldMode} -> {newMode}");
        }

        private void LogTestInstructions()
        {
            Debug.Log("=== Table Menu System Test Instructions ===");
            Debug.Log("Keyboard shortcuts:");
            Debug.Log("M - Toggle menu display/hide");
            Debug.Log("1 - Show main menu panel");
            Debug.Log("2 - Show game mode panel");
            Debug.Log("3 - Show settings panel");
            Debug.Log("4 - Show audio panel");
            Debug.Log("5 - Show exit panel");
            Debug.Log("L - Switch to single player mode");
            Debug.Log("N - Switch to multiplayer mode");
            Debug.Log("T - Run full test sequence");
            Debug.Log("==========================================");
        }

        public void TestMenuPosition()
        {
            if (tableMenuSystem != null)
            {
                Vector3 menuPos = tableMenuSystem.GetTableMenuPosition();
                Debug.Log($"Test: Menu position - {menuPos}");
            }
        }

        public void TestVRInteraction()
        {
            if (vrMenuInteraction != null)
            {
                Debug.Log("Test: Simulate VR menu button");
                vrMenuInteraction.OnMenuButtonPressed();
            }
        }

        private void OnDestroy()
        {
            // Clean up event listeners
            if (tableMenuSystem != null)
            {
                tableMenuSystem.OnMenuVisibilityChanged -= OnMenuVisibilityChanged;
                tableMenuSystem.OnPanelChanged -= OnPanelChanged;
            }

            if (gameModeManager != null)
            {
                gameModeManager.OnModeChanged -= OnGameModeChanged;
            }
        }

        #if UNITY_EDITOR
        [UnityEditor.MenuItem("PongHub/Test/Table Menu System")]
        public static void CreateTestObject()
        {
            var testObj = new GameObject("TableMenuSystemTest");
            testObj.AddComponent<TableMenuSystemTest>();
            UnityEditor.Selection.activeGameObject = testObj;
            Debug.Log("TableMenuSystemTest object created");
        }
        #endif
    }
}