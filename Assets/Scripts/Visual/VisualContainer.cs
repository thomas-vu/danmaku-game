using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class VisualContainer : MonoBehaviour
{
    public List<Vector3> positionList = new List<Vector3>();
    public float distanceBetweenX;
    public float distanceBetweenY;
    public float distanceBetweenZ;

    public void Add(Transform tf)
    {
        tf.SetParent(transform);
        positionList.Insert(0, new Vector3(distanceBetweenX, distanceBetweenY, distanceBetweenZ) * (transform.childCount - 1));
    }

    public void Insert(int index, Transform tf)
    {
        tf.SetParent(transform);
        tf.SetSiblingIndex(index);
        positionList.Insert(0, new Vector3(distanceBetweenX, distanceBetweenY, distanceBetweenZ) * (transform.childCount - 1));
    }

    public void Remove(Transform tf)
    {
        tf.SetParent(null);
        positionList.RemoveAt(0);
    }

    public void Adjust(int numberOfChildren)
    {
        if (numberOfChildren > 0)
        {
            Sequence s = DOTween.Sequence();

            // setting sorting order and shifting cards in container, also centering the container
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).Find("Canvas").GetComponent<Canvas>().sortingOrder = i;
                s.Insert(0f, transform.GetChild(i).DOLocalMove(positionList[i], 0.5f));
            }

            s.Insert(0f, transform.DOMoveX(-positionList[0].x / 2f, 0.5f));
        }
    }
}