using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Logic : NetworkBehaviour
{
    #region Variables

    // SINGLETON
    public static Logic Instance;

    public bool gameStarted = false;
    public int numberOfPlayers = 0;
    public int numberOfPlayersToStart = 4;
    public string turnOrderString;

    public int whoseTurn;

    public bool eventActive;
    public GameObject eventInitiator;
    public GameObject eventTarget;
    public float eventTimeLeft;

    public List<Vector3> Spawn4 = new List<Vector3>();

    #endregion

    public void ShootEvent(GameObject player, GameObject target)
    {
        eventActive = true;
        eventInitiator = player;
        eventTarget = target;
    }

    [ClientRpc]
    void RpcArrangePlayers(string turnOrderString)
    {
        // converting turnOrderString back to a List so we can use List methods
        List<int> turnOrder = new List<int>();
        foreach (char ch in turnOrderString) turnOrder.Add(int.Parse(ch.ToString()));
        // foreach (var i in turnOrder) print("uh" + i);

        int localPlayerIndex = turnOrder.IndexOf(Player.localPlayer.playerNumber);

        List<int> localTurnOrder = new List<int>(turnOrder.Skip(localPlayerIndex).Concat(turnOrder.Take(localPlayerIndex))); // rotates the list so the local player's number is at the front
        // foreach (var i in localTurnOrder) print("eh" + i);

        for (int i = 0; i < numberOfPlayers; i++) GameObject.Find("Visual/Players/Player" + localTurnOrder[i]).transform.position = Spawn4[i]; // spawns from 6 o'clock, going clockwise
        GameObject.Find("Logic/Players/Player1").GetComponent<Player>().health = 1;
        GameObject.Find("Logic/Players/Player2").GetComponent<Player>().health = 2;
        GameObject.Find("Logic/Players/Player3").GetComponent<Player>().health = 3;
        GameObject.Find("Logic/Players/Player4").GetComponent<Player>().health = 4;
    }

    void Update()
    {
        if (isServer)
        {
            if (!gameStarted && numberOfPlayers == numberOfPlayersToStart)
            {
                gameStarted = true;

                List<int> turnOrder = new List<int>(Enumerable.Range(1, numberOfPlayers)); // [1,2,3,4] for 4 players
                turnOrder.Shuffle();

                // ClientRpc cannot take Lists as arguments, so we convert it to a string
                turnOrderString = "";
                foreach (int i in turnOrder) turnOrderString += i;
            }

            if (Input.GetKeyDown(KeyCode.Q)) RpcArrangePlayers(turnOrderString);
        }

        if (eventActive)
        {
            eventTimeLeft -= Time.deltaTime;
            string message = "Player" + eventInitiator.GetComponent<Player>().playerNumber + " shoots Player" + eventTarget.GetComponent<Player>().playerNumber;
            eventInitiator.GetComponent<Player>().RpcShowMessage(false, message, eventTimeLeft);
            if (eventTimeLeft < 0)
            {
                eventInitiator.GetComponent<Player>().RpcShowMessage(true, "", 0);
                eventTarget.GetComponent<Player>().health -= 1;

                eventActive = false;
                eventInitiator = null;
                eventTarget = null;
                eventTimeLeft = 10.0f;
            }
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Spawn4.Add(new Vector3(0, -2.25f, -1f)); // bottom spawn (6 o'clock), going clockwise
        Spawn4.Add(new Vector3(-5f, 0.5f, -1f));
        Spawn4.Add(new Vector3(0, 3.5f, -1f));
        Spawn4.Add(new Vector3(5f, 0.5f, -1f));

        eventActive = false;
        eventTimeLeft = 10.0f;
    }
}