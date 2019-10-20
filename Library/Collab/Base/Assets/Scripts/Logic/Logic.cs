using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Logic : MonoBehaviour
{
    #region Variables

    // SINGLETON
    public static Logic Instance;

    public int numberOfPlayers = 0;
    public int whoseTurn;

    public bool eventActive;
    public GameObject eventTarget;
    public float eventTimeLeft;

    #endregion

    public void shootEvent(GameObject target)
    {
        eventTarget = target;
        eventActive = true;
    }

    void Update()
    {
        if (eventActive)
        {
            eventTimeLeft -= Time.deltaTime;
            if (eventTimeLeft < 0)
            {
                eventTarget.GetComponent<Player>().health -= 1;

                eventActive = false;
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
        eventActive = false;
        eventTimeLeft = 10.0f;
    }
}