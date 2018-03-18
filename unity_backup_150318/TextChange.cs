using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class TextChange : MonoBehaviour {

    public Text CoordinateText;
    // Use this for initialization
    void Start () {
        
        CoordinateText = GetComponent<Text>();
        CoordinateText.text = "STAM2";
        //Get a reference to our text LevelText's text component by finding it by name and calling GetComponent.
        //CoordinateText = GameObject.Find("COOR_TEXT").GetComponent<Text>();
        //Set the text of levelText to the string "Day" and append the current level number.
        // CoordinateText.text = "STAM Day ";

    }

    // Update is called once per frame
    void Update () {
        //CoordinateText.text = "STAM2";
        //movementSpeed.ToString();

    }
}
