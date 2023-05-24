using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

/// <summary>
/// Lets the user changed their decision about not giving us the location permission
/// </summary>
public class UILocationPermission : MonoBehaviour
{
    [SerializeField]
    Button button;

    private void Start()
    {
        button.onClick.AddListener(RequestLocationPermission);
    }
    /// <summary>
    /// asks for the location permission
    /// </summary>
    private void RequestLocationPermission()
    {
        Permission.RequestUserPermission(Permission.FineLocation);
    }
}
