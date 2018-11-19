
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eCardState
{
    drawpile,
    tableau,
    target,
    discard
}

public class CardProspector : Card
{ 
    //CardProspector extends card
    [Header("Set Dynamically: CardProspector")]
    public eCardState state = eCardState.drawpile;

    //this hiddenBy list stores which other cards will keep this one face down
    public List<CardProspector> hiddenby = new List<CardProspector>();

    //this layoutID matches this card to the tablear XML if its a tableau card
    public int layoutID;

    //the SlotDef class stores info pulled from the LayoutXML <slot>
    public SlotDef slotDef;

    //this allows the card to react to being clicked
    public override void OnMouseUpAsButton()
    {
        //call the CardClicked method on the prospector singleton
        Prospector.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }
}