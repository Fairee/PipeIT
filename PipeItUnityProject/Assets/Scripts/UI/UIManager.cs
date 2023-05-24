using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using TMPro;

/// <summary>
/// Makes the some of the screen appear and hide based on the state of the Geospatial Handler
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject cameraPermissionScreen;

    [SerializeField]
    private GameObject locationPermissionScreen;

    [SerializeField]
    private GameObject messageScreen;

    [SerializeField]
    private GameObject trackingScreen;

    [SerializeField]
    private GameObject losttracking;

    [SerializeField]
    private GameObject mainScreen;

    [SerializeField]
    private GameObject dataRetrievalFailour;


    private GameObject active;

    private GeoSpatialManager geoManager;
    private RemoteDataHandler remoteDataHandler;



    private void Start()
    {
        //hide all the UIs
        cameraPermissionScreen.SetActive(false);
        locationPermissionScreen.SetActive(false);
        messageScreen.SetActive(false);
        trackingScreen.SetActive(false);
        losttracking.SetActive(false);
        mainScreen.SetActive(false);
        dataRetrievalFailour.SetActive(false);
        geoManager = GeoSpatialManager.Instance;
        geoManager.ErrorStateChanged.AddListener(UpdateState);
        remoteDataHandler = RemoteDataHandler.Instance;
        remoteDataHandler.TimeRunOut.AddListener(DataRetrievalFailMessage);
    }


    /// <summary>
    /// Give the data retrieval error message
    /// </summary>
    /// <param name="message">The message to be displayed to the user</param>
    private void DataRetrievalFailMessage(string message) {
        dataRetrievalFailour.SetActive(true);
        dataRetrievalFailour.GetComponent<UIMessageScreen>().SetText(message);
    }

    /// <summary>
    /// Decide what screen to show based on the Error state
    /// </summary>
    /// <param name="error">the current error state</param>
    /// <param name="message">the message to be shown</param>
    private void UpdateState(GeoSpatialManager.ErrorState error, string message) {
        if (active != null)
        {
            active.SetActive(false);
        }
        
        switch (error){
            case GeoSpatialManager.ErrorState.Camera:
                active = cameraPermissionScreen;
                Debug.Log(active.name + "Was set");
                break;
            case GeoSpatialManager.ErrorState.Location:
                active = locationPermissionScreen;
                Debug.Log(active.name + "Was set");
                break;
            case GeoSpatialManager.ErrorState.Message:
                active = messageScreen;
                messageScreen.GetComponent<UIMessageScreen>().SetText(message);
                break;
            case GeoSpatialManager.ErrorState.Tracking:
                active = trackingScreen;
                break;
            case GeoSpatialManager.ErrorState.NoError:
                //active the main Screen when the calibration is done
                if (active == trackingScreen)
                    mainScreen.SetActive(true);
                active = null;
                break;
            case GeoSpatialManager.ErrorState.LostLocation:
                active = losttracking;
                break;
            default:
                active = null;
                break;
        
        }

        if (active != null)
        {
            Debug.Log(active.name + "Was set to active");
            active.SetActive(true);
        }
    }

}
