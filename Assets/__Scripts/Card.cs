using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{

    [Header("Set Dynamically")]
    public string suit; //suit of the card
    public int rank; //rank of the card
    public Color color = Color.black; //color to tint pips
    public string colS = "Black"; //or "Red". name of the color

    //this list holds all of the decorator GameObjects
    public List<GameObject> decoGOs = new List<GameObject>();

    //this list holds all of the pip GameObjects
    public List<GameObject> pipGOs = new List<GameObject>();

    public GameObject back; //the GameObject of the back of the card

    public CardDefinition def; //parsed from the DeckXML.xml

    //list of the SpriteRenderer components of this GameObject and its children
    public SpriteRenderer[] spriteRenderers;

    void Start()
    {
        SetSortOrder(0); //ensures that the card starts properly depth sorted    
    }

    //if spriteRenderers is not yet defined, this function will define it
    public void PopulateSpriteRenderers()
    {
        if(spriteRenderers == null || spriteRenderers.Length == 0)
        {
            //get spriteRenderer components of this GameObject and its children
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    //sets the sortingLayerName on the spriteRenderer components
    public void SetSortingLayerName(string tSLN)
    {
        PopulateSpriteRenderers();

        foreach(SpriteRenderer tSR in spriteRenderers)
        {
            tSR.sortingLayerName = tSLN;
        }
    }

    //sets the sortingOrder on the spriteRenderer components
    public void SetSortOrder(int sOrd)
    {
        PopulateSpriteRenderers();

        //iterate through all the spriteRenderers as tSR
        foreach(SpriteRenderer tSR in spriteRenderers)
        {
            if(tSR.gameObject == this.gameObject)
            {
                //if the gameObject is this.gameObject, its the background
                tSR.sortingOrder = sOrd;

                continue;
            }

            //switch based on the names
            switch(tSR.gameObject.name)
            {
                case "back":
                    //set to the highest layer to cover the other sprites 
                    tSR.sortingOrder = sOrd + 2;
                    break;

                case "face":
                default:
                    //set it to the middle layer to be above the background
                    tSR.sortingOrder = sOrd + 1;
                    break;
            }
        }
    }

    public bool faceUp
    {
        get
        {
            return(!back.activeSelf);
        }

        set
        {
            back.SetActive(!value);
        }

    }

    //virtual methods can be overridden by subclass methods with the same name 
    virtual public void OnMouseUpAsButton()
    {
        print(name);
    }
}

    [System.Serializable] //serializable class, can be edited in inspector
    public class Decorator
    {
        //class that stores info about each decorator or pip from DeckXML

        public string type; //for card pips, type = "pip"
        public Vector3 loc; //location of the sprite on the card
        public bool flip = false; //whether to flip the sprite vertically
        public float scale = 1f; //scale of the sprite
    }

    [System.Serializable]
    public class CardDefinition
    {
        //stores info on the rank of each card

        public string face; //sprite to use for each face card
        public int rank; //the rank (1-13) of this card
        public List<Decorator> pips = new List<Decorator>(); //pips used
    }

