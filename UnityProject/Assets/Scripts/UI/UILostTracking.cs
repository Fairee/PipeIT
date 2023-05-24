using Google.XR.ARCoreExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// Updates the Lost tracking screen
/// </summary>
public class UILostTracking : MonoBehaviour
{
    [SerializeField]
    private TMP_Text VPS;

    // To get the VPS status
    GeoSpatialManager geoManager;
    private void Start()
    {
        geoManager = GeoSpatialManager.Instance;
        geoManager.AvaibalityCheck.AddListener(UpdateVPSStatus);
    }
    /// <summary>
    /// Updates the VPS statues based on the availability
    /// </summary>
    /// <param name="availability">The VPSAvailability</param>
    public void UpdateVPSStatus(VpsAvailability availability)
    {
        if (availability == VpsAvailability.Available)
        {
            VPS.text = "VPS is available.";
            VPS.color = Color.green;
        }
        else if (availability == VpsAvailability.Unavailable)
        {
            VPS.text = "VPS is NOT AVAIBLE.";
            VPS.color = Color.red;
        }
        else if (availability == VpsAvailability.ErrorNetworkConnection)
        {
            VPS.text = "A network error.";
            VPS.color = Color.magenta;
        }
        else
        {
            VPS.text = "Error: " + availability.ToString();
            VPS.color = Color.black;
        }
    }
}
