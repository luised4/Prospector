﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//the ScoreBoard class manages showing the score to the player

public class Scoreboard : MonoBehaviour
{
    public static Scoreboard S; //singleton for Scoreboard

    [Header("Set in Inspector")]
    public GameObject prefabFloatingScore;

    [Header("Set Dynamically")]
    [SerializeField] private int _score = 0;
    [SerializeField] private string _scoreString;

    private Transform canvasTrans;

    //the score property also sets the scoreString
    public int Score
    {
        get
        {
            return (_score);
        }

        set
        {
            _score = value;
            ScoreString = _score.ToString("N0");
        }
    }

    //the scoreString property also sets the Text.text			
    public string ScoreString
    {
        get
        {
            return (_scoreString);
        }

        set
        {
            _scoreString = value;
            GetComponent<Text>().text = _scoreString;
        }
    }

    void Awake()
    {
        if (S == null)
        {
            S = this;
        }

        else
        {
            Debug.Log("Error: Scoreboard.Aware(): S is already set!");
        }

        canvasTrans = transform.parent;
    }

    //when called by SendMessage, this adds the fs.score to this.score
    public void FSCallback(FloatingScore fs)
    {
        Score += fs.Score;
    }

    //this will instantiate a new FloatingScore GameObject and initialize it
    //it also returns a pointer to the FloatingScore created so that the
    //calling function can do more with it (like set font sizes and so on)
    public FloatingScore CreateFloatingScore(int amt, List<Vector2> pts)
    {
        GameObject go = Instantiate<GameObject>(prefabFloatingScore);
        go.transform.SetParent(canvasTrans);

        FloatingScore fs = go.GetComponent<FloatingScore>();
        fs.Score = amt;
        fs.reportFinishTo = this.gameObject; //set fs to call back to this							
        fs.Init(pts);
        return (fs);
    }
}