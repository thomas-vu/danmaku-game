using UnityEngine;
using System.Collections;
using DG.Tweening;

public class DeckThickness : MonoBehaviour
{
    public float heightOfOneCard;

    public void updateThickness(int numberOfCardsInDeck)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, -heightOfOneCard * numberOfCardsInDeck);
    }
}