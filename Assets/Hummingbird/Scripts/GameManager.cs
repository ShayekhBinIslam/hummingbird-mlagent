using System.Collections;
using UnityEngine;

/// <summary>
/// Manages game logic and controls the UI
/// </summary>
public class GameManager : MonoBehaviour
{
    [Tooltip("Game ends when an agent collects this much nectar")]
    public float maxNectar = 8f;

    [Tooltip("Game ends after this many seconds have elapsed")]
    public float timerAmount = 60f;

    [Tooltip("The UI Controller")]
    public UIController uiController;

    [Tooltip("The player hummingbird")]
    public HummingbirdAgent player;

    [Tooltip("The ML-Agent opponent hummingbird")]
    public HummingbirdAgent opponent;

    [Tooltip("The flower area")]
    public FlowerArea flowerArea;

    [Tooltip("The main camera for the scene")]
    public Camera mainCamera;

    // When the game timer started
    private float gameTimerStartTime;

    /// <summary>
    /// All possible game states
    /// </summary>
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Gameover
    }

    /// <summary>
    /// The current game state
    /// </summary>
    public GameState State { get; private set; } = GameState.Default;

    /// <summary>
    /// Gets the time remaining in the game
    /// </summary>
    public float TimeRemaining
    {
        get
        {
            if (State == GameState.Playing)
            {
                float timeRemaining = timerAmount - (Time.time - gameTimerStartTime);
                return Mathf.Max(0f, timeRemaining);
            }
            else
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// Handles a button click in different states
    /// </summary>
    public void ButtonClicked()
    {
        if (State == GameState.Gameover)
        {
            // In the Gameover state, button click should go to the main menu
            MainMenu();
        }
        else if (State == GameState.MainMenu)
        {
            // In the MainMenu state, button click should start the game
            StartCoroutine(StartGame());
        }
        else
        {
            Debug.LogWarning("Button clicked in unexpected state: " + State.ToString());
        }
    }

    /// <summary>
    /// Called when the game starts
    /// </summary>
    private void Start()
    {
        // Subscribe to button click events from the UI
        uiController.OnButtonClicked += ButtonClicked;

        // Start the main menu
        MainMenu();
    }

    /// <summary>
    /// Called on destroy
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from button click events from the UI
        uiController.OnButtonClicked -= ButtonClicked;
    }

    /// <summary>
    /// Shows the main menu
    /// </summary>
    private void MainMenu()
    {
        // Set the state to "main menu"
        State = GameState.MainMenu;

        // Update the UI
        uiController.ShowBanner("");
        uiController.ShowButton("Start");

        // Use the main camera, disable agent cameras
        mainCamera.gameObject.SetActive(true);
        player.agentCamera.gameObject.SetActive(false);
        opponent.agentCamera.gameObject.SetActive(false); // Never turn this back on

        // Reset the flowers
        flowerArea.ResetFlowers();

        // Reset the agents
        player.OnEpisodeBegin();
        opponent.OnEpisodeBegin();

        // Freeze the agents
        player.FreezeAgent();
        opponent.FreezeAgent();
    }

    /// <summary>
    /// Starts the game with a countdown
    /// </summary>
    /// <returns>IEnumerator</returns>
    private IEnumerator StartGame()
    {
        // Set the state to "preparing"
        State = GameState.Preparing;

        // Update the UI (hide it)
        uiController.ShowBanner("");
        uiController.HideButton();

        // Use the player camera, disable the main camera
        mainCamera.gameObject.SetActive(false);
        player.agentCamera.gameObject.SetActive(true);

        // Show countdown
        uiController.ShowBanner("3");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("2");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("1");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("Go!");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("");

        // Set the state to "playing"
        State = GameState.Playing;

        // Start the game timer
        gameTimerStartTime = Time.time;

        // Unfreeze the agents
        player.UnfreezeAgent();
        opponent.UnfreezeAgent();
    }

    /// <summary>
    /// Ends the game
    /// </summary>
    private void EndGame()
    {
        // Set the game state to "game over"
        State = GameState.Gameover;

        // Freeze the agents
        player.FreezeAgent();
        opponent.FreezeAgent();

        // Update banner text depending on win/lose
        if (player.NectarObtained >= opponent.NectarObtained )
        {
            uiController.ShowBanner("You win!");
        }
        else
        {
            uiController.ShowBanner("ML-Agent wins!");
        }

        // Update button text
        uiController.ShowButton("Main Menu");
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    private void Update()
    {
        if (State == GameState.Playing)
        {
            // Check to see if time has run out or either agent got the max nectar amount
            if (TimeRemaining <= 0f ||
                player.NectarObtained >= maxNectar ||
                opponent.NectarObtained >= maxNectar)
            {
                EndGame();
            }

            // Update the timer and nectar progress bars
            uiController.SetTimer(TimeRemaining);
            uiController.SetPlayerNectar(player.NectarObtained / maxNectar);
            uiController.SetOpponentNectar(opponent.NectarObtained / maxNectar);
        }
        else if (State == GameState.Preparing || State == GameState.Gameover)
        {
            // Update the timer
            uiController.SetTimer(TimeRemaining);
        }
        else
        {
            // Hide the timer
            uiController.SetTimer(-1f);

            // Update the progress bars
            uiController.SetPlayerNectar(0f);
            uiController.SetOpponentNectar(0f);
        }

    }
}
