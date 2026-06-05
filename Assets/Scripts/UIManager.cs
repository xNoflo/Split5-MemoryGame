using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Transform boardRoot;
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private TMP_Text movesLabel;
    [SerializeField] private TMP_Text roundLabel;
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text endGameBodyLabel;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button restartGameButton;
    [SerializeField] private Button endGameMainMenuButton;

    private MemoryGameManager _gameManager;

    public Transform BoardRoot => boardRoot;

    public void Initialize(MemoryGameManager gameManager)
    {
        _gameManager = gameManager;
        // edited by Noah: this manager now drives scene-authored UI references only, instead of generating canvas objects in code.
        WireButtons();
        UpdateHud(30, 0, 0, 8, 0, 16, 1, 2);
    }

    public void UpdateHud(int secondsRemaining, int moves, int roundPairsMatched, int pairsPerRound, int totalPairsMatched, int totalPairs, int round, int totalRounds)
    {
        if (timerLabel != null)
        {
            timerLabel.text = $"Timer\n{secondsRemaining}s";
        }

        if (movesLabel != null)
        {
            movesLabel.text = $"Moves\n{moves}";
        }

        if (roundLabel != null)
        {
            roundLabel.text = $"Round {round}/{totalRounds}\nBoard {roundPairsMatched}/{pairsPerRound}\nTotal {totalPairsMatched}/{totalPairs}";
        }
    }

    public void ShowMainMenu()
    {
        SetActiveIfPresent(hudPanel, false);
        SetActiveIfPresent(mainMenuPanel, true);
        SetActiveIfPresent(endGamePanel, false);
    }

    public void ShowEndScreen(int totalPairsMatched, int totalPairs, int roundsCompleted, int totalRounds, bool clearedAllRounds)
    {
        if (endGameBodyLabel != null)
        {
            string headline = clearedAllRounds ? "Run Complete" : "Time's Up";
            endGameBodyLabel.text = $"{headline}\nPairs matched: {totalPairsMatched}/{totalPairs}\nRounds reached: {roundsCompleted}/{totalRounds}";
        }

        SetActiveIfPresent(hudPanel, false);
        SetActiveIfPresent(mainMenuPanel, false);
        SetActiveIfPresent(endGamePanel, true);
    }

    public void HideOverlay()
    {
        SetActiveIfPresent(hudPanel, true);
        SetActiveIfPresent(mainMenuPanel, false);
        SetActiveIfPresent(endGamePanel, false);
    }

    private void WireButtons()
    {
        WireButton(menuButton, _gameManager.ReturnToMainMenu);
        WireButton(startGameButton, _gameManager.StartGame);
        WireButton(restartGameButton, _gameManager.RestartGame);
        WireButton(endGameMainMenuButton, _gameManager.ReturnToMainMenu);
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetActiveIfPresent(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
