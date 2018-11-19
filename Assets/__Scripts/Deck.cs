using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [Header("Set in Inspector")]
    public bool startFaceUp = false;

    //suits
    public Sprite suitClub;
    public Sprite suitDiamond;
    public Sprite suitHeart;
    public Sprite suitSpade;

    public Sprite[] faceSprites;
    public Sprite[] rankSprites;

    public Sprite cardBack;
    public Sprite cardBackGold;
    public Sprite cardFront;
    public Sprite cardFrontGold;

    //prefabs
    public GameObject prefabCard;
    public GameObject prefabSprite;


    [Header("Set Dynamically")]
    public PT_XMLReader xmlr;
    public List<string> cardNames;
    public List<Card> cards;
    public List<Decorator> decorators;
    public List<CardDefinition> cardDefs;
    public Transform deckAnchor;
    public Dictionary<string, Sprite> dictSuits;

    //InitDeck is called by Prospector when it is ready
    public void InitDeck(string deckXMLText)
    {
        //this creates an anchor for all the card GameObjects in the hierarchy
        if(GameObject.Find("_Deck") == null)
        {
            GameObject anchorGO = new GameObject("_Deck");
            deckAnchor = anchorGO.transform;
        }

        //initialize the Dictionary of SuitSprits with necessary Sprits
        dictSuits = new Dictionary<string, Sprite>()
        {
            { "C", suitClub },
            { "D", suitDiamond },
            { "H", suitHeart },
            { "S", suitSpade }
        };

        ReadDeck(deckXMLText);

        MakeCards();
    }

    //ReadDeck parses the XML file passed to it into CardDefinitions
    public void ReadDeck(string deckXMLText)
    {
        xmlr = new PT_XMLReader(); //creates a new PT_XMLReader
        xmlr.Parse(deckXMLText); //use that PT_XMLReader to parse DeckXML

        //this prints a tst line
        string s = "xml[0] decorator[0] ";
        s += "type=" + xmlr.xml["xml"][0]["decorator"][0].att("type");
        s += "x=" + xmlr.xml["xml"][0]["decorator"][0].att("x");
        s += "y=" + xmlr.xml["xml"][0]["decorator"][0].att("y");
        s += "scale=" + xmlr.xml["xml"][0]["decorator"][0].att("scale");
        //print(s);

        //read decorators for all cards
        decorators = new List<Decorator>(); // Init List of decorators

        //grab PT_XMLHashlist of all <decorator>s in the XML file
        PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];
        Decorator deco;

        for(int i = 0; i < xDecos.Count; i++)
        {
            //for each <decorator> in the XML
            deco = new Decorator(); // Make new decorator

            //copy attributes of the <decorator> to the Decorator
            deco.type = xDecos[i].att("type");

            //bool deco.flip is true if the text of the flip attribute is 1
            deco.flip = (xDecos[i].att("flip") == "1");

            //floats need to be parsed from the attribute strings
            deco.scale = float.Parse(xDecos[i].att("scale"));

            //Vector3 loc initializes to [0,0,0], we just need to modify it
            deco.loc.x = float.Parse(xDecos[i].att("x"));
            deco.loc.y = float.Parse(xDecos[i].att("y"));
            deco.loc.z = float.Parse(xDecos[i].att("z"));

            //add the temporary deco to the List decorators
            decorators.Add(deco);
        }

        //read pip locations for each card number
        cardDefs = new List<CardDefinition>();

        //grab PT_XMLHashlist of all <card>s in the XML file
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];

        for(int i = 0; i < xCardDefs.Count; i++)
        {
            //for each of the <card>s
            //create a new card definition
            CardDefinition cDef = new CardDefinition();

            //parse the attribute values and add them to cDef
            cDef.rank = int.Parse(xCardDefs[i].att("rank"));

            //grab PT_XMLHashlist of all the <pip>s on this <card>
            PT_XMLHashList xPips = xCardDefs[i]["pip"];

            if(xPips != null)
            {
                for(int j = 0; j < xPips.Count; j++)
                {
                    //iterate through all the <pip>s
                    deco = new Decorator();

                    //<pip>s on the <card> are handles via the Decorator Class
                    deco.type = "pip";
                    deco.flip = (xPips[j].att("flip") == "1");
                    deco.loc.x = float.Parse(xPips[j].att("x"));
                    deco.loc.y = float.Parse(xPips[j].att("y"));
                    deco.loc.z = float.Parse(xPips[j].att("z"));

                    if(xPips[j].HasAtt("scale"))
                    {
                        deco.scale = float.Parse(xPips[j].att("scale"));
                    }

                    cDef.pips.Add(deco);
                }
            }

            //face cards (jack, queen, king) have a face attribute
            if(xCardDefs[i].HasAtt("face"))
            {
                cDef.face = xCardDefs[i].att("face");
            }

            cardDefs.Add(cDef);
        }
    }

    //get the proper CardDefinition based on Rank (1-14 is ace to king)
    public CardDefinition GetCardDefinitionByRank(int rnk)
    {
        //search through all of the CardDefinitions
        foreach(CardDefinition cd in cardDefs)
        {
            //if the rank is correct, return this definition
            if(cd.rank == rnk)
            {
                return(cd);
            }
        }

        return(null);
    }

    //make the card GameObjects
    public void MakeCards()
    {
        //cardNames will be the names of cards to build
        //each suit goes from 1 to 14 (eg, C1 to C14 fro clubs)
        cardNames = new List<string>();
        string[] letters = new string[] { "C", "D", "H", "S" };

        foreach(string s in letters)
        {
            for(int i = 0; i < 13; i++)
            {
                cardNames.Add(s + (i + 1));
            }
        }

        //make a list to hold all of the cards
        cards = new List<Card>();

        //iterate through all of the card names that were just made
        for(int i = 0; i < cardNames.Count; i++)
        {
            //make the card and add it to the cards deck
            cards.Add(MakeCard(i));
        }
    }

    private Card MakeCard(int cNum)
    {
        //create a new card GameObject
        GameObject cgo = Instantiate(prefabCard) as GameObject;

        //set the transform.parent of the new card to the anchor
        cgo.transform.parent = deckAnchor;
        Card card = cgo.GetComponent<Card>(); //get the card component

        //this line stacks the cards so that they are all in nice rows
        cgo.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);

        //assign basic values to the card
        card.name = cardNames[cNum];
        card.suit = card.name[0].ToString();
        card.rank = int.Parse(card.name.Substring(1));

        if(card.suit == "D" || card.suit == "H")
        {
            card.colS = "Red";
            card.color = Color.red;
        }

        //pull the CardDefinition for this card
        card.def = GetCardDefinitionByRank(card.rank);

        AddDecorators(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return card;
    }

    //these private variables will be reused several times in the helper methods
    private Sprite _tSP = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;

    private void AddDecorators(Card card)
    {
        //add decorators
        foreach(Decorator deco in decorators)
        {
            if(deco.type == "suit")
            {
                //instantiate a sprite GameObject
                _tGO = Instantiate(prefabSprite) as GameObject;

                //get the SpriteRenderer component
                _tSR = _tGO.GetComponent<SpriteRenderer>();

                //set the sprite to the proper suit
                _tSR.sprite = dictSuits[card.suit];
            }

            else
            {
                _tGO = Instantiate(prefabSprite) as GameObject;
                _tSR = _tGO.GetComponent<SpriteRenderer>();

                //get the proper sprite to show this rank
                _tSP = rankSprites[card.rank];

                //assign this rank sprite to the Sprite Renderer
                _tSR.sprite = _tSP;

                //set the color of the rank to match the suit
                _tSR.color = card.color;
            }

            //make the deco sprites render above the card
            _tSR.sortingOrder = 1;

            //make the decorator sprite a child of the card
            _tGO.transform.SetParent(card.transform);

            //set the localPosition based on the location from DeckXML
            _tGO.transform.localPosition = deco.loc;

            //flip the decorator if needed
            if(deco.flip)
            {
                //an euler rotation of 180 degrees around the Z-axis will flip it
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }

            //set the scale to keep decos from being too big
            if(deco.scale != 1)
            {
                _tGO.transform.localScale = Vector3.one * deco.scale;
            }

            //name this GameObject so its easy to see
            _tGO.name = deco.type;

            //add this deco GameObject to the List card.decoGOs
            card.decoGOs.Add(_tGO);
        }
    }

    private void AddPips(Card card)
    {
        //for each of the pips in the definition...
        foreach(Decorator pip in card.def.pips)
        {
            // ...instantiate a pip game object
            _tGO = Instantiate(prefabSprite) as GameObject;

            //set the parent to be the card GameObject
            _tGO.transform.SetParent(card.transform);

            //set the position to that specified in the XML
            _tGO.transform.localPosition = pip.loc;

            //flip it if necessary
            if(pip.flip)
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }

            //scale it if necessary (only for the ace)
            if(pip.scale != 1)
            {
                _tGO.transform.localScale = Vector3.one * pip.scale;
            }

            //give this game object a name
            _tGO.name = "pip";

            //get the sprite renderer component
            _tSR = _tGO.GetComponent<SpriteRenderer>();

            //set the sprite to the proper suit
            _tSR.sprite = dictSuits[card.suit];

            //set sortingOrder so the pip is rendered above the card_front
            _tSR.sortingOrder = 1;

            //add this to the card's list of pips
            card.pipGOs.Add(_tGO);
        }
    }

    private void AddFace(Card card)
    {
        if(card.def.face == "")
        {
            return; //no need to run if this isn't a face card
        }

        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();

        //generate the right name and pass it to GetFace()
        _tSP = GetFace(card.def.face + card.suit);
        _tSR.sprite = _tSP; //assign this sprite to _tSR
        _tSR.sortingOrder = 1; //set the sortingOrder
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "face";
    }

    //find the proper face card sprite
    private Sprite GetFace(string faceS)
    {
        foreach(Sprite _tSP in faceSprites)
        {
            //if this sprite has the right name...
            if(_tSP.name == faceS)
            {
                //...then return the sprite
                return(_tSP);
            }
        }

        //if nothing can be found, return null
        return(null);
    }

    private void AddBack(Card card)
    {
        //add card back
        //the Card_Back will be able to cover everything else on the card
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;

        //this is a higher sortingOrder than anything else
        _tSR.sortingOrder = 2;
        _tGO.name = "back";
        card.back = _tGO;

        //default to face up
        card.faceUp = startFaceUp; // Use the property faceUp of card
    }

    //shuffle the cards in Deck.cards
    static public void Shuffle(ref List<Card> oCards)
    {
        //create a temporary list to hold the new shuffle order
        List<Card> tCards = new List<Card>();

        int ndx; //this will hold the index of the card to be moved
        tCards = new List<Card>(); //initialize the temporary list

        //repeat as long as there are cards in the original list
        while(oCards.Count > 0)
        {
            //pick the index of a random card
            ndx = Random.Range(0, oCards.Count);

            //add that card to the temporary list
            tCards.Add(oCards[ndx]);

            //remove that card from the original list
            oCards.RemoveAt(ndx);
        }

        //replace the original list with the temporary list
        oCards = tCards;

        //because oCards is a reference parameter, the original arg that was passed in is changed as well
    }
}
