using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

/// <summary>
/// When the user doesn't give us permission for the camera, this will allow them to change their opinion
/// </summary>
public class UICameraPermission : MonoBehaviour
{
    [SerializeField]
    Button button;


    /// <summary>
    /// Add a listner to a button to ask for permissions
    /// </summary>
    private void Start()
    {
        button.onClick.AddListener(RequestCameraPermission);
    }

    /// <summary>
    /// Request the camera permission
    /// </summary>
    private void RequestCameraPermission()
    {
        Permission.RequestUserPermission(Permission.Camera);
    }
}
