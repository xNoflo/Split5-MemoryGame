using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CardView : MonoBehaviour
{
    private const string ImagesFolderName = "Images";
    private const string StreamingVideosFolderName = "Videos";
    private static readonly Color HiddenCardColor = new(0.1f, 0.2f, 0.24f, 0.96f);
    private static readonly Color MatchedCardColor = new(0.22f, 0.42f, 0.27f, 1f);
    private static readonly Color CoverColor = new(0.17f, 0.4f, 0.46f, 0.96f);

    private Image _backgroundImage;
    private Button _button;
    private Image _cardImage;
    private RawImage _videoImage;
    private VideoPlayer _videoPlayer;
    private Image _coverImage;
    private TextMeshProUGUI _coverLabel;

    private Sprite _runtimeSprite;
    private bool _isVideoPrepared;
    private bool _isVideoPlayable;
    private bool _hasVideoSource;
    private bool _hasRequestedPrepare;

    public CardSO CardData { get; private set; }
    public bool IsRevealed { get; private set; }
    public bool IsMatched { get; private set; }

    public event Action<CardView> Clicked;

    public static CardView Create(Transform parent, string cardName, TMP_FontAsset fontAsset)
    {
        GameObject root = new GameObject(cardName, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Button), typeof(CardView));
        root.transform.SetParent(parent, false);

        GameObject imageChild = new GameObject("CardImage", typeof(RectTransform), typeof(Image));
        imageChild.transform.SetParent(root.transform, false);

        GameObject videoChild = new GameObject("CardVideo", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer));
        videoChild.transform.SetParent(root.transform, false);

        GameObject coverChild = new GameObject("CardCover", typeof(RectTransform), typeof(Image));
        coverChild.transform.SetParent(root.transform, false);

        GameObject coverTextChild = new GameObject("CardCoverLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        coverTextChild.transform.SetParent(coverChild.transform, false);

        StretchToFill((RectTransform)imageChild.transform);
        StretchToFill((RectTransform)videoChild.transform);
        StretchToFill((RectTransform)coverChild.transform);
        StretchToFill((RectTransform)coverTextChild.transform);

        CardView cardView = root.GetComponent<CardView>();
        cardView.CacheReferences();
        cardView.ConfigureVisuals(fontAsset);

        return cardView;
    }

    public void SetCard(CardSO card)
    {
        CacheReferences();

        CardData = card;
        IsMatched = false;
        IsRevealed = false;
        _isVideoPrepared = false;
        _isVideoPlayable = false;
        _hasVideoSource = false;
        _hasRequestedPrepare = false;

        gameObject.name = card != null ? card.cardName : "Card";
        _backgroundImage.color = HiddenCardColor;
        _cardImage.enabled = false;
        _cardImage.sprite = null;
        _videoImage.enabled = false;
        _videoImage.texture = null;
        _videoPlayer.Stop();
        _videoPlayer.clip = null;
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = string.Empty;
        _button.interactable = card != null;

        ReleaseRuntimeSprite();

        if (card == null)
        {
            return;
        }

        _hasVideoSource = TryConfigureVideo(card);

        Sprite spriteToShow = card.cardImage != null ? card.cardImage : LoadSpriteFromFile(card.cardImageFileName);
        if (spriteToShow != null)
        {
            _cardImage.sprite = spriteToShow;
        }

        HideImmediate();
    }

    public void Reveal()
    {
        if (CardData == null || IsMatched)
        {
            return;
        }

        IsRevealed = true;
        _coverImage.enabled = false;
        _coverLabel.enabled = false;
        _cardImage.enabled = _cardImage.sprite != null;
        _videoImage.enabled = _isVideoPrepared && _videoImage.texture != null;

        EnsureVideoPreparation();

        if (_isVideoPrepared)
        {
            PlayPreparedVideoFromStart();
        }
    }

    public void HideImmediate()
    {
        if (CardData == null)
        {
            return;
        }

        IsRevealed = false;
        _cardImage.enabled = false;
        _videoImage.enabled = false;
        _coverImage.enabled = true;
        _coverLabel.enabled = true;
        StopVideoPlayback();
        _isVideoPrepared = false;
        _isVideoPlayable = false;
        _hasRequestedPrepare = false;
    }

    public void MarkMatched()
    {
        IsMatched = true;
        IsRevealed = true;
        _button.interactable = false;
        _coverImage.enabled = false;
        _coverLabel.enabled = false;
        _backgroundImage.color = MatchedCardColor;

        EnsureVideoPreparation();

        if (_isVideoPrepared)
        {
            PlayPreparedVideoFromStart();
        }
    }

    public void SetInteractable(bool isInteractable)
    {
        if (!IsMatched)
        {
            _button.interactable = isInteractable;
        }
    }

    private void ConfigureVisuals(TMP_FontAsset fontAsset)
    {
        _backgroundImage.raycastTarget = true;
        _backgroundImage.color = HiddenCardColor;

        _button.targetGraphic = _backgroundImage;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);
        ColorBlock colors = _button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.97f, 0.98f, 1f);
        colors.pressedColor = new Color(0.74f, 0.86f, 0.9f, 1f);
        _button.colors = colors;

        _cardImage.preserveAspect = true;
        _cardImage.raycastTarget = false;

        _videoImage.raycastTarget = false;

        _coverImage.color = CoverColor;
        _coverImage.raycastTarget = false;

        _coverLabel.text = "?";
        _coverLabel.alignment = TextAlignmentOptions.Center;
        _coverLabel.fontSize = 56f;
        _coverLabel.color = new Color(0.93f, 0.99f, 0.98f, 1f);
        _coverLabel.raycastTarget = false;
        if (fontAsset != null)
        {
            _coverLabel.font = fontAsset;
        }

        _videoPlayer.playOnAwake = false;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.isLooping = true;
        _videoPlayer.skipOnDrop = true;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        _videoPlayer.renderMode = VideoRenderMode.APIOnly;
        _videoPlayer.sendFrameReadyEvents = true;
        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.frameReady -= OnVideoFrameReady;
        _videoPlayer.frameReady += OnVideoFrameReady;
        _videoPlayer.errorReceived -= OnVideoErrorReceived;
        _videoPlayer.errorReceived += OnVideoErrorReceived;
    }

    private bool TryConfigureVideo(CardSO card)
    {
        if (!string.IsNullOrWhiteSpace(card.cardVideoFileName))
        {
            string videoPath = ResolveStreamingVideoPath(card.cardVideoFileName);
            if (string.IsNullOrWhiteSpace(videoPath))
            {
                return false;
            }

            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = videoPath;
        }
        else if (card.cardVideo != null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogWarning($"WebGL requires URL-based video playback, but '{card.cardName}' has no video file name.", this);
            return false;
#else
            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.clip = card.cardVideo;
#endif
        }
        else
        {
            return false;
        }

        return true;
    }

    private string ResolveStreamingVideoPath(string videoFileName)
    {
        if (string.IsNullOrWhiteSpace(videoFileName))
        {
            return null;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        return $"{Application.streamingAssetsPath}/{StreamingVideosFolderName}/{Uri.EscapeDataString(videoFileName)}";
#else
        string videoPath = Path.Combine(Application.streamingAssetsPath, StreamingVideosFolderName, videoFileName);
        if (!File.Exists(videoPath))
        {
            Debug.LogWarning($"Video file not found for card '{gameObject.name}': {videoPath}", this);
            return null;
        }

        return new Uri(videoPath).AbsoluteUri;
#endif
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        _isVideoPrepared = true;
        SyncVideoTexture(source);

        if (IsRevealed)
        {
            PlayPreparedVideoFromStart();
        }
    }

    private void OnVideoFrameReady(VideoPlayer source, long frameIdx)
    {
        if (_isVideoPlayable)
        {
            return;
        }

        SyncVideoTexture(source);
    }

    private void OnVideoErrorReceived(VideoPlayer source, string message)
    {
        _isVideoPrepared = false;
        _isVideoPlayable = false;
        _hasRequestedPrepare = false;
        _videoImage.enabled = false;
        Debug.LogWarning($"Video playback failed for '{gameObject.name}': {message}", this);
    }

    private void SyncVideoTexture(VideoPlayer source)
    {
        if (source == null || source.texture == null)
        {
            return;
        }

        _videoImage.texture = source.texture;
        _videoImage.enabled = IsRevealed;
        _isVideoPlayable = true;
    }

    private void EnsureVideoPreparation()
    {
        if (!_hasVideoSource || _hasRequestedPrepare || _isVideoPrepared)
        {
            return;
        }

        _hasRequestedPrepare = true;
        _videoPlayer.Prepare();
    }

    private void PlayPreparedVideoFromStart()
    {
        if (_videoPlayer == null || !_isVideoPrepared)
        {
            return;
        }

        if (_videoPlayer.canSetTime)
        {
            _videoPlayer.time = 0d;
        }

        _videoPlayer.Play();
    }

    private void StopVideoPlayback()
    {
        if (_videoPlayer == null)
        {
            return;
        }

        if (_videoPlayer.isPlaying || _videoPlayer.isPrepared)
        {
            _videoPlayer.Stop();
        }
    }

    private void OnClicked()
    {
        Clicked?.Invoke(this);
    }

    private Sprite LoadSpriteFromFile(string imageFileName)
    {
        if (string.IsNullOrWhiteSpace(imageFileName))
        {
            return null;
        }

        string imagePath = Path.Combine(Application.dataPath, ImagesFolderName, imageFileName);
        if (!File.Exists(imagePath))
        {
            Debug.LogWarning($"Image file not found for card '{name}': {imagePath}", this);
            return null;
        }

        byte[] fileBytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(fileBytes))
        {
            Destroy(texture);
            return null;
        }

        texture.name = imageFileName;
        _runtimeSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        _runtimeSprite.name = imageFileName;
        return _runtimeSprite;
    }

    private void CacheReferences()
    {
        if (_backgroundImage == null)
        {
            _backgroundImage = GetComponent<Image>();
        }

        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_cardImage == null)
        {
            _cardImage = transform.Find("CardImage")?.GetComponent<Image>();
        }

        if (_videoImage == null)
        {
            _videoImage = transform.Find("CardVideo")?.GetComponent<RawImage>();
        }

        if (_videoPlayer == null)
        {
            _videoPlayer = transform.Find("CardVideo")?.GetComponent<VideoPlayer>();
        }

        if (_coverImage == null)
        {
            _coverImage = transform.Find("CardCover")?.GetComponent<Image>();
        }

        if (_coverLabel == null)
        {
            _coverLabel = transform.Find("CardCover/CardCoverLabel")?.GetComponent<TextMeshProUGUI>();
        }
    }

    private static void StretchToFill(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.frameReady -= OnVideoFrameReady;
            _videoPlayer.errorReceived -= OnVideoErrorReceived;
        }

        ReleaseRuntimeSprite();
    }

    private void ReleaseRuntimeSprite()
    {
        if (_runtimeSprite == null)
        {
            return;
        }

        Texture2D texture = _runtimeSprite.texture;
        Destroy(_runtimeSprite);
        if (texture != null)
        {
            Destroy(texture);
        }

        _runtimeSprite = null;
    }
}
