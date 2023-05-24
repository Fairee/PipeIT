using Google.XR.ARCoreExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIHVIShowers : MonoBehaviour
{
    [SerializeField]
    private TMP_Text horizontal;
    [SerializeField]
    private TMP_Text vertical;
    [SerializeField]
    private TMP_Text yaw;

    //To get access to the current accuracy and the min accuracy
    GeoSpatialManager geoManager;
    private void Start()
    {
        geoManager = GeoSpatialManager.Instance;
    }

    /// <summary>
    /// Retrieves the accuracy values and updates the colors based on it
    /// </summary>
    private void Update()
    {
        double horizontalWanted, verticalWanted, yawWanted;
        (horizontalWanted, verticalWanted, yawWanted) = geoManager.GetMinValues();
        GeospatialPose pose = geoManager.GetPose();

        horizontal.color = GetColor(horizontalWanted, pose.HorizontalAccuracy);
        vertical.color = GetColor(verticalWanted, pose.VerticalAccuracy);
        yaw.color = GetColor(yawWanted, pose.OrientationYawAccuracy);
    }


    /// <summary>
    /// Gets the appropriate color based on the values
    /// </summary>
    /// <param name="wanted">the wanted accuracy</param>
    /// <param name="have">the current accuracy</param>
    private Color GetColor(double wanted, double have) {
        if (have > 2 * wanted || have == 0)
        {
            return  Color.red;
        }
        else if (have >= wanted)
        {
            return new Color(255, 69, 0);
        }
        else if (have > wanted * 0.75)
        {
            return Color.green;
        }
        else
        {
            return Color.blue;
        }

    }
}
