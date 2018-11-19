using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Prospector : MonoBehaviour
{

    static public Prospector S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f; //2 second delay between rounds
    public Text gameOverText, roundResultText, highScoreText;

    [Header("Set Dynamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

    void Awake()
    {
        S = this;
        SetUpUITexts();
    }

    void SetUpUITexts()
    {
        //set up the highscore UI text
        GameObject go = GameObject.Find("HighScore");

        if(go != null)
        {
            highScoreText = go.GetComponent<Text>();
        }

        int highScore = ScoreManager.HIGH_SCORE;
        string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
        go.GetComponent<Text>().text = hScore;

        //set up the UI texts that show at the end of the round
        go = GameObject.Find("GameOver");

        if(go != null)
        {
            gameOverText = go.GetComponent<Text>();
        }

        go = GameObject.Find("RoundResult");

        if (go != null)
        {
            roundResultText = go.GetComponent<Text>();
        }

        //make the end of the round texts invisible
        ShowResultsUI(false);
    }

    void ShowResultsUI(bool show)
    {
        gameOverText.gameObject.SetActive(show);
        roundResultText.gameObject.SetActive(show);
    }

    // Use this for initialization
    void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;

        deck = GetComponent<Deck>(); //get the deck
        deck.InitDeck(deckXML.text); //parse DeckXML to it
        Deck.Shuffle(ref deck.cards); //this shuffles the deck by reference

        Card c;
        for(int cNum = 0; cNum < deck.cards.Count; cNum++)
        {
            c = deck.cards[cNum];
            c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
        }

        layout = GetComponent<Layout>(); //get the layout component
        layout.ReadLayout(layoutXML.text); //pass LayoutXML to it
        drawPile = ConvertListCardsTOListCardProspectors(deck.cards);
        LayoutGame();
    }

    List<CardProspector> ConvertListCardsTOListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach(Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }

        return lCP;
    }

    //the draw function will pull a single card from the drawPile and return it
    CardProspector Draw()
    {
        CardProspector cd = drawPile[0];
        drawPile.RemoveAt(0);
        return cd;
    }

    //LayoutGame() positions the initial tableau of cards, AKA the mine
    void LayoutGame()
    {

        if(layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        CardProspector cp;

        //follow the layout
        foreach(SlotDef tSD in layout.slotDefs)
        {
            cp = Draw(); //pull a card from the top (beginning) of the draw 
            cp.faceUp = tSD.faceUp; //set its faceUp to the value in SlotDef
            cp.transform.parent = layoutAnchor;

            //set the localPosition of the card based on slotDef
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);

            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            cp.state = eCardState.tableau;

            //CardProspectors in the tableau have the state CardState.tableau
            cp.SetSortingLayerName(tSD.layerName); //set the sorting layers

            tableau.Add(cp); //add this CardProspector to the list tableau
        }

        //set which cards are hiding others
        foreach(CardProspector tCP in tableau)
        {
            foreach(int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenby.Add(cp);
            }
        }

        MoveToTarget(Draw()); //set up the initial target card
        UpdateDrawPile(); //set up the Draw pile
    }

    private CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach(CardProspector tCP in tableau)
        {
            if(tCP.layoutID == layoutID)
            {
                return tCP;
            }
        }

        //if not found, return null
        return null;
    }

    //this turns cards in the mine face up or face down
    private void SetTableauFaces()
    {
        foreach(CardProspector cd in tableau)
        {
            bool faceUp = true; //assume the card will be faceup

            foreach(CardProspector cover in cd.hiddenby)
            {
                //if either of the covering cards are in the tableau
                if(cover.state == eCardState.tableau)
                {
                    faceUp = false; //then this card is face down
                }
            }

            cd.faceUp = faceUp; //set the value on the card
        }
    }

    //moves the current target to this discardPile
    void MoveToDiscard(CardProspector cd)
    {
        //set the state of the card to discard
        cd.state = eCardState.discard;
        discardPile.Add(cd); //add to the discard pile list
        cd.transform.parent = layoutAnchor; //update its transform parent

        //position this card on the discard pike
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID + 0.5f);

        cd.faceUp = true;

        //place it on top of the pile for depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    //make cd the new target card
    void MoveToTarget(CardProspector cd)
    {
        //if there is currently a target card, move it to discardPile
        if (target != null)
        {
            MoveToDiscard(target);
        }

        target = cd; //cd is the new target
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        //move to the target position
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID);

        cd.faceUp = true;

        //set the depth sorting
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    //arranges all of the cards in the drawPile to show how many are left
    void UpdateDrawPile()
    {
        CardProspector cd;

        //go through all of the cards of the draw pile
        for(int i = 0; i < drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            //position it correctly with the layout.drawpile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;

            cd.transform.localPosition = new Vector3(layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x), layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y), -layout.drawPile.layerID + 0.1f * i);

            cd.faceUp = false;
            cd.state = eCardState.drawpile;

            //set depth of sorting
            cd.SetSortingLayerName(layout.discardPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    //called any time a card is clicked in the game
    public void CardClicked(CardProspector cd)
    {
        //the reaction is determined by the state of the clicked card
        switch(cd.state)
        {
            case eCardState.target:
                //clicking the target card does nothing
                break;

            case eCardState.drawpile:
                //clicking any card in the drawpile will draw the next card
                MoveToDiscard(target);
                MoveToTarget(Draw());
                UpdateDrawPile();
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;

            case eCardState.tableau:
                //clicking a card in the tableau will check if it's a valid play

                bool validMatch = true;

                //if the card is faced down, its not valid
                if(!cd.faceUp)
                {
                    validMatch = false;
                }

                if(!AdjacentRank(cd, target))
                {
                    //if it's not an adjacent rank, its not valid
                    validMatch = false;
                }

                if(!validMatch) 
                {
                    return; //return if not valid
                }

                tableau.Remove(cd); //remove it from the tableau List
                MoveToTarget(cd); //make it the target card
                SetTableauFaces(); //update tableau card face-ups
                ScoreManager.EVENT(eScoreEvent.mine);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
                
        }

        //check to see wether game is over or not
        CheckForGameOver();
    }

    //check whether game is over
    private void CheckForGameOver()
    {
        //if tableau is empty, the game is over
        if(tableau.Count == 0)
        {
            //call game over with a win
            GameOver(true);
            return;
        }

        //if there are still cards in the drawpile, the games not over
        if(drawPile.Count > 0)
        {
            return;
        }

        //check for remaining valid plays
        foreach(CardProspector cd in tableau)
        {
            if(AdjacentRank(cd, target))
            {
                //if there's a valid play, the games not over
                return;
            }
        }

        //since there are no valid plays, the game is over
        GameOver(false);
    }

    //called when the game is over
    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;

        if(fsRun != null)
        {
            score += fsRun.score;
        }

        if (won)
        {
            gameOverText.text = "Round Over";
            roundResultText.text = "You won this round!\nRound Score: " + score;
            ShowResultsUI(true);
            ScoreManager.EVENT(eScoreEvent.gameWin);
            FloatingScoreHandler(eScoreEvent.gameWin);
        }

        else
        {
            gameOverText.text = "Game Over";

            if(ScoreManager.HIGH_SCORE <= score)
            {
                string str = "You got the high score!\nHigh Score: " + score;
                roundResultText.text = str;
            }

            else
            {
                roundResultText.text = "Your final score was: " + score;
            }

            ShowResultsUI(true);
            ScoreManager.EVENT(eScoreEvent.gameLoss);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }

        //reload the scene, resetting the game
        //SceneManager.LoadScene("Prospector_Scene0");

        //reload the scene in reloadDelay seconds
        //this will give the score a moment to travel
        Invoke("ReloadLevel", reloadDelay);
    }

    void ReloadLevel()
    {
        //reload the scene, resetting the game
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    //return true if the two cards are adjacent in rank (ace and king wraparound)
    public bool AdjacentRank(CardProspector c0, CardProspector c1)
    {
        //if either card is face down, it's not adjacent
        if(!c0.faceUp || !c1.faceUp)
        {
            return (false);
        }

        //if they are one apart, they are adjacent
        if(Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }

        //if one is an ace and the other king, they are adjacent
        if (c0.rank == 1 && c1.rank == 13)
        {
            return (true);
        }

        if (c0.rank == 13 && c1.rank == 1)
        {
            return (true);
        }

        //otherwise return false
        return (false);
    }

    //handle FloatingScore movement
    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;

        switch(evt)
        {
            //same things need to happen whether it's a draw, win or loss
            case eScoreEvent.draw: //drawing a card
            case eScoreEvent.gameWin: //win the round
            case eScoreEvent.gameLoss: //lose the round

                //add fsRun to the Scoreboard score
                if(fsRun != null)
                {
                    //create points for the bezier curve
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);

                    //also adjuts the fontSize
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null; //clear fsRun so it's created again
                }

                break;

            case eScoreEvent.mine: //remove a mine card

                //create a FloatingScore for this score
                FloatingScore fs;

                //move it from the mosuePosition to fsPosRun
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });

                if(fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }

                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }

                break;
        }
    }
}
