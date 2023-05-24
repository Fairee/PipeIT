using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Sign : MonoBehaviour
{
    List<string> data;
    string[] fieldnames;
    double elevation;
    Vector3D position;
    //To get the elevation of the sign
    ElevationAPIHandler elevationHandler;
    //To get the type of the signs (small or big)
    SettingsManager settingsManager;
    //To get the dbf fields
    RemoteDataHandler remoteDataHandler;


    [SerializeField]
    TMP_Text textBig;
    [SerializeField]
    TMP_Text textSmall;
    float animationTime = 1f;

    [SerializeField]
    GameObject small;
    [SerializeField]
    GameObject big;

    Transform cam;
    // Start is called before the first frame update
    void Awake()
    {
        //Initialzes all the neccessary stuff
        elevationHandler = ElevationAPIHandler.Instance;
        elevationHandler.RequestDone.AddListener(Refresh);

        settingsManager = SettingsManager.Instance;
        settingsManager.SignTypeChanged.AddListener(ChangeSignType);

        remoteDataHandler = RemoteDataHandler.Instance;

        small = transform.Find("Small").gameObject;
        big = transform.Find("Big").gameObject;

        cam = Camera.main.transform;
        textBig = big.transform.Find("Text").GetComponent<TMP_Text>();
        textSmall = small.transform.Find("Text").GetComponent<TMP_Text>();
        ChangeSignType(settingsManager.GetSignType());
        Initialize();
        StartCoroutine(CreationAnimation());
    }
    /// <summary>
    /// Changes the type of the sign (big or small)
    /// </summary>
    /// <param name="type">the type to change to</param>
    private void ChangeSignType(int type) {
        if (type == 0)
        {
            small.SetActive(false);
            big.SetActive(true);
        }
        else {
            big.SetActive(false);
            small.SetActive(true);
        }
    
    }
    /// <summary>
    /// Simple animation that changes the scale of the sign over one second
    /// </summary>
    /// <returns></returns>
    IEnumerator CreationAnimation() {
        float elapsedTime = 0f;

        while (elapsedTime < animationTime) {
            float t = elapsedTime / animationTime;
            float value = Mathf.Lerp(0, 1, t);
            transform.localScale = new Vector3(value, value, value);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = new Vector3(1, 1, 1);
    }
    /// <summary>
    /// Changes the scale of the sign over the second and then destroys it
    /// </summary>
    /// <returns></returns>
    public IEnumerator DestroyAnimation() {
        float elapsedTime = 0f;

        while (elapsedTime < animationTime)
        {
            float t = elapsedTime / animationTime;
            float value = Mathf.Lerp(1, 0, t);
            transform.localScale = new Vector3(value, value, value);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);

    }
    /// <summary>
    /// Refreshes the text on the sign (When the elevation api give us the result)
    /// </summary>
    /// <param name="elevation"> The found elevation
    /// <param name="vector">The position of the sign that is being changed</param>
    private void Refresh(double elevation, Vector3D vector)
    {
        if (position == vector) {
            this.elevation = elevation;
            SetTextBig();
            SetTextSmall();
        }
    }
    /// <summary>
    /// Set the sign value
    /// </summary>
    /// <param name="data">The .dbf data that the sign should showcase</param>
    /// <param name="position">The position of the sign</param>
    /// <param name="uisignzoom">The reference to the zoom UI</param>
    public void SetSign(List<string> data, Vector3D position, UISignZoom uisignzoom) {
        this.data = data;
        this.position = position;
        big.GetComponent<OpenUI>().uiSignZoom = uisignzoom;
        small.GetComponentInChildren<OpenUI>().uiSignZoom = uisignzoom;
        elevationHandler.makeRequest(position);
        SetTextBig();
        SetTextSmall();
    }
    /// <summary>
    /// Gets the text from the big sign
    /// </summary>
    /// <returns>The text of the big sign</returns>
    public string GetText() {
        string text = "";
        for (int i = 0; i < fieldnames.Length - 1; i++)
        {
            text += fieldnames[i] + ": " + data[i] + "\n";
        }
        text += "Nadmorska vyska " + elevation.ToString("0.####") + "\n";
        text += "Podzemi: " + (elevation-position.z).ToString("0.####") + "\n";
        return text;
    }
    /// <summary>
    /// Sets the text for the Big sign
    /// </summary>
    private void SetTextBig() {
        string text = "";
        for (int i = 0; i < fieldnames.Length - 1; i++) {
            text += fieldnames[i] + ": " + data[i] + "\n";
        }
        text += "Nadmorska vyska " + elevation.ToString("0.####") + "\n";
        text += "Podzemi: " + (elevation - position.z).ToString("0.####") + "\n";
        textBig.text = text;
    }
    /// <summary>
    /// Sets the text for the small sign
    /// </summary>
    private void SetTextSmall() {
        string text = "";
        text += (elevation - position.z).ToString("0.####") + "\n";
        textSmall.text = text;
    }
    /// <summary>
    /// Gets the dbf fields
    /// </summary>
    private void Initialize() {
        fieldnames = remoteDataHandler.GetDBFFieldNames();   
    }

    // Update is called once per frame
    void Update()
    {
        //Make the sign rotate to the camera
        Vector3 direction = transform.position - cam.position;

        Quaternion rotation = Quaternion.LookRotation(direction);

        transform.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
    }


}
