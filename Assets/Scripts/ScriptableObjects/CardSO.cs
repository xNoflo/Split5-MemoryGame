using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Card Game Objects/Card")]
public class CardSO : ScriptableObject
{
    public string cardName;
    public string pairName;
    public Sprite cardImage;
}
    
