using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using DG.Tweening;

public class Draggable : MonoBehaviour  // TODO: understand the pointer displacement stuff, possibly simplify it
{
    public static bool dragging = false;    // a flag to know if we are dragging any GameObjects

    public bool draggingThis = false;       // a flag to know if we are currently dragging this GameObject
    public bool canDrag = true;
    private Vector3 pointerDisplacement;    // distance from the center of this GameObject to the point where we clicked to start dragging
    private float zDisplacement;            // distance from camera to mouse on Z axis

    void OnMouseDown()
    {
        if (canDrag)
        {
            dragging = true;
            draggingThis = true;
            HoverPreview.PreviewsAllowed = false;  // when we are dragging something, all previews should be off
            transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "Dragging";

            zDisplacement = -Camera.main.transform.position.z + transform.position.z;
            Vector3 screenMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDisplacement);
            pointerDisplacement = -transform.position + Camera.main.ScreenToWorldPoint(screenMousePos);
        }
    }

    void Update()
    {
        if (draggingThis)
        {
            Vector3 screenMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDisplacement);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenMousePos);
            transform.position = new Vector3(mousePos.x - pointerDisplacement.x, mousePos.y - pointerDisplacement.y, transform.position.z);
        }
    }

    IEnumerator DelayPreviews()
    {
        yield return new WaitForSeconds(0.25f);
        if (!dragging) HoverPreview.PreviewsAllowed = true;  // only re-enable previews if we didn't start dragging another object
    }

    void OnMouseUp()
    {
        if (draggingThis)
        {
            dragging = false;
            draggingThis = false;
            StartCoroutine(DelayPreviews());  // re-enable previews after 0.25 seconds (so previews don't immediately pop up when releasing a card)
            string cardName = GetComponent<CardInfo>().cardName;
            bool targetsPlayers = GetComponent<CardInfo>().targetsPlayers;
            bool canTargetSelf = GetComponent<CardInfo>().canTargetSelf;
            string hit = DetectHit(targetsPlayers, canTargetSelf);
            bool returnToPosition = true;

            // if it's your main step
            if (Logic.Instance.currentPlayer == Player.localPlayer && Logic.Instance.mainStepActive && Logic.Instance.currentEvent == null)
            {
                if (cardName == "shootSpring" && hit != null)
                {
                    Player.localPlayer.CmdShoot(hit, transform.GetSiblingIndex());
                    returnToPosition = false;
                }
            }
            // if you're reacting to an event
            else if (Logic.Instance.currentEvent != null)  
            {
                VisualContainer vcHand = Player.localPlayer.visual.transform.Find("Hand").GetComponent<VisualContainer>();
                VisualContainer vcIntentArea = GameObject.Find("Visual/IntentArea").GetComponent<VisualContainer>();

                if (cardName == "grazeSpring" && hit == "PlayArea" && transform.parent.name == "Hand")
                {
                    Player.localPlayer.CmdIntendGraze();
                    transform.GetComponent<HoverPreview>().ThisPreviewEnabled = false;

                    vcHand.Remove(transform);
                    vcHand.Adjust(transform.childCount);
                    vcIntentArea.Add(transform);
                    vcIntentArea.Adjust(transform.childCount);

                    returnToPosition = false;
                }

                if (cardName == "grazeSpring" && hit == "HandArea" && transform.parent.name == "IntentArea")
                {
                    vcIntentArea.Remove(transform);
                    vcIntentArea.Adjust(transform.childCount);
                    vcHand.Add(transform);
                    vcHand.Adjust(transform.childCount);

                    returnToPosition = false;
                }
            }

            if (returnToPosition) StartCoroutine(PlayAnimation("ReturnToPosition"));
        }
    }

    public string DetectHit(bool targetsPlayers, bool canTargetSelf)
    {
        // raycast to mousePosition and store all the hits in the array
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 30f);

        if (targetsPlayers)
        {
            for (int i = 1; i < Logic.Instance.numberOfPlayers + 1; i++)  // TODO: this can potentially go wrong once players start dying
            {
                // skips checking own collider for cards that cannot target yourself (like Shoot)
                if (!canTargetSelf && Player.localPlayer.playerNumber == i) continue;

                BoxCollider bc = GameObject.Find("Visual/Players/Player" + i + "/Character").transform.GetChild(0).GetComponent<BoxCollider>();

                // check if the collider that we hit is the collider on a Player, returns their playerNumber
                foreach (RaycastHit h in hits) if (h.collider == bc) return i.ToString();
            }
        }
        else
        {
            // check if the collider that we hit is the collider on the PlayArea
            foreach (RaycastHit h in hits)
                if (h.collider == GameObject.Find("Visual/PlayArea").GetComponent<BoxCollider>()) return "PlayArea";

            // check if the collider that we hit is the collider on the HandArea
            foreach (RaycastHit h in hits)
                if (h.collider == GameObject.Find("Visual/HandArea").GetComponent<BoxCollider>()) return "HandArea";
        }

        return null;  // no hits
    }

    public IEnumerator PlayAnimation(string animation)
    {
        bool animating = true;
        Sequence s = DOTween.Sequence();

        switch (animation)
        {
            // returns the card to its position in the visual container
            case "ReturnToPosition":
                transform.Find("Canvas").GetComponent<Canvas>().sortingLayerName = "DontDimThisLayer";
                Vector3 positionInContainer = transform.parent.GetComponent<VisualContainer>().positionList[transform.GetSiblingIndex()];
                s.Append(transform.DOLocalMove(positionInContainer, 1f).SetEase(Ease.OutQuint));
                s.OnComplete(() => animating = false);
                break;
            
            // moves the card to play preview area, then the discard pile
            case "SuccessfullyPlayed":
                s.Append(transform.DOMove(GameObject.Find("Visual/PlayPreviewArea").transform.position, 0.75f));
                s.AppendInterval(0.75f);
                s.Append(transform.DOMove(GameObject.Find("Visual/MainDeckDiscardPile").transform.position, 0.75f));
                s.OnComplete(() =>
                {
                    Destroy(gameObject);
                    GameObject.Find("Logic/MainDeckDiscardPile").GetComponent<MainDeckDiscardPile>().mainDeckDiscardPile.Insert(0, Factory.mdCardDict[GetComponent<CardInfo>().cardName]);
                    animating = false;
                });
                break;
        }

        while (animating) yield return null;
    }
}