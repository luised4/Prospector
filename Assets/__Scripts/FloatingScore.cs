using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

//an enum to track the possible states of a FloatingScore
public enum eFSState
{
    idle,
    pre,
    active,
    post
}

//FloatingScore can move itself on the screen following a bezier curve
public class FloatingScore : MonoBehaviour
{
    [Header("Set Dynamically")]
    public eFSState state = eFSState.idle;

    [SerializeField]
    private int _score = 0;
    public string scoreString;

    //the score property sets both _score and scoreString
    public int score
    {
        get
        {
            return(_score);
        }

        set
        {
            _score = value;
            scoreString = score.ToString("N0"); //"N0" adds commands to the num

            //search "C# standard numeric format strings" for ToString formats
            GetComponent<Text>().text = scoreString;
        }
    }

    public List<Vector2> bezierPts; //bezier points for movement
    public List<float> fontSizes;   //bezier points for font scaling
    public float timeStart = -1f;
    public float timeDuration = 1f;
    public string easingCurve = Easing.InOut; //uses easing in Utils.cs

    //the GameObject that will receive the SendMessage when this is done moving
    public GameObject reportFinishTo = null;
    private RectTransform rectTrans;
    private Text txt;

    //set up the FloatingScore and movement
    public void Init(List<Vector2> ePts, float eTimeS = 0, float eTimeD = 1)
    {
        rectTrans = GetComponent<RectTransform>();
        rectTrans.anchoredPosition = Vector2.zero;
        txt = GetComponent<Text>();

        bezierPts = new List<Vector2>(ePts);

        if(ePts.Count == 1)
        {   
            //if theres only one point												
            transform.position = ePts[0];
            return;
        }

        //if eTimeS is the default, just start at the current time			
        if(eTimeS == 0)
        {
            eTimeS = Time.time;
        }

        timeStart = eTimeS;
        timeDuration = eTimeD;
        state = eFSState.pre; //set it to the pre state, ready to start moving			
    }

    public void FSCallback(FloatingScore fs)
    {
        //when this callback is called by SendMessage,
        //add the score from the calling FloatingScore
        score += fs.score;
    }

    //update is called once per frame
    void Update()
    {
        //if this is not moving, just return							
        if (state == eFSState.idle)
        {
            return;
        }

        //get u from the current time and duration
        //u ranges from 0 to 1
        float u = (Time.time - timeStart) / timeDuration;

        //use easing class from utils to curve the u value								
        float uC = Easing.Ease(u, easingCurve);

        if(u < 0)
        { 
            //if u < 0, then we shouldn't move yet
            state = eFSState.pre;
            txt.enabled = false; //hide the score initally
        }

        else
        {
            if(u >= 1)
            { 
                //if u >= 1, we're done moving
                uC = 1; //Set uC=1 so we don't overshoot
                state = eFSState.post;

                if(reportFinishTo != null)
                { 
                    //if theres a callback game object
                    //use SendMessage to call the FSCallback method with this parameter
                    reportFinishTo.SendMessage("FSCallback", this);

                    //now that the message has been sent, destroy this GameObject																	
                    Destroy(gameObject);
                }

                else
                {   
                    //if there is nothing to callback
                    state = eFSState.idle;
                }
            }

            else
            {
                //0 <= u <= 1, which means that this is active and moving
                state = eFSState.active;
                txt.enabled = true;
            }

            //use bezier curve to move this to the right point											
            Vector2 pos = Utils.Bezier(uC, bezierPts);

            //RectTransform anchors can be used to position UI objects relative
            //to total size of the screen
            rectTrans.anchorMin = rectTrans.anchorMax = pos;

            if(fontSizes != null && fontSizes.Count > 0)
            {
                //if fontSizes has a value in it, then adjust the fontsize
                int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes));
                GetComponent<Text>().fontSize = size;
            }
        }
    }
}
