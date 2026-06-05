using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CardView : MonoBehaviour
{
    private const string ImagesFolderName = "Images";

    private Image _backgroundImage;
    private Button _button;
    private Image _cardImage;
    private RawImage _videoImage;
    private VideoPlayer _videoPlayer;
    private Image _coverImage;
    private TextMeshProUGUI _coverLabel;

    private Sprite _runtimeSprite;
    private bool _isVideoPrepared;

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

        gameObject.name = card != null ? card.cardName : "Card";
        _backgroundImage.color = new Color(0.09f, 0.11f, 0.18f, 1f);
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

        TryConfigureVideo(card);

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

        if (_isVideoPrepared)
        {
            _videoPlayer.Play();
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
        _videoPlayer.Pause();
    }

    public void MarkMatched()
    {
        IsMatched = true;
        IsRevealed = true;
        _button.interactable = false;
        _coverImage.enabled = false;
        _coverLabel.enabled = false;
        _backgroundImage.color = new Color(0.14f, 0.32f, 0.2f, 1f);

        if (_isVideoPrepared)
        {
            _videoPlayer.Play();
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
        _backgroundImage.color = new Color(0.09f, 0.11f, 0.18f, 1f);

        _button.targetGraphic = _backgroundImage;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);
        ColorBlock colors = _button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        _button.colors = colors;

        _cardImage.preserveAspect = true;
        _cardImage.raycastTarget = false;

        _videoImage.raycastTarget = false;

        _coverImage.color = new Color(0.33f, 0.19f, 0.56f, 1f);
        _coverImage.raycastTarget = false;

        _coverLabel.text = "?";
        _coverLabel.alignment = TextAlignmentOptions.Center;
        _coverLabel.fontSize = 56f;
        _coverLabel.color = Color.white;
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
        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        _videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private bool TryConfigureVideo(CardSO card)
    {
        if (card.cardVideo != null)
        {
            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.clip = card.cardVideo;
        }
        else if (!string.IsNullOrWhiteSpace(card.cardVideoFileName))
        {
            string videoPath = Path.Combine(Application.dataPath, ImagesFolderName, card.cardVideoFileName);
            if (!File.Exists(videoPath))
            {
                Debug.LogWarning($"Video file not found for card '{card.cardName}': {videoPath}", this);
                return false;
            }

            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = new Uri(videoPath).AbsoluteUri;
        }
        else
        {
            return false;
        }

        _videoPlayer.Prepare();
        return true;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        _isVideoPrepared = true;
        _videoImage.texture = source.texture;
        _videoImage.enabled = IsRevealed && source.texture != null;

        if (IsRevealed)
        {
            source.Play();
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
