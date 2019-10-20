using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// this struct contains the logic of main deck cards
public struct MDCard
{
    public string name;
    public string season;   // spring, summer, autumn, winter
    public bool splitCard;
    public int pointValue;
    public string expansion;    // base, lunaticExtra

    // card types
    public bool action;
    public bool reaction;
    public bool item;
    public bool invocation;
    public bool danmaku;
    public bool dodge;
    public bool healing;
    public bool powerup;
    public bool defense;
    public bool artifact;
}

public class SyncListMDCard : SyncListStruct<MDCard>
{

}