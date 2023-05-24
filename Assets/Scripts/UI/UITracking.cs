using Google.XR.ARCoreExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// The calibration screen
/// </summary>
public class UITracking : MonoBehaviour
{
    [SerializeField]
    private TMP_Text horizontal;
    [SerializeField]
    private TMP_Text vertical;
    [SerializeField]
    private TMP_Text yaw;
    [SerializeField]
    private TMP_Text VPS;
    // to be able to check on the availability of the VPS
    GeoSpatialManager geoManager;
    private void Start() {
        geoManager = GeoSpatialManager.Instance;
        geoManager.AvaibalityCheck.AddListener(UpdateVPSStatus);
    }

    private void Update()
    {
        double horizontalWanted, verticalWanted, yawWanted;
        (horizontalWanted, verticalWanted, yawWanted) = geoManager.GetInitValues();
        GeospatialPose pose = geoManager.GetPose();
        UpdateHorizontal(horizontalWanted, pose.HorizontalAccuracy);
        UpdateVertical(verticalWanted, pose.VerticalAccuracy);
        UpdateYaw(yawWanted, pose.OrientationYawAccuracy);

    }
    /// <summary>
    /// Updates the vertical message and color based on the current value
    /// </summary>
    /// <param name="wanted">the value we want to reach</param>
    /// <param name="have">the value that we have</param>
    private void UpdateVertical(double wanted, double have) {
        if (have > 2 * wanted || have == 0)
        {
            vertical.color = Color.red;
            vertical.text = "Your current vertical accuracy is BAD.";
        }
        else if (have >= wanted)
        {
            vertical.color = new Color(255, 69, 0);
            vertical.text = "Your current vertical accuracy is ALMOST good";
        }
        else {
            vertical.color = Color.green;
            vertical.text = "Your current vertical accuracy is GREAT!";
        }
    
    }
    /// <summary>
    /// Updates the horizontal message and color based on the current value
    /// </summary>
    /// <param name="wanted">the value we want to reach</param>
    /// <param name="have">the value that we have</param>
    private void UpdateHorizontal(double wanted, double have)
    {
        if (have > 2 * wanted || have == 0)
        {
            horizontal.color = Color.red;
            horizontal.text = "Your current horizontal accuracy is BAD.";
        }
        else if (have >= wanted)
        {
            horizontal.color = new Color(255, 69, 0);
            horizontal.text = "Your current horizontal accuracy is ALMOST good";
        }
        else
        {
            horizontal.color = Color.green;
            horizontal.text = "Your current horizontal accuracy is GREAT!";
        }
    }
    /// <summary>
    /// Updates the yaw message and color based on the current value
    /// </summary>
    /// <param name="wanted">the value we want to reach</param>
    /// <param name="have">the value that we have</param>
    private void UpdateYaw(double wanted, double have)
    {
        if (have > 2 * wanted || have == 0)
        {
            yaw.color = Color.red;
            yaw.text = "Your current yaw accuracy is BAD.";
        }
        else if (have >= wanted)
        {
            yaw.color = new Color(255, 69, 0);
            yaw.text = "Your current yaw accuracy is ALMOST good";
        }
        else
        {
            yaw.color = Color.green;
            yaw.text = "Your current yaw accuracy is GREAT!";
        }
    }
    /// <summary>
    /// Updates the VPS status message based on the VPS status
    /// </summary>
    /// <param name="availability">The current availability of the VPS</param>
    public void UpdateVPSStatus(VpsAvailability availability)
    {
        if (availability == VpsAvailability.Available)
        {
            VPS.text = "VPS is available at your current location. Try to LOOK AROUND with your device.";
            VPS.color = Color.green;
        }
        else if (availability == VpsAvailability.Unavailable) {
            VPS.text = "VPS is NOT AVAILABLE at your current location. Try to find location with Google StreetsView coverage.";
            VPS.color = Color.red;
        }
        else if (availability == VpsAvailability.ErrorNetworkConnection)
        {
            VPS.text = "Lost internet connection";
            VPS.color = Color.magenta;
        }
        else {
            VPS.text = "VPS got this error: " + availability.ToString() + " Please restart the app.";
            VPS.color = Color.black;
        }
    }
}
