using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(fileName = "CardCollection", menuName = "Card Game Objects/Card Collection")]
public class CardCollection : ScriptableObject
{
    public List<CardSO> cards;

}
