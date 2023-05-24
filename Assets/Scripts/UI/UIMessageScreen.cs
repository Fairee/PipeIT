using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Takes a message and show it to the user on the screen
/// </summary>
public class UIMessageScreen : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;
    /// <summary>
    /// Sets the text of the message to the UI text
    /// </summary>
    /// <param name="message">the text to be shown</param>
    public void SetText(string message) {
        if(message != null)
        text.SetText(message);        
    }    
}
