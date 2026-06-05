using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "Card", menuName = "Card Game Objects/Card")]
public class CardSO : ScriptableObject
{
    public string cardName;
    public string pairName;
    public Sprite cardImage;
    public string cardImageFileName;
    public VideoClip cardVideo;
    public string cardVideoFileName;
}
    
