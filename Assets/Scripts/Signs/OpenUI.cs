using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The button to open the UI
/// </summary>
public class OpenUI : MonoBehaviour
{
    public UISignZoom uiSignZoom;
    Sign sign;
    // Start is called before the first frame update

    private void Start()
    {
        sign = GetComponentInParent<Sign>();
    }
    /// <summary>
    /// Opens the UI and sets the text for it
    /// </summary>
    public void OpenIt()
    {
        uiSignZoom.SetText(sign.GetText());
    }
}
