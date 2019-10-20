using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Factory : MonoBehaviour
{
    public static Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    public static Dictionary<string, MDCard> mdCardDict = new Dictionary<string, MDCard>();

    #region Prefabs

    public GameObject playerVisual;

    public GameObject hakureiReimu;

    public GameObject shootSpring;
    public GameObject grazeSpring;

    #endregion

    #region MDCards

    public static MDCard shootSpringMD = new MDCard
    {
        name = "shootSpring",
        season = "spring",
        pointValue = 1,
        expansion = "base",
        action = true,
        danmaku = true
    };

    public static MDCard grazeSpringMD = new MDCard
    {
        name = "grazeSpring",
        season = "spring",
        pointValue = 1,
        expansion = "base",
        reaction = true,
        dodge = true
    };

    #endregion

    void Awake()
    {
        prefabDict.Add("playerVisual", playerVisual);
        prefabDict.Add("hakureiReimu", hakureiReimu);
        prefabDict.Add("shootSpring", shootSpring);
        prefabDict.Add("grazeSpring", grazeSpring);

        mdCardDict.Add("shootSpring", shootSpringMD);
        mdCardDict.Add("grazeSpring", grazeSpringMD);
    }
}