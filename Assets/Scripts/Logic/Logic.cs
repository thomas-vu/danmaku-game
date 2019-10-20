using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Logic : NetworkBehaviour
{
    #region Variables

    // SINGLETON
    public static Logic Instance { private set; get; }

    public SpriteRenderer BlackScreen;

    public List<GameObject> visualCharacters = new List<GameObject>();

    public bool gameStarted = false;
    public int numberOfPlayers = 0;
    public int numberOfActivePlayers = 0;

    public Player currentPlayer;

    public bool mainStepActive;
    public float mainStepTime = 10f;        // TODO: should possibly go in a settings script

    public List<Event> eventStack = new List<Event>();
    public Event currentEvent;
    public bool eventActive;

    List<Vector3> spawnPositions4 = new List<Vector3>
    {
        new Vector3(0, -2f, -1f),           // bottom spawn (6 o'clock), going clockwise
        new Vector3(-5.5f, 0.5f, -1f),
        new Vector3(0, 3.5f, -1f),
        new Vector3(5.5f, 0.5f, -1f)
    };

    List<string> partnerList = new List<string>
    {
        "Partner",          // 5 player
        "Partner",          // 5 player
        "EXMidboss",        // 5 player
        "OneTruePartner"    // 7 player
    };

    List<string> stageBossList = new List<string>
    {
        "StageBoss",        // 4 player
        "StageBoss",        // 4 player
        "StageBoss",        // 4 player
        "AntiHeroine",      // 5 player
        "Challenger",       // 5 player
        "FinalBoss"         // 5 player
    };

    List<string> exBossList = new List<string>
    {
        "EXBoss",           // 4 player
        "PhantomBoss"       // 4 player
    };

    #endregion

    // SERVER ONLY: sets up turn order, assigns roles, maxHealth, health, and maxHandSize for all players
    void SetupGame()
    {
        // randomizing turn order
        List<int> turnOrderList = new List<int>(Enumerable.Range(1, numberOfPlayers)); // [1,2,3,4] for 4 players
        turnOrderList.Shuffle();

        // constructing the roleList
        List<string> roleList = new List<string>();

        if (numberOfPlayers == 4)
        {
            exBossList.Shuffle();

            roleList.Add("Heroine");
            roleList.Add("StageBoss");
            roleList.Add("StageBoss");
            roleList.Add(exBossList[0]);
        }

        if (numberOfPlayers == 5)
        {
            partnerList.Take(3).ToList().Shuffle();
            stageBossList.Shuffle();
            exBossList.Shuffle();

            roleList.Add("Heroine");
            roleList.Add(partnerList[0]);
            roleList.Add(stageBossList[0]);
            roleList.Add(stageBossList[1]);
            roleList.Add(exBossList[0]);
        }

        // assigning roles
        roleList.Shuffle();
        for (int i = 0; i < numberOfPlayers; i++) GameObject.Find("Logic/Players/Player" + turnOrderList[i]).GetComponent<Player>().role = roleList[i];

        // rotating the list so the heroine's playerNumber is at the front
        int heroineIndex = roleList.IndexOf("Heroine");
        turnOrderList = turnOrderList.Skip(heroineIndex).Concat(turnOrderList.Take(heroineIndex)).ToList();

        // serializing turnOrderList
        string turnOrder = "";
        foreach (int i in turnOrderList) turnOrder += i;

        List<Player> activePlayers = new List<Player>();
        foreach (int i in turnOrderList) activePlayers.Add(GameObject.Find("Logic/Players/Player" + i).GetComponent<Player>());

        // assigning reaction order for each player's events
        foreach (Player player in activePlayers) player.updateReactionOrder(activePlayers);

        // assigning health and hand size
        foreach (Player player in activePlayers)
        {
            if (player.role == "Heroine")
            {
                player.maxHealth = 5;
                player.maxHandSize = 5;
            }
            else
            {
                player.maxHealth = 4;
                player.maxHandSize = 4;
            }

            player.health = player.maxHealth;
        }
        
        RpcSetupGame(turnOrder);  // further visual set-up on all clients
    }
    
    [ClientRpc]
    void RpcSetupGame(string turnOrder)
    {
        // arranging players around the table using the turnOrder string
        int localPlayerIndex = turnOrder.IndexOf(Player.localPlayer.playerNumber.ToString());
        string localTurnOrder = turnOrder.Substring(localPlayerIndex) + turnOrder.Substring(0, localPlayerIndex); // rotates the string so the local player's playerNumber is at the front

        for (int i = 0; i < numberOfPlayers; i++)
        {
            if (numberOfPlayers == 4) GameObject.Find("Visual/Players/Player" + localTurnOrder[i]).transform.position = spawnPositions4[i]; // spawns from 6 o'clock, going clockwise
            //if (numberOfPlayers == 5) GameObject.Find("Visual/Players/Player" + localTurnOrder[i]).transform.position = spawnPositions5[i];
        }

        // reconstructing the activePlayers list
        List<Player> activePlayers = new List<Player>();
        foreach (char i in turnOrder) activePlayers.Add(GameObject.Find("Logic/Players/Player" + i).GetComponent<Player>());

        StartCoroutine(GameLoop(activePlayers));  // start the game loop
    }

    IEnumerator GameLoop(List<Player> activePlayers)
    {
        while (activePlayers[0].maxHandSize == 0) yield return null;  // waiting for SyncVars to update from the server >.< TODO: not ideal, host ends up being milliseconds ahead of other clients, not sure what impact this will have, maybe it's fine

        // dealing everyone their starting hand
        foreach (Player player in activePlayers)
            yield return StartCoroutine(MainDeck.Instance.Draw(player, player.maxHandSize));

        currentPlayer = activePlayers[0];  // activePlayers[0] is the heroine, they have the first turn
        while (true)
        {
            yield return StartCoroutine(PlayerTurn(currentPlayer));  // begin next player's turn

            // get next currentPlayer
            int newIndex = activePlayers.IndexOf(currentPlayer) + 1;
            if (newIndex == numberOfPlayers) currentPlayer = activePlayers[0];
            else currentPlayer = activePlayers[newIndex];
        }
    }

    IEnumerator PlayerTurn(Player player)
    {
        player.visual.transform.Find("PlayerUI/TurnIndicator").GetComponent<Image>().color = Color.green;  // turning turn indicator green

        // Start of Turn
        // Incident Step
        yield return StartCoroutine(DrawStep(player));
        yield return StartCoroutine(MainStep(player));
        // Discard Step

        player.visual.transform.Find("PlayerUI/TurnIndicator").GetComponent<Image>().color = Color.white;  // turning turn indicator white
    }
    
    IEnumerator DrawStep(Player player)
    {
        yield return StartCoroutine(MainDeck.Instance.Draw(player, 2));  // TODO: change 2 to player's draw amount
    }

    IEnumerator MainStep(Player player)
    {
        //UpdateHandGlows(true, player);

        float mainStepTimeLeft = mainStepTime;
        mainStepActive = true;
        while (mainStepTimeLeft > 0f)
        {
            if (!eventActive)
            {
                mainStepTimeLeft -= Time.deltaTime;
                GameObject.Find("Visual/TurnTimer/Timer").GetComponent<Text>().text = Mathf.CeilToInt(mainStepTimeLeft).ToString();
            }
            yield return null;
        }
        mainStepActive = false;
        GameObject.Find("Visual/TurnTimer/Timer").GetComponent<Text>().text = "";  // clears the timer on screen

        //UpdateHandGlows(false, player);
    }
    
    public void UpdateHandGlows(bool flag, Player player)
    {
        if (Player.localPlayer != player) return;
        
        for (int i = 0; i < player.hand.Count; i++)
        {
            Transform card = player.visual.transform.Find("Hand").GetChild(i);
            if (card.GetComponent<CardInfo>().cardName == "shootSpring")
            {
                card.Find("Canvas/Glow").gameObject.SetActive(flag);
                card.Find("Preview/Canvas/Glow").gameObject.SetActive(flag);
            }
        }
    }

    public IEnumerator BeginIntentPhase(float eventTimeLeft)
    {
        // intent phase countdown
        while (eventTimeLeft > 0f)
        {
            eventTimeLeft -= Time.deltaTime;
            GameObject.Find("Visual/EventVisual/Canvas/Timer").GetComponent<Text>().text = Mathf.CeilToInt(eventTimeLeft).ToString();

            yield return null;
        }
        GameObject.Find("Visual/EventVisual/Canvas/Timer").GetComponent<Text>().text = "";  // clears the timer on screen

        currentEvent.Resolve();

        // TODO: uhhhh this is wrong
        if (eventStack.Count != 0)
        {
            currentEvent = eventStack[0];
            eventStack.RemoveAt(0);
        }
        else
        {
            eventActive = false;
            currentEvent = null;
        }
    }

    public IEnumerator StartEvent(string cardName, int indexInHand, string initiator, string eventName, string target)
    {
        GameObject card;

        if (Player.localPlayer.playerNumber == int.Parse(initiator))
            card = GameObject.Find("Visual/Players/Player" + initiator + "/Hand").transform.GetChild(indexInHand).gameObject;
        else
            card = Instantiate(Factory.prefabDict["shootSpring"], GameObject.Find("Visual/Players/Player" + initiator).transform.position, Quaternion.identity);

        card.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
        card.GetComponent<Draggable>().canDrag = false;

        yield return StartCoroutine(card.GetComponent<Draggable>().PlayAnimation("SuccessfullyPlayed"));
        yield return StartCoroutine(ShowEvent(initiator, eventName, target));

        StartCoroutine(BeginIntentPhase(15f));  // TODO: put timer in a settings script
    }

    IEnumerator ShowEvent(string initiator, string eventName, string target)
    {
        // dim the screen
        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;
            BlackScreen.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 0.75f, elapsedTime));
            yield return null;
        }

        // show initiator, event, and target
        GameObject init = Instantiate(
            Factory.prefabDict[GameObject.Find("Logic/Players/Player" + initiator).GetComponent<Player>().character],
            GameObject.Find("Visual/EventVisual/Initiator").transform);

        GameObject ev = Instantiate(
            Factory.prefabDict[eventName],
            GameObject.Find("Visual/EventVisual/Event").transform);

        GameObject targ = Instantiate(
            Factory.prefabDict[GameObject.Find("Logic/Players/Player" + target).GetComponent<Player>().character],
            GameObject.Find("Visual/EventVisual/Target").transform);

        init.transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "DontDimThisLayer";
        ev.transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "DontDimThisLayer";
        targ.transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "DontDimThisLayer";

        init.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
        ev.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
        ev.GetComponent<Draggable>().canDrag = false;
        targ.GetComponent<HoverPreview>().ThisPreviewEnabled = false;

        foreach (GameObject character in visualCharacters)
            character.GetComponent<HoverPreview>().ThisPreviewEnabled = false;
    }

    void Update()
    {
        if (isServer && Input.GetKeyDown(KeyCode.Q) && !gameStarted && numberOfPlayers >= 4 && numberOfPlayers <= 8)
        {
            gameStarted = true;
            SetupGame();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void Start()
    {
        Instance = this;
    }
}