using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MemoryGameManager : MonoBehaviour
{
    private const string DefaultCollectionPath = "MainCardCollection";
    private const int PairsPerRound = 8;
    private const float RunDurationSeconds = 30f;
    private const float MismatchDelaySeconds = 0.9f;
    private const float NextRoundDelaySeconds = 1.1f;

    [SerializeField] private string cardCollectionResourcePath = DefaultCollectionPath;
    [SerializeField] private CardCollection cardCollection;
    [SerializeField] private Transform gridParent;

    private readonly List<CardView> _activeCardViews = new();
    private readonly List<MediaEntry> _remainingMediaEntries = new();
    private readonly List<MediaEntry> _allMediaEntries = new();

    private UIManager _uiManager;
    private TMP_FontAsset _fontAsset;

    private CardView _firstSelection;
    private CardView _secondSelection;
    private bool _gameRunning;
    private bool _inputLocked;
    private float _timeRemaining;
    private int _currentRound;
    private int _totalRounds;
    private int _pairsThisRoundTarget;
    private int _movesThisRound;
    private int _pairsMatchedThisRound;
    private int _totalPairsMatched;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "NoahTestScene")
        {
            return;
        }

        if (FindFirstObjectByType<MemoryGameManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(MemoryGameManager));
        managerObject.AddComponent<MemoryGameManager>();
    }

    private void Awake()
    {
        if (cardCollection == null)
        {
            cardCollection = Resources.Load<CardCollection>(cardCollectionResourcePath);
        }

        _uiManager = FindFirstObjectByType<UIManager>();
        if (_uiManager == null)
        {
            // edited by Noah: NoahTestScene is expected to own the gameplay canvas and UIManager component directly in the scene.
            Debug.LogError("UIManager is missing from NoahTestScene. Add it to the scene-authored canvas before playing.");
        }

        _fontAsset = FindFirstObjectByType<TextMeshProUGUI>()?.font;
    }

    private void Start()
    {
        if (_uiManager == null)
        {
            enabled = false;
            return;
        }

        EnsurePlayableCardData();
        ValidatePlayableMediaPool();
        _uiManager.Initialize(this);
        _fontAsset = _uiManager.ActiveFont != null ? _uiManager.ActiveFont : _fontAsset;
        gridParent = _uiManager.BoardRoot;
        ClearExistingCards();
        _uiManager.ShowMainMenu();
    }

    private void Update()
    {
        if (!_gameRunning)
        {
            return;
        }

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            UpdateHud();
            EndGame(false);
            return;
        }

        UpdateHud();
    }

    public void StartGame()
    {
        // edited by Noah: reset all run state here so both boards share one 30 second timer from start to finish.
        StopAllCoroutines();
        EnsurePlayableCardData();
        ValidatePlayableMediaPool();
        _remainingMediaEntries.Clear();
        _remainingMediaEntries.AddRange(_allMediaEntries);
        Shuffle(_remainingMediaEntries);

        _currentRound = 0;
        _totalPairsMatched = 0;
        _gameRunning = true;
        _inputLocked = false;
        _timeRemaining = RunDurationSeconds;
        _firstSelection = null;
        _secondSelection = null;

        if (_remainingMediaEntries.Count == 0)
        {
            _uiManager.ShowEndScreen(0, 0, 0, 0, false);
            return;
        }

        _uiManager.HideOverlay();
        BeginNextRound();
    }

    public void ReturnToMainMenu()
    {
        StopAllCoroutines();
        _gameRunning = false;
        _inputLocked = false;
        _firstSelection = null;
        _secondSelection = null;
        ClearExistingCards();
        _uiManager.ShowMainMenu();
    }

    public void RestartGame()
    {
        StartGame();
    }

    private void BeginNextRound()
    {
        if (_remainingMediaEntries.Count == 0)
        {
            EndGame(true);
            return;
        }

        _currentRound++;
        _movesThisRound = 0;
        _pairsMatchedThisRound = 0;
        _inputLocked = false;
        _firstSelection = null;
        _secondSelection = null;

        List<CardSO> roundCards = BuildRoundCards();
        PopulateGrid(roundCards);
        _gameRunning = true;
        UpdateHud();
    }

    private List<CardSO> BuildRoundCards()
    {
        int pairCount = Mathf.Min(PairsPerRound, _remainingMediaEntries.Count);
        _totalRounds = Mathf.CeilToInt((float)_allMediaEntries.Count / PairsPerRound);
        _pairsThisRoundTarget = pairCount;

        List<CardSO> roundCards = new();
        for (int i = 0; i < pairCount; i++)
        {
            MediaEntry mediaEntry = _remainingMediaEntries[0];
            _remainingMediaEntries.RemoveAt(0);
            roundCards.Add(CreateRoundCard(mediaEntry, false));
            roundCards.Add(CreateRoundCard(mediaEntry, true));
        }

        Shuffle(roundCards);
        return roundCards;
    }

    private void PopulateGrid(List<CardSO> cards)
    {
        if (gridParent == null)
        {
            return;
        }

        ClearExistingCards();

        foreach (CardSO card in cards)
        {
            CardView cardView = CardView.Create(gridParent, card.cardName, _fontAsset);
            cardView.SetCard(card);
            cardView.Clicked += HandleCardClicked;
            _activeCardViews.Add(cardView);
        }
    }

    private void HandleCardClicked(CardView cardView)
    {
        if (!_gameRunning || _inputLocked || cardView == null || cardView.IsMatched || cardView.IsRevealed)
        {
            return;
        }

        cardView.Reveal();

        if (_firstSelection == null)
        {
            _firstSelection = cardView;
            return;
        }

        if (_firstSelection == cardView)
        {
            return;
        }

        _secondSelection = cardView;
        _movesThisRound++;
        UpdateHud();

        // edited by Noah: the selection resolver centralizes match, mismatch, round-complete, and timer-safe transitions.
        if (IsMatch(_firstSelection.CardData, _secondSelection.CardData))
        {
            _firstSelection.MarkMatched();
            _secondSelection.MarkMatched();
            _pairsMatchedThisRound++;
            _totalPairsMatched++;
            _firstSelection = null;
            _secondSelection = null;
            UpdateHud();

            if (_pairsMatchedThisRound >= _pairsThisRoundTarget)
            {
                StartCoroutine(AdvanceAfterRoundClear());
            }
        }
        else
        {
            StartCoroutine(HideMismatchedCards());
        }
    }

    private IEnumerator HideMismatchedCards()
    {
        _inputLocked = true;
        SetCardsInteractable(false);
        yield return new WaitForSeconds(MismatchDelaySeconds);

        _firstSelection?.HideImmediate();
        _secondSelection?.HideImmediate();
        _firstSelection = null;
        _secondSelection = null;
        _inputLocked = false;
        SetCardsInteractable(true);
    }

    private IEnumerator AdvanceAfterRoundClear()
    {
        _inputLocked = true;
        SetCardsInteractable(false);
        yield return new WaitForSeconds(NextRoundDelaySeconds);

        if (_remainingMediaEntries.Count > 0)
        {
            BeginNextRound();
        }
        else
        {
            EndGame(true);
        }
    }

    private void EndGame(bool clearedAllRounds)
    {
        StopAllCoroutines();
        _gameRunning = false;
        _inputLocked = true;
        SetCardsInteractable(false);

        // edited by Noah: the end screen reports total progress across both boards so the player sees the full 16-pair run result.
        _uiManager.ShowEndScreen(_totalPairsMatched, _allMediaEntries.Count, _currentRound, _totalRounds, clearedAllRounds);
    }

    private void UpdateHud()
    {
        _uiManager.UpdateHud(
            Mathf.CeilToInt(_timeRemaining),
            _movesThisRound,
            _pairsMatchedThisRound,
            _pairsThisRoundTarget,
            _totalPairsMatched,
            _allMediaEntries.Count,
            _currentRound,
            Mathf.Max(_totalRounds, 1));
    }

    private void ValidatePlayableMediaPool()
    {
        _allMediaEntries.RemoveAll(mediaEntry =>
            mediaEntry == null ||
            string.IsNullOrWhiteSpace(mediaEntry.Id) ||
            (string.IsNullOrWhiteSpace(mediaEntry.ImageFileName) && string.IsNullOrWhiteSpace(mediaEntry.VideoFileName)));
    }

    private void EnsurePlayableCardData()
    {
        _allMediaEntries.Clear();

        if (cardCollection == null)
        {
            cardCollection = Resources.Load<CardCollection>(cardCollectionResourcePath);
        }

        if (cardCollection != null && cardCollection.cards != null)
        {
            HashSet<string> addedKeys = new();
            foreach (CardSO card in cardCollection.cards)
            {
                if (card == null)
                {
                    continue;
                }

                MediaEntry mediaEntry = CreateMediaEntryFromCard(card);
                if (mediaEntry == null || !addedKeys.Add(mediaEntry.Key))
                {
                    continue;
                }

                _allMediaEntries.Add(mediaEntry);
            }
        }

        if (_allMediaEntries.Count > 0)
        {
            return;
        }

        // edited by Noah: if the collection is unavailable, build 16 unique media entries directly so each round can still deal 8 duplicated items into the 4x4 grid.
        AddVideoEntry("bossandceo", "mainlymannie-boss-and-ceo.mp4");
        AddVideoEntry("fuckher", "billy-porter-fuck.mp4");
        AddVideoEntry("hairitdontmove", "my-hair-my-hair-it-dont-move.mp4");
        AddVideoEntry("scuba", "nick-wilde-zootopia.mp4");
        AddVideoEntry("rightback", "karlee-girl-karlee.mp4");
        AddVideoEntry("thismorning", "shocked-surprised.mp4");
        AddVideoEntry("todaydrainedme", "zach-campbell-zachary-campbell.mp4");
        AddVideoEntry("toomuchice", "jayylaurent-too-much-ice.mp4");

        AddVideoEntry("anthonymackie", "anthony-mackie-straight-face.mp4");
        AddVideoEntry("beatinguplilo", "beating-up-beating-up-lilo.mp4");
        AddVideoEntry("dancemoves", "dance-moves.mp4");
        AddVideoEntry("howrude", "how-rude.mp4");
        AddVideoEntry("kabangu", "kabangu-upset.mp4");
        AddVideoEntry("podies", "po-dies.mp4");
        AddVideoEntry("spongebob", "spongebob.mp4");
        AddVideoEntry("szzybrb", "szzybrb-that-was-too-good.mp4");
    }

    private void AddImageEntry(string baseName, string imageFileName)
    {
        _allMediaEntries.Add(new MediaEntry(baseName, null, imageFileName, null, null));
    }

    private void AddVideoEntry(string baseName, string videoFileName)
    {
        _allMediaEntries.Add(new MediaEntry(baseName, null, null, null, videoFileName));
    }

    private static CardSO CreateRoundCard(MediaEntry mediaEntry, bool secondCard)
    {
        CardSO card = ScriptableObject.CreateInstance<CardSO>();
        string suffix = secondCard ? "2" : "1";
        string otherSuffix = secondCard ? "1" : "2";
        card.cardName = $"{mediaEntry.Id}_{suffix}";
        card.pairName = $"{mediaEntry.Id}_{otherSuffix}";
        card.cardImage = mediaEntry.Image;
        card.cardImageFileName = mediaEntry.ImageFileName;
        card.cardVideo = mediaEntry.Video;
        card.cardVideoFileName = mediaEntry.VideoFileName;
        return card;
    }

    private static MediaEntry CreateMediaEntryFromCard(CardSO card)
    {
        string imageFile = string.IsNullOrWhiteSpace(card.cardImageFileName) ? null : card.cardImageFileName;
        string videoFile = string.IsNullOrWhiteSpace(card.cardVideoFileName) ? null : card.cardVideoFileName;

        if (imageFile == null && videoFile == null)
        {
            if (card.cardImage != null)
            {
                imageFile = card.cardImage.name;
            }
            else if (card.cardVideo != null)
            {
                videoFile = card.cardVideo.name;
            }
        }

        if (imageFile == null && videoFile == null)
        {
            return null;
        }

        string id = DeriveBaseId(card.cardName, card.pairName, imageFile, videoFile);
        return new MediaEntry(id, card.cardImage, imageFile, card.cardVideo, videoFile);
    }

    private static bool IsMatch(CardSO first, CardSO second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        return NormalizeCardId(first.pairName) == NormalizeCardId(second.cardName)
            && NormalizeCardId(second.pairName) == NormalizeCardId(first.cardName);
    }

    private static string NormalizeCardId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string DeriveBaseId(string cardName, string pairName, string imageFileName, string videoFileName)
    {
        string preferred = StripPairSuffix(cardName);
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        preferred = StripPairSuffix(pairName);
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        string fileName = !string.IsNullOrWhiteSpace(videoFileName) ? videoFileName : imageFileName;
        return NormalizeCardId(System.IO.Path.GetFileNameWithoutExtension(fileName));
    }

    private static string StripPairSuffix(string value)
    {
        string normalized = NormalizeCardId(value);
        if (normalized.EndsWith("1"))
        {
            normalized = normalized[..^1];
        }
        else if (normalized.EndsWith("2"))
        {
            normalized = normalized[..^1];
        }

        if (normalized.EndsWith("_"))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    private void SetCardsInteractable(bool isInteractable)
    {
        foreach (CardView cardView in _activeCardViews)
        {
            if (cardView != null)
            {
                cardView.SetInteractable(isInteractable);
            }
        }
    }

    private void ClearExistingCards()
    {
        foreach (CardView cardView in _activeCardViews)
        {
            if (cardView != null)
            {
                cardView.Clicked -= HandleCardClicked;
            }
        }

        _activeCardViews.Clear();

        if (gridParent == null)
        {
            return;
        }

        List<GameObject> children = new();
        foreach (Transform child in gridParent)
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject child in children)
        {
            Destroy(child);
        }
    }

    private static void Shuffle<T>(List<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (items[i], items[randomIndex]) = (items[randomIndex], items[i]);
        }
    }

    private sealed class MediaEntry
    {
        public MediaEntry(string id, Sprite image, string imageFileName, VideoClip video, string videoFileName)
        {
            Id = NormalizeCardId(id);
            Image = image;
            ImageFileName = imageFileName;
            Video = video;
            VideoFileName = videoFileName;
        }

        public string Id { get; }
        public Sprite Image { get; }
        public string ImageFileName { get; }
        public VideoClip Video { get; }
        public string VideoFileName { get; }
        public string Key => !string.IsNullOrWhiteSpace(VideoFileName)
            ? $"video:{NormalizeCardId(VideoFileName)}"
            : $"image:{NormalizeCardId(ImageFileName)}";
    }
}
