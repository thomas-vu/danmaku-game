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

    [SyncVar]
    public string reactionOrder;  // order for other players' reactions to this player's events

    [SyncVar(hook = "OnChangeRole")]
    public string role;

    public string character;

    [SyncVar]
    public int maxHealth;
    
    [SyncVar]
    public int maxHandSize;

    [SyncVar(hook = "OnChangeHealth")]
    public int health;

    public SyncListMDCard hand = new SyncListMDCard();

    #endregion

    void Start()
    {
        Logic.Instance.numberOfPlayers += 1;
        Logic.Instance.numberOfActivePlayers += 1;
        playerNumber = Logic.Instance.numberOfPlayers;
        name = "Player" + playerNumber;
        transform.SetParent(GameObject.Find("Logic/Players").transform);

        visual = Instantiate(Factory.prefabDict["playerVisual"], GameObject.Find("Visual/Players").transform);
        visual.name = "Player" + playerNumber;

        //------------------------------everything below this line is a placeholder-------------------------------------------------

        // TODO: character is hardcoded to reimu atm
        character = "hakureiReimu";
        GameObject visualCharacter = Instantiate(Factory.prefabDict["hakureiReimu"], visual.transform.Find("Character"));
        visualCharacter.name = "hakureiReimu";
        Logic.Instance.visualCharacters.Add(visualCharacter);

        // Setting "Player#" text on the visual
        GameObject.Find("Visual/Players/Player" + playerNumber + "/PlayerUI/PlayerText").GetComponent<Text>().text = "Player" + playerNumber;
    }

    public override void OnStartLocalPlayer()
    {
        localPlayer = this;
    }

    void Update()
    {
        //if (!isLocalPlayer) return;
    }

    #region Commands

    [Command]
    public void CmdShoot(string target, int indexInHand)
    {
        hand.RemoveAt(indexInHand);
        RpcShoot(target, indexInHand);
    }
    
    [Command]
    public void CmdIntendGraze()
    {
        Event ev = Logic.Instance.currentEvent;
        if (ev.reactionOrder.Contains(playerNumber.ToString()))      // players can't react to their own events
        {
            ev.reactions[ev.reactionOrder.IndexOf(playerNumber.ToString())].Add("grazeSpring");
        }
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    public void RpcShoot(string target)
    {
        Event shootEvent = new Event
        (
            "shootSpring",                                                              // eventName
            this,                                                                       // initiator
            GameObject.Find("Logic/Players/Player" + target).GetComponent<Player>(),    // target
            reactionOrder                                                               // reactionOrder
        );

        Logic.Instance.eventActive = true;
        Logic.Instance.currentEvent = shootEvent;

        // visual for updating the hand
        if (isLocalPlayer)
        {
            Transform selfHandVisual = visual.transform.Find("Hand");

            Sequence s = DOTween.Sequence();

            // setting sorting order and shifting cards in hand, also centering the hand
            for (int i = 0; i < selfHandVisual.childCount; i++)
            {
                selfHandVisual.GetChild(i).Find("Canvas").GetComponent<Canvas>().sortingOrder = i;
                s.Insert(0f, selfHandVisual.GetChild(i).DOLocalMove(selfHandVisual.GetComponent<VisualContainer>().positionList[i], 0.5f));
            }

            s.Insert(0f, selfHandVisual.DOMoveX(-selfHandVisual.GetComponent<VisualContainer>().positionList[0].x / 2f, 0.5f));
        }
    }

    #endregion

    public void updateReactionOrder(List<Player> activePlayers)
    {
        string turnOrder = "";
        foreach (Player player in activePlayers) turnOrder += player.playerNumber.ToString();

        int thisPlayersIndex = turnOrder.IndexOf(playerNumber.ToString());
        string thisPlayersTurnOrder = turnOrder.Substring(thisPlayersIndex) + turnOrder.Substring(0, thisPlayersIndex); // rotates the string so this player's playerNumber is at the front
        reactionOrder = thisPlayersTurnOrder.Substring(1);
    }

    public void OnChangeHealth(int health)
    {
        this.health = health;
        visual.transform.Find("PlayerUI/Health/HealthText").GetComponent<Text>().text = health.ToString();
    }

    public void OnChangeRole(string role)
    {
        this.role = role;
        visual.transform.Find("PlayerUI/RoleText").GetComponent<Text>().text = role;
    }
}