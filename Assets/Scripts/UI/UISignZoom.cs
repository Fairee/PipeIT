using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI that showcase the zoomed information about the pipe
/// </summary>
public class UISignZoom : MonoBehaviour
{
    Button button;
    TMP_Text text;
    void Awake()
    {
        button = GetComponentInChildren<Button>();
        text = GetComponentInChildren<TMP_Text>();
        if (button == null || text == null) {
            Debug.Log("UISignZoom couldnt find its child component");
        }
        button.onClick.AddListener(Close);
    }
    /// <summary>
    /// Closes the UI
    /// </summary>
    private void Close() {
        gameObject.SetActive(false);
    }
    /// <summary>
    /// Sets the text of the UI
    /// </summary>
    /// <param name="message">the text to be shown</param>
    public void SetText(string message) {
        gameObject.SetActive(true);
        text.text = message;
    }
}
