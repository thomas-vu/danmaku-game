using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class MainDeckDiscardPile : NetworkBehaviour
{
    // SINGLETON
    public static MainDeckDiscardPile Instance { private set; get; }

    public SyncListMDCard mainDeckDiscardPile = new SyncListMDCard();
    public List<Sprite> sprites = new List<Sprite>();
    public Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

    void Start()
    {
        Instance = this;
        mainDeckDiscardPile.Callback = OnDeckListChanged;

        spriteDict.Add("shootSpring", sprites[0]);
        spriteDict.Add("grazeSpring", sprites[1]);

        if (!isServer) return;
    }

    private void OnDeckListChanged(SyncListStruct<MDCard>.Operation op, int index)
    {
        GameObject.Find("Visual/MainDeckDiscardPile/Canvas/Card").GetComponent<Image>().sprite = spriteDict[mainDeckDiscardPile[0].name];
        GameObject.Find("Visual/MainDeckDiscardPile").GetComponent<DeckThickness>().updateThickness(mainDeckDiscardPile.Count);
        //Debug.Log("list changed " + op);
    }
}