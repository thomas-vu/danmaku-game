using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class Player : NetworkBehaviour
{
    #region Variables

    public static Player localPlayer;
    public GameObject visual;

    public int playerNumber;

    [SyncVar(hook = "OnChangeRole")]
    public string role;
    public string character;

    [SyncVar]
    public SyncListMDCard hand = new SyncListMDCard();

    public int maxHealth;
    public int maxHandSize;

    [SyncVar(hook = "OnChangeHealth")]
    public int health;

    //public GameObject powerup;
    //public GameObject defense;
    //public GameObject artifact;

    #endregion

    void Start()
    {
        Logic.Instance.numberOfPlayers += 1;
        playerNumber = Logic.Instance.numberOfPlayers;
        name = "Player" + playerNumber;
        transform.SetParent(GameObject.Find("Logic/Players").transform);

        visual = Instantiate(GameObject.Find("MyPrefabs").GetComponent<MyPrefabs>().playerVisual);
        visual.name = "Player" + playerNumber;
        visual.transform.SetParent(GameObject.Find("Visual/Players").transform);
        visual.GetComponent<PlayerVisual>().playerInLogic = gameObject;

        //------------------------------everything below this line is a placeholder-------------------------------------------------

        // TODO character is hardcoded to reimu atm
        character = "HakureiReimu";
        GameObject visualCharacter = Instantiate(GameObject.Find("MyPrefabs").GetComponent<MyPrefabs>().hakureiReimu);
        visualCharacter.name = "HakureiReimu";
        visualCharacter.transform.SetParent(visual.transform.GetChild(0));

        // Setting "Player#" text on the visual
        GameObject.Find("Visual/Players/Player" + playerNumber + "/PlayerUI/PlayerText").GetComponent<Text>().text = "Player" + playerNumber;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Z)) CmdSuicide();
        if (Input.GetKeyDown(KeyCode.Space)) CmdDrawACard();
    }

    // Local Player Only
    public void DragReleased(GameObject releasedObject)
    {
        if (releasedObject.GetComponent<CardIdentity>().identity == "Shoot")
        {
            GameObject target = DetectPlayerHit(false);
            if (target != null) CmdShoot(target);
        }

        if (releasedObject.GetComponent<CardIdentity>().identity == "Graze") if (DetectPlayAreaHit()) CmdGraze();
    }

    public GameObject DetectPlayerHit(bool canTargetSelf)
    {
        for (int i = 1; i < Logic.Instance.numberOfPlayers + 1; i++)
        {
            // skips checking own collider for cards that cannot target yourself (like Shoot)
            if (!canTargetSelf && playerNumber == i) continue;

            BoxCollider bc = GameObject.Find("Visual/Players/Player" + i + "/Character").transform.GetChild(0).GetComponent<BoxCollider>();

            // raycast to mousePosition and store all the hits in the array
            RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 30f);

            foreach (RaycastHit h in hits)
            {
                // check if the collider that we hit is the collider on this GameObject
                if (h.collider == bc) return bc.gameObject.transform.parent.parent.GetComponent<PlayerVisual>().playerInLogic;
            }
        }

        return null;
    }

    public bool DetectPlayAreaHit()
    {
        // raycast to mousePosition and store all the hits in the array
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 30f);

        foreach (RaycastHit h in hits)
        {
            // check if the collider that we hit is the collider on this GameObject
            if (h.collider == GameObject.Find("Visual/PlayArea").GetComponent<BoxCollider>()) return true;
        }

        return false;
    }

    #region Commands

    [Command]
    void CmdGraze()
    {
        Logic.Instance.eventActive = false;
        Logic.Instance.eventInitiator = null;
        Logic.Instance.eventTarget = null;
        Logic.Instance.eventTimeLeft = 10.0f;
        Logic.Instance.RpcShowMessage(true, "", 0); // clears the text and timer messages
    }

    [Command]
    void CmdShoot(GameObject target)
    {
        Logic.Instance.eventActive = true;
        Logic.Instance.eventInitiator = this;
        Logic.Instance.eventTarget = target.GetComponent<Player>();
    }

    [Command]
    void CmdSuicide()
    {
        health -= 1;
    }

    [Command]
    public void CmdDrawACard()
    {
        // LOGIC
        MDCard drawnCard = GameObject.Find("Logic/MainDeck").GetComponent<MainDeck>().mainDeck[0];
        GameObject.Find("Logic/MainDeck").GetComponent<MainDeck>().mainDeck.RemoveAt(0);
        hand.Add(drawnCard);
        // LOGIC

        // VISUAL
        RpcDrawACard(drawnCard.name);
        RpcShowOtherDraw(drawnCard.name);
        // VISUAL
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    void RpcDrawACard(string cardName)
    {
        if (!isLocalPlayer) return;

        GameObject handVisual = visual.transform.GetChild(2).gameObject;

        int numberOfCardsInHand = 0; // this is the number of cards in hand BEFORE the next card is drawn
        foreach (GameObject g in handVisual.GetComponent<SameDistanceChildren>().Children) if (g.transform.childCount == 1) numberOfCardsInHand += 1; // calculating this number

        // spawning the card on top of the deck, disabling its preview and dragging
        GameObject drawnCard = Instantiate(MyPrefabs.prefabDict[cardName], GameObject.Find("Visual/MainDeck").transform.position, Quaternion.Euler(new Vector3(0f, -179f, 0f)));
        drawnCard.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
        drawnCard.GetComponent<Draggable>().canDrag = false;

        // while the card is travelling, make it appear above everything
        drawnCard.transform.GetChild(0).GetComponent<Canvas>().sortingOrder = 500;
        drawnCard.transform.GetChild(0).GetComponent<Canvas>().sortingLayerName = "AboveEverything";

        // parenting each card to the next slot to make room for the new card
        for (int i = numberOfCardsInHand; i > 0; i--) handVisual.GetComponent<SameDistanceChildren>().Children[i-1].transform.GetChild(0).SetParent(handVisual.transform.GetChild(i));
        // parent the drawn card to slot 0 in the hand
        drawnCard.transform.SetParent(handVisual.transform.GetChild(0));

        // tween sequence for drawing the card
        Sequence s = DOTween.Sequence();
        s.Append(drawnCard.transform.DOMove(GameObject.Find("Visual/DrawPreviewArea").transform.position, 0.75f));
        s.Insert(0f, drawnCard.transform.DORotate(Vector3.zero, 0.75f));
        s.AppendInterval(0.75f);
        s.Append(drawnCard.transform.DOLocalMove(Vector3.zero, 0.75f));

        // shifting other cards in hand and centering the hand
        foreach (GameObject g in handVisual.GetComponent<SameDistanceChildren>().Children)
        {
            if (g.transform.childCount == 1) s.Insert(1.5f, g.transform.GetChild(0).DOLocalMove(Vector3.zero, 0.3f));
        }
        s.Insert(1.5f, handVisual.transform.DOMoveX((handVisual.transform.GetChild(0).position.x - handVisual.transform.GetChild(numberOfCardsInHand).position.x) / 2f, 0.3f));

        // update hand size visual, set sorting order of the cards in hand, re-enable previews and dragging
        s.OnComplete(() =>
        {
            GameObject.Find("Visual/Players/Player" + playerNumber + "/PlayerUI/HandSize/HandSizeText").GetComponent<Text>().text = hand.Count.ToString();
            for (int i = 0; i < numberOfCardsInHand + 1; i++) handVisual.GetComponent<SameDistanceChildren>().Children[i].transform.GetChild(0).GetChild(0).GetComponent<Canvas>().sortingOrder = -i;
            drawnCard.GetComponent<HoverPreview>().ThisPreviewEnabled = true;
            drawnCard.GetComponent<Draggable>().canDrag = true;
        });
    }

    [ClientRpc]
    public void RpcShowOtherDraw(string cardName)
    {
        if (isLocalPlayer) return;

        // spawning the card on top of the deck, disabling its preview and dragging
        GameObject drawnCard = Instantiate(MyPrefabs.prefabDict[cardName], GameObject.Find("Visual/MainDeck").transform.position, Quaternion.Euler(new Vector3(0f, -179f, 0f)));
        drawnCard.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
        drawnCard.GetComponent<Draggable>().canDrag = false;

        // while the card is travelling, make it appear above everything
        drawnCard.transform.GetChild(0).GetComponent<Canvas>().sortingOrder = 500;
        drawnCard.transform.GetChild(0).GetComponent<Canvas>().sortingLayerName = "AboveEverything";

        // tween sequence for drawing the card
        Sequence s = DOTween.Sequence();
        s.Append(drawnCard.transform.DOMove(GameObject.Find("Visual/DrawPreviewArea").transform.position, 0.75f));
        s.AppendInterval(0.75f);
        s.Append(drawnCard.transform.DOLocalMove(visual.transform.GetChild(1).GetChild(4).position, 0.75f));
        s.Insert(1.5f, drawnCard.transform.DOScale(Vector3.zero, 0.75f));
        s.OnComplete(() =>
        {
            GameObject.Find("Visual/Players/Player" + playerNumber + "/PlayerUI/HandSize/HandSizeText").GetComponent<Text>().text = hand.Count.ToString();
            Destroy(drawnCard);
        });
    }

    //[ClientRpc]
    //public void RpcShowMessage(bool clear, string message, float timer)
    //{
    //    if (clear)
    //    {
    //        GameObject.Find("Visual/Event/Text").GetComponent<Text>().text = "";
    //        GameObject.Find("Visual/Event/Timer").GetComponent<Text>().text = "";
    //    }
    //    else
    //    {
    //        GameObject.Find("Visual/Event/Text").GetComponent<Text>().text = message;
    //        GameObject.Find("Visual/Event/Timer").GetComponent<Text>().text = Mathf.CeilToInt(timer).ToString();
    //    }
    //}

    #endregion

    void OnChangeHealth(int health)
    {
        GameObject.Find("Visual/Players/Player" + playerNumber + "/PlayerUI/Health/HealthText").GetComponent<Text>().text = health.ToString();
    }

    void OnChangeRole(string role)
    {
        GameObject.Find("Visual/Players/Player" + playerNumber + "/PlayerUI/RoleText").GetComponent<Text>().text = role;
    }

    public override void OnStartLocalPlayer()
    {
        localPlayer = this;
    }
}