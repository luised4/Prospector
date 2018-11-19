using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//enum to handle all the possible scoring events
public enum eScoreEvent
{
    draw,
    mine,
    mineGold,
    gameWin,
    gameLoss
}

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager S;

    public static int SCORE_FROM_PREV_ROUND = 0;
    public static int HIGH_SCORE = 0;

    [Header("Set Dynamically")]
    public int chain = 0;
    public int scoreRun = 0;
    public int score = 0;

    void Awake()
    {
        if(S == null)
        {
            S = this; //set the private singleton
        }

        else
        {
            Debug.Log("ERROR: ScoreManager.Aware(): S is already set");
        }

        //check for a high score in the PlayerPrefs
        if(PlayerPrefs.HasKey("ProspectorHighScore"))
        {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }

        //add the score from last round, which will be > 0 if it was a win
        score = score + SCORE_FROM_PREV_ROUND;

        //and reset the SCORE_FROM_PREV_ROUND 
        SCORE_FROM_PREV_ROUND = 0;
    }

    public static void EVENT(eScoreEvent evt)
    {
        try
        { //try-catch stops an error from breaking your program
            S.Event(evt);
        }

        catch (System.NullReferenceException nre)
        {
            Debug.LogError("ScoreManager.EVENT() called while s=null.\n" + nre);
        }
    }

    void Event(eScoreEvent evt)
    {
        switch (evt)
        {
            //same things need to happen whether it's a draw, a win, or a lose
            case eScoreEvent.draw: //drawing the card
            case eScoreEvent.gameWin: //won the round
            case eScoreEvent.gameLoss: //lost the round
                chain = 0;
                score += scoreRun;
                scoreRun = 0;
                break;

            case eScoreEvent.mine:
                chain++;
                scoreRun += chain;
                break;
        }

        //this second switch statement handles round wins and loses
        switch (evt)
        {
            case eScoreEvent.gameWin:

                //if its a win, add the score to the next round
                //static fields are NOT reset by SceneManager.LoadScene()
                SCORE_FROM_PREV_ROUND = score;
                print("You won this round! Round Score " + score);
                break;

            case eScoreEvent.gameLoss:

                //if its a loss, check against the high score
                if(HIGH_SCORE <= score)
                {
                    print("You got the high score! High Score: " + score);
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                }

                else
                {
                    print("Your final score for this game was: " + score);
                }

                break;

            default:
                print("score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
                break;
        }
    }

    public static int CHAIN { get { return S.chain; } }
    public static int SCORE { get { return S.score; } }
    public static int SCORE_RUN { get { return S.scoreRun; } }
}
