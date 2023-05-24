using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using System.Text;
using System.IO;
using TMPro;

/// <summary>
/// This is for Debugging purposes. This saves information in a file and then prints it onto the debug log.
/// This file will not be accessible to the user on the phone ... It's pain working with androids
/// If its not working, you may not be using the right manifest. You need to use a custom manifest and add these two lines:
///   <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"/>
///   < application android: requestLegacyExternalStorage = "true" >
///   It also show some debbuging information on the screen when set ... Used for the debbuging UI (which is turned off for the release)
/// </summary>
public class SnapShot : MonoBehaviour
{
    [SerializeField]
    Button button;

    [SerializeField]
    Button button2;
    [SerializeField]
    Button button3;

    [SerializeField]
    private TMP_Text text;

    float deltaTime = 0;

    private GeoSpatialManager geoSpatialManager;
    private RemoteDataHandler remoteDataHandler;


    private void Start()
    {
        //if needed ask for the permision to be able to write into a file
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        geoSpatialManager = GeoSpatialManager.Instance;
        remoteDataHandler = RemoteDataHandler.Instance;
        button.onClick.AddListener(SnapShotIT);
        button2.onClick.AddListener(SnapRead);
        button3.onClick.AddListener(SnapClear);
    }
    /// <summary>
    /// Counts the FPS, gets the users poisiton and gets the recieved and sent bytes and put it into the text
    /// </summary>
    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        Vector3D position = geoSpatialManager.GetCurrentPosition();
        text.text = position.x.ToString() + " " + position.y.ToString() + " " + position.z.ToString() + "Rotation: " + geoSpatialManager.GetRotation().eulerAngles + "Sent: " + remoteDataHandler.byteCounterSend + " Recieved: " + remoteDataHandler.byteCounterRecieved;
        text.text += "FPS " + fps;
    }

    /// <summary>
    /// Takes a snapshot of your current position and rotation and add it to the file
    /// </summary>
    private void SnapShotIT()
    {
        Vector3D vec = geoSpatialManager.GetCurrentPosition();
        Vector3 rot = geoSpatialManager.GetRotation().eulerAngles;
        StringBuilder row = new StringBuilder();
        row.Append(vec.x.ToString("G17"));
        row.Append(",");
        row.Append(vec.y.ToString("G17"));
        row.Append(",");
        row.Append(vec.z.ToString("G17"));
        row.Append(",");
        row.Append(rot.x.ToString("G17"));
        row.Append(",");
        row.Append(rot.y.ToString("G17"));
        row.Append(",");
        row.Append(rot.z.ToString("G17"));
        row.Append("\n");
        string s = row.ToString();
        Debug.Log(s);
        string path = Path.Combine(Application.persistentDataPath, "myFile.txt");
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            File.AppendAllText(path, s);
        }
    }

    /// <summary>
    /// Reads the file and write it to the debug in messages of chunk size
    /// </summary>
    private void SnapRead()
    {
        string path = Path.Combine(Application.persistentDataPath, "myFile.txt");
        string content = File.ReadAllText(path);
        int chunk = 800;
        for (int i = 0; i < content.Length; i += chunk)
        {
            
            if (i + chunk > content.Length)
            {
                chunk = content.Length - 1 - i; 
            }
            Debug.Log(content.Substring(i, chunk));
        }
    }
    /// <summary>
    /// Clear the file
    /// </summary>
    private void SnapClear()
    {
        string path = Path.Combine(Application.persistentDataPath, "myFile.txt");
        string content = "";
        File.WriteAllText(path, content);
    }
}