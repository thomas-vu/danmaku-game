using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class MainDeck : NetworkBehaviour
{
    // SINGLETON
    public static MainDeck Instance { private set; get; }

    public SyncListMDCard mainDeck = new SyncListMDCard();
    public int animationsCompleted = 0;

    void Start()
    {
        Instance = this;
        mainDeck.Callback = OnDeckListChanged;

        if (!isServer) return;

        mainDeck.Add(Factory.shootSpringMD); // 1
        mainDeck.Add(Factory.shootSpringMD); // 2
        mainDeck.Add(Factory.shootSpringMD); // 3
        mainDeck.Add(Factory.shootSpringMD); // 4
        mainDeck.Add(Factory.shootSpringMD); // 5
        mainDeck.Add(Factory.shootSpringMD); // 6
        mainDeck.Add(Factory.shootSpringMD); // 7
        mainDeck.Add(Factory.shootSpringMD); // 8
        mainDeck.Add(Factory.shootSpringMD); // 9
        mainDeck.Add(Factory.shootSpringMD); // 10
        mainDeck.Add(Factory.shootSpringMD); // 11
        mainDeck.Add(Factory.shootSpringMD); // 12
        mainDeck.Add(Factory.shootSpringMD); // 13
        mainDeck.Add(Factory.shootSpringMD); // 14
        mainDeck.Add(Factory.shootSpringMD); // 15
        mainDeck.Add(Factory.shootSpringMD); // 16

        mainDeck.Add(Factory.grazeSpringMD); // 1
        mainDeck.Add(Factory.grazeSpringMD); // 2
        mainDeck.Add(Factory.grazeSpringMD); // 3
        mainDeck.Add(Factory.grazeSpringMD); // 4
        mainDeck.Add(Factory.grazeSpringMD); // 5
        mainDeck.Add(Factory.grazeSpringMD); // 6
        mainDeck.Add(Factory.grazeSpringMD); // 7
        mainDeck.Add(Factory.grazeSpringMD); // 8
        mainDeck.Add(Factory.grazeSpringMD); // 9
        mainDeck.Add(Factory.grazeSpringMD); // 10
        mainDeck.Add(Factory.grazeSpringMD); // 11
        mainDeck.Add(Factory.grazeSpringMD); // 12
        mainDeck.Add(Factory.grazeSpringMD); // 13
        mainDeck.Add(Factory.grazeSpringMD); // 14
        mainDeck.Add(Factory.grazeSpringMD); // 15
        mainDeck.Add(Factory.grazeSpringMD); // 16

        mainDeck.Shuffle();
    }

    public IEnumerator Draw(Player player, int numberOfCards)
    {
        if (isServer)
        {
            for (int i = 0; i < numberOfCards; i++)
            {
                player.hand.Add(mainDeck[0]);
                RpcShowDraw(player.playerNumber, mainDeck[0].name);
                mainDeck.RemoveAt(0);
                yield return new WaitForSeconds(0.75f);
            }
        }

        while (animationsCompleted < numberOfCards) yield return null;

        animationsCompleted = 0;
    }

    [ClientRpc]
    void RpcShowDraw(int drawingPlayer, string cardName)
    {
        // spawning the card on top of the deck
        GameObject drawnCard = Instantiate(
            Factory.prefabDict[cardName],                            // original
            GameObject.Find("Visual/MainDeck").transform.position,   // position
            Quaternion.Euler(new Vector3(0f, -179f, 0f)));           // rotation

        // disable previewing, dragging, and change sorting layer
        drawnCard.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
        drawnCard.GetComponent<Draggable>().canDrag = false;
        drawnCard.transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "DontDimThisLayer";

        // tween sequence for drawing the card
        Sequence s = DOTween.Sequence();
        s.Append(drawnCard.transform.DOMove(GameObject.Find("Visual/DrawPreviewArea").transform.position, 0.75f));
        s.AppendInterval(0.75f);

        Transform selfHandVisual = GameObject.Find("Visual/Players/Player" + drawingPlayer + "/Hand").transform;
        Transform otherHandVisual = GameObject.Find("Visual/Players/Player" + drawingPlayer + "/PlayerUI/HandSize").transform;
        Text handSizeText = GameObject.Find("Visual/Players/Player" + drawingPlayer + "/PlayerUI/HandSize/HandSizeText").GetComponent<Text>();

        if (Player.localPlayer.playerNumber == drawingPlayer)  // animation for drawing a card for this player
        {
            // handles parenting of card, sorting order in hand, and calculates hand positions
            selfHandVisual.GetComponent<VisualContainer>().Add(drawnCard.transform);

            s.Insert(0f, drawnCard.transform.DORotate(Vector3.zero, 0.75f));

            // shifting cards in hand and centering the hand
            for (int i = 0; i < selfHandVisual.childCount; i++)
                s.Insert(1.5f, selfHandVisual.GetChild(i).DOLocalMove(selfHandVisual.GetComponent<VisualContainer>().positionList[i], 0.5f));

            s.Insert(1.5f, selfHandVisual.DOMoveX(-selfHandVisual.GetComponent<VisualContainer>().positionList[0].x / 2f, 0.5f));

            // update hand size visual, re-enable previews and dragging
            s.OnComplete(() =>
            {
                handSizeText.text = (int.Parse(handSizeText.text) + 1).ToString();

                drawnCard.GetComponent<HoverPreview>().ThisPreviewEnabled = true;
                drawnCard.GetComponent<Draggable>().canDrag = true;
                drawnCard.transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "DontDimThisLayer";

                // setting sorting order of cards in hand
                for (int i = 0; i < selfHandVisual.childCount; i++)
                    selfHandVisual.GetChild(i).Find("Canvas").GetComponent<Canvas>().sortingOrder = i;

                animationsCompleted += 1;
            });
        }
        else  // animation for another player drawing a card
        {
            s.Append(drawnCard.transform.DOMove(otherHandVisual.position, 0.75f));
            s.Insert(1.5f, drawnCard.transform.DOScale(Vector3.zero, 0.75f));
            s.OnComplete(() =>
            {
                handSizeText.text = (int.Parse(handSizeText.text) + 1).ToString();

                Destroy(drawnCard);

                animationsCompleted += 1;
            });
        }
    }

    private void OnDeckListChanged(SyncListStruct<MDCard>.Operation op, int index)
    {
        GameObject.Find("Visual/MainDeck").GetComponent<DeckThickness>().updateThickness(mainDeck.Count);
        //Debug.Log("list changed " + op);
    }
}