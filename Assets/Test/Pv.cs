using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using DG.Tweening;

public class Pv : NetworkBehaviour
{
    public int playerNumber;

    void Start()
    {
        Test.Instance.numberOfPlayers += 1;
        playerNumber = Test.Instance.numberOfPlayers;
        name = "pv" + playerNumber;
        
        transform.Find("PlayerUI/HandSize/HandSizeText").GetComponent<Text>().text = playerNumber.ToString();
    }

    void Update()
    {
		
	}
}