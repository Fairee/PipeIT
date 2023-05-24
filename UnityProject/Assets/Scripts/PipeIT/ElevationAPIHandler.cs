using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Takes care of getting the elevation from the elvation API
/// </summary>
public class ElevationAPIHandler : Singleton<ElevationAPIHandler>
{
    [SerializeField]
    private string apiKey = "AIzaSyC1JAibcPNNYAFit2tPm41_IiXfZBvbqrs";
    /// <summary>
    /// The elevation API has returned a value for given position
    /// </summary>
    public UnityEvent<double, Vector3D> RequestDone;

    private void Start()
    {
        Vector3D pos = new Vector3D(50.161023, 14.74461, 0);
        makeRequest(pos);
    }
    /// <summary>
    /// Sends a request to the Elevation API
    /// </summary>
    /// <param name="position">the position to find the elevation for</param>
    public void makeRequest(Vector3D position) {
        StartCoroutine(ReuqestElevationData(position));
    }

    /// <summary>
    /// Creates the request and waits for the respond, then invokes an event
    /// </summary>
    /// <param name="position">position for the request</param>
    /// <returns></returns>
    IEnumerator ReuqestElevationData(Vector3D position) {
        string url = $"https://maps.googleapis.com/maps/api/elevation/json?locations={position.x.ToString("0.0#######", System.Globalization.CultureInfo.InvariantCulture)},{position.y.ToString("0.0#######", System.Globalization.CultureInfo.InvariantCulture)}&key={apiKey}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseJson = webRequest.downloadHandler.text;

                JObject json = JObject.Parse(responseJson);
                double elevation = (double)json["results"][0]["elevation"];
                RequestDone.Invoke(elevation, position);
            }
            else
            {
                Debug.Log("Elevation request failed: " + webRequest.error);
            }
        }
    }

}
