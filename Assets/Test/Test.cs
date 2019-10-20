using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using DG.Tweening;

public class Test : NetworkBehaviour
{
    public static Test Instance;
    public int numberOfPlayers = 0;

    List<Vector3> spawnPositions4 = new List<Vector3>
    {
        new Vector3(0, -2f, -1f),           // bottom spawn (6 o'clock), going clockwise
        new Vector3(-5.5f, 0.5f, -1f),
        new Vector3(0, 3.5f, -1f),
        new Vector3(5.5f, 0.5f, -1f)
    };

    [ClientRpc]
    void RpcTest()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            GameObject.Find("pv" + (i+1)).transform.position = spawnPositions4[i];
        }
    }

    [ClientRpc]
    void RpcTest2()
    {
        int prev = int.Parse(GameObject.Find("pv1/PlayerUI/HandSize/HandSizeText").GetComponent<Text>().text);
        GameObject.Find("pv1/PlayerUI/HandSize/HandSizeText").GetComponent<Text>().text = (prev + 1).ToString();
    }

    void RpcTest3()
    {
        RpcTest2();
    }

    void Start()
    {
        Instance = this;
    }

    void Update()
    {
        if (isServer && Input.GetKeyDown(KeyCode.Q))
        {
            RpcTest();
        }

        if (isServer && Input.GetKeyDown(KeyCode.W))
        {
            Invoke("RpcTest3", 5f);
        }
    }
}