using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unused : MonoBehaviour {

    // This class is a collection of unused functions, saved here in case we want to use them at some point.

    private static GameObject getCurrentlyDragging()
    {
        Draggable[] allDraggables = FindObjectsOfType<Draggable>();

        foreach (Draggable d in allDraggables)
        {
            if (d.draggingThis)
                return d.gameObject;
        }
        return null;
    }

    // Line to find all objects of a type
    //foreach (Player p in FindObjectsOfType<Player>()) if (p.isLocalPlayer) p.DragReleased(gameObject);
}
