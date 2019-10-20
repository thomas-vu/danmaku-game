using System.Collections;
using System.Collections.Generic;

public class Event
{
    public string eventName;
    public Player initiator;
    public Player target;    
    public string reactionOrder;

    public List<string>[] reactions;
    public bool grazed;
    
    public Event(string eventName, Player initiator, Player target, string reactionOrder)
    {
        this.eventName = eventName;
        this.initiator = initiator;
        this.target = target;
        this.reactionOrder = reactionOrder;

        reactions = new List<string>[reactionOrder.Length];
        for (int i = 0; i < reactions.Length; i++) reactions[i] = new List<string>();

        grazed = false;
    }

    public void Resolve()
    {
        if (eventName == "shootSpring")
        {
            foreach (List<string> playerReactions in reactions)
                foreach (string reaction in playerReactions)
                    if (reaction == "grazeSpring" && !grazed)
                    {
                        grazed = true;

                    }

            if (!grazed) target.health -= 1;
        }
    }
}