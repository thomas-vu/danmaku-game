using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class MainDeck : NetworkBehaviour
{
    public SyncListMDCard mainDeck = new SyncListMDCard();

    void Start()
    {
        mainDeck.Callback = OnDeckListChanged;

        if (!isServer) return;

        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 1
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 2
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 3
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 4
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 5
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 6
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 7
        mainDeck.Add(MyPrefabs.mdCardDict["shootSpring"]); // 8

        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 1
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 2
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 3
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 4
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 5
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 6
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 7
        mainDeck.Add(MyPrefabs.mdCardDict["grazeSpring"]); // 8

        mainDeck.Shuffle();
    }

    private void OnDeckListChanged(SyncListStruct<MDCard>.Operation op, int index)
    {
        GameObject.Find("Visual/MainDeck").GetComponent<DeckThickness>().updateThickness(mainDeck.Count);
        //Debug.Log("list changed " + op);
    }
}