using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using UnityEngine.Android;
using System.Collections;

public class GeoSpatialManager : Singleton<GeoSpatialManager>
{

    public enum ErrorState { Null = 0, NoError = 1, Tracking = 2, Message = 4, Camera = 8, Location = 16, Time = 32, LostLocation = 64}

    [Header("[ AR Components]")]
    public ARSessionOrigin sessionOrigin;
    public ARSession session;
    public ARAnchorManager anchorManager;
    public AREarthManager earthManager;
    public ARCoreExtensions aRCoreExtensions;
    public AROcclusionManager aROcclusionManager;
    public ARCameraManager aRCameraManager;

    private ErrorState errorState = ErrorState.Null;

    /// <summary>
    /// Something happened in the Geospatial loop
    /// </summary>
    public UnityEvent<ErrorState, string> ErrorStateChanged;
    /// <summary>
    /// We have good/bad accuracy
    /// </summary>
    public UnityEvent<bool> AccuracyChanged;
    /// <summary>
    /// We finished the initialization of the core Geospatial features
    /// </summary>
    public UnityEvent InitializationDone;
    /// <summary>
    /// We reached enough accuracy to start the visualization
    /// </summary>
    public UnityEvent AccuracyReached;
    /// <summary>
    /// Information about the VPS status
    /// </summary>
    public UnityEvent<VpsAvailability> AvaibalityCheck;
    /// <summary>
    /// The accuracy was held for the whole second
    /// </summary>
    public UnityEvent AccuracyCoroutineFinished;

    //Variables for the update loop
    private bool cameraPermissionRequested = false;
    private bool locationPermissionRequested = false;
    private bool appStarted = false;
    private bool runable = true;
    private bool ranInitialization = false;
    private bool newlyEnabledGeospatialAPI = true;

    //Variables for the accuracy checking
    private bool initAccuracyReached = false;
    private Coroutine accuracyTimer;
    private float accuracyHoldTime = 1f;
    private bool accuracyCoroutinefinished = false;

    [Header("[Accuracy]")]
    //Minimal intial and runtime accuracies
    [SerializeField]
    private float initYawAccuracy =1.5f;
    [SerializeField]
    private float initVerticalAccuracy = 1f;
    [SerializeField]
    private float initHorizontalAccuracy =  1f;
    [SerializeField]
    private float minYawAccuracy = 3f;
    [SerializeField]
    private float minVerticalAccuracy = 1.5f;
    [SerializeField]
    private float minHorizontalAccuracy = 1.5f;

    //max time during which the geospatial api has to intialize or we will throw error
    private float intializeTime = 2f;

    //the last state we had
    private ARSessionState lastState = ARSessionState.None;

    //To retrieve data about occlusion culling and light estimation
    private SettingsManager settings;


    // Start is called before the first frame update
    void Start()
    {
        if (sessionOrigin == null || session == null || aRCoreExtensions == null) {
            runable = false;
        }
        settings = SettingsManager.Instance;
        settings.OcclusionTypeChanged.AddListener(OcclusionModeChanged);
        settings.OcclusionChanged.AddListener(turnOcclusionManagerOnOff);
        settings.LightingChanged.AddListener(LightingModeChanged);
    }

    // Update is called once per frame
    void Update()
    {

        if (!runable) {
            return;
        }
        if (!CheckCameraPermission()) {
            return;
        }
        if (!CheckLocationPermission()) {
            return;
        }
        if (!appStarted) {
            sessionOrigin.gameObject.SetActive(true);
            session.gameObject.SetActive(true);
            aRCoreExtensions.gameObject.SetActive(true);
            appStarted = true;
        }
        CheckInput();
        CheckSessionState();
        if (ARSession.state != ARSessionState.SessionInitializing && ARSession.state != ARSessionState.SessionTracking) {
            return;
        }

        FeatureSupported featureSupported = earthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);

        switch (featureSupported) {
            case FeatureSupported.Unsupported:
                SetErrorState(ErrorState.Message, "Geospatial API is not supported on this device! Sorry.");
                enabled = false;
                return;
            case FeatureSupported.Unknown:
                SetErrorState(ErrorState.Message, "Geospatial API has an unknown error. Sorry.");
                return;
            case FeatureSupported.Supported:
                if (aRCoreExtensions.ARCoreExtensionsConfig.GeospatialMode == GeospatialMode.Disabled) {
                    aRCoreExtensions.ARCoreExtensionsConfig.GeospatialMode = GeospatialMode.Enabled;
                    newlyEnabledGeospatialAPI = true;
                    return;
                }
                break;
        }


        //If the geospatialAPI was currently turned on, give it some time to load
        if (newlyEnabledGeospatialAPI) {
            intializeTime -= Time.deltaTime;
            if (intializeTime < 0)
            {
                newlyEnabledGeospatialAPI = false;
            }
            else {
                return;
            }
        }

        EarthState earthState = earthManager.EarthState;
        if (earthState != EarthState.Enabled) {
            SetErrorState(ErrorState.Message, "Error: Geospatial AR didn't start. Sorry. " + earthState);
            enabled = false;
            return;
        }

        if (!ranInitialization) {
            Initialize();
            StartCoroutine(VPSCheck());
        }

        CheckTracking();
       
    }
    /// <summary>
    /// Every second check the VPS status and invoke an update
    /// </summary>
    /// <returns></returns>
    private IEnumerator VPSCheck() {
        while (true) {
            GeospatialPose pose = earthManager.CameraGeospatialPose;
            VpsAvailabilityPromise promise = AREarthManager.CheckVpsAvailabilityAsync(pose.Latitude, pose.Longitude);
            while (promise.State == PromiseState.Pending) {
                yield return null;
            }
            if (promise.State == PromiseState.Done)
            {
                AvaibalityCheck.Invoke(promise.Result);
            }
            yield return new WaitForSeconds(1);
        } 
    }
    /// <summary>
    /// Start a timer during which we can't fall below the necessary accuracy
    /// </summary>
    /// <param name="time">the amount of time</param>
    /// <returns></returns>
    private IEnumerator AccuracyTimer(float time) {
        yield return new WaitForSecondsRealtime(time);
        if (errorState != ErrorState.NoError)
        {
            Debug.Log("Invoke DONE");
            SetErrorState(ErrorState.NoError);
            AccuracyCoroutineFinished.Invoke();
            AccuracyChanged.Invoke(true);
            
            accuracyCoroutinefinished = true;
            initAccuracyReached = true;
        }
    }
    /// <summary>
    /// Checks how is our accuracy
    /// </summary>
    /// <returns></returns>
    private bool CheckTracking() {

        GeospatialPose pose = earthManager.CameraGeospatialPose;
        //check whether we are in the calibration phase or the run time phase
        if (!initAccuracyReached)
        {
            if (pose.OrientationYawAccuracy < initYawAccuracy && pose.HorizontalAccuracy < initHorizontalAccuracy && pose.VerticalAccuracy < initVerticalAccuracy && pose.VerticalAccuracy != 0)
            {
                //if we are good start the coroutine
                if (accuracyTimer == null) {
                    AccuracyReached.Invoke();
                    accuracyTimer = StartCoroutine(AccuracyTimer(accuracyHoldTime));
                }
                return true;
            }
            //if we lose the accuracy stop the coroutine
            if (errorState == ErrorState.NoError)
            {
                StopCoroutine(accuracyTimer);
                accuracyTimer = null;
                SetErrorState(ErrorState.Tracking);
                return false;
            }
        }
        else if(accuracyCoroutinefinished) {
            //check whether or not our accuracy is within the limit and send singla when the status changes
            if (pose.OrientationYawAccuracy < minYawAccuracy && pose.HorizontalAccuracy < minHorizontalAccuracy && pose.VerticalAccuracy < minVerticalAccuracy && pose.VerticalAccuracy != 0)
            {
                if (errorState != ErrorState.NoError)
                {
                    AccuracyChanged.Invoke(true);
                    SetErrorState(ErrorState.NoError);
                }
                return true;
            }

            else if (errorState == ErrorState.NoError)
            {
                AccuracyChanged.Invoke(false);
                SetErrorState(ErrorState.LostLocation);
                return false;
            }

        }
        //if the earth manager stops tracking send a signal that we stopped tracking
        if (earthManager.EarthTrackingState != TrackingState.Tracking)
        {
            if (errorState == ErrorState.NoError)
            {
                StopCoroutine(accuracyTimer);
                accuracyTimer = null;
                SetErrorState(ErrorState.Tracking);
            }
            return false;
        }

        return false;
    }

    /// <summary>
    /// Initialize the core ARCore features
    /// </summary>
    private void Initialize()
    {
        ranInitialization = true;
        Debug.Log("START:" + settings.GetOcclusionSwitch());
        OcclusionModeChanged(settings.GetOcclusionMode(), settings.GetOcclusionSwitch());
        LightingModeChanged(settings.GetLightingMode());
        SetErrorState(ErrorState.Tracking);
        InitializationDone.Invoke();
    }
    /// <summary>
    /// Checks if the user quits the application
    /// </summary>
    private void CheckInput() {
        if (Input.GetKeyUp(KeyCode.Escape)) {
            Application.Quit();
        }
    }
    /// <summary>
    /// Check whether the the Geospatial API works
    /// </summary>
    private void CheckSessionState() {
        if (lastState == ARSession.state) {
            return;
        }

        if (ARSession.state == ARSessionState.CheckingAvailability ||
            ARSession.state == ARSessionState.Ready ||
            ARSession.state == ARSessionState.SessionInitializing ||
            ARSession.state == ARSessionState.SessionTracking)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            return;
        }

        SetErrorState(ErrorState.Message, "ARSession state error: " + ARSession.state);
        enabled = false;
    }
    /// <summary>
    /// Sets the error
    /// </summary>
    /// <param name="error">the type of error</param>
    /// <param name="message">the message connected to the error</param>
    public void SetErrorState(ErrorState error, string message = null) {
        if (errorState != error) {
            errorState = error;
            ErrorStateChanged.Invoke(error, message);
            Debug.Log("THis error state:" + error);
        }

    }

    /// <summary>
    /// Check if we have camera permissions
    /// </summary>
    /// <returns>true if yes</returns>
    private bool CheckCameraPermission() {
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            if (errorState == ErrorState.Camera)
            {
                SetErrorState(ErrorState.NoError);
            }
            return true;
        }

        if (errorState != ErrorState.Camera) {
            SetErrorState(ErrorState.Camera);
        }

        if (!cameraPermissionRequested) {
            Permission.RequestUserPermission(Permission.Camera);
            cameraPermissionRequested = true;
        }
        return false;
    }
    /// <summary>
    /// Check if we have location permisions
    /// </summary>
    /// <returns>true if yes </returns>
    private bool CheckLocationPermission() {
        if (Permission.HasUserAuthorizedPermission(Permission.FineLocation)) {
            if (errorState == ErrorState.Location) {
                SetErrorState(ErrorState.NoError);
            }
            return true;
        }
        if (errorState != ErrorState.Location) {
            SetErrorState(ErrorState.Location);
        }
        if (!locationPermissionRequested) {
            Permission.RequestUserPermission(Permission.FineLocation);
            locationPermissionRequested = true;
        }
        return false;

    }
    /// <summary>
    /// Gets our current position
    /// </summary>
    /// <returns>our current position</returns>
    public Vector3D GetCurrentPosition() {
        GeospatialPose pose = earthManager.CameraGeospatialPose;
        return new Vector3D(pose.Latitude, pose.Longitude, pose.Altitude);
    }

    /// <summary>
    /// Get out current pose
    /// </summary>
    /// <returns>our current Geospatial pose</returns>
    public GeospatialPose GetPose() {
        return earthManager.CameraGeospatialPose;
    }

    /// <summary>
    /// gets the calibration accuracy limits
    /// </summary>
    /// <returns>the calibration accuracy limits</returns>
    public (double horizontalWanted, double verticalWanted, double yawWanted) GetInitValues() {
        return (initHorizontalAccuracy, initVerticalAccuracy, initYawAccuracy);
    }

    /// <summary>
    /// gets the runtime accuracy limits
    /// </summary>
    /// <returns>the runtime accuracy limits</returns>
    public (double horizontalWanted, double verticalWanted, double yawWanted) GetMinValues()
    {
        return (minHorizontalAccuracy, minVerticalAccuracy, minYawAccuracy);
    }
    /// <summary>
    /// Creates a new anchor
    /// </summary>
    /// <param name="lat">latitude of the anchor</param>
    /// <param name="lon">longitude of the anchor</param>
    /// <param name="altitude">altitude of the anchor</param>
    /// <returns></returns>
    public ARGeospatialAnchor GetAnchor(double lat, double lon, double altitude) {
        GeospatialPose pose = earthManager.CameraGeospatialPose;
        var anchor = anchorManager.AddAnchor(lat, lon, altitude, Quaternion.identity);
        return anchor;
    }

    /// <summary>
    /// Change the state of the occlusion manager
    /// </summary>
    /// <param name="switcher">false to turn off, true to turn on</param>
    private void turnOcclusionManagerOnOff(bool switcher) {
        var descriptor = aROcclusionManager.descriptor?.environmentDepthImageSupported;
        if (descriptor.HasValue && descriptor.Value == Supported.Supported)
        {
            if (switcher == true)
            {
                aROcclusionManager.requestedEnvironmentDepthMode = settings.GetOcclusionMode();
            }        
            else {
                aROcclusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
            }
        }
    }
    /// <summary>
    /// Changes the mode of the occlusion culling
    /// </summary>
    /// <param name="mode">the mode we want to switch to</param>
    /// <param name="switcher">if the occlusion culling is on or off</param>
    private void OcclusionModeChanged(EnvironmentDepthMode mode, bool switcher) {
        Debug.Log("Occlusion culling was set to: " + switcher);
        var descriptor = aROcclusionManager.descriptor?.environmentDepthImageSupported;
        if (descriptor.HasValue && descriptor.Value == Supported.Supported)
        {
            if (!switcher)
            {
                aROcclusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
            }
            else
            {
                aROcclusionManager.requestedEnvironmentDepthMode = mode;
            }
        }
        Debug.Log(descriptor);
    }

    /// <summary>
    /// Checks wheter the device supports occlusion culling
    /// </summary>
    /// <returns>true if it does</returns>
    public bool GetOcclusionSupported() {
        var descriptor = aROcclusionManager.descriptor?.environmentDepthImageSupported;
        if (descriptor.HasValue && descriptor.Value == Supported.Supported)
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// Change the lightEstimation mode
    /// </summary>
    /// <param name="mode">the mode to change to</param>
    private void LightingModeChanged(LightEstimationMode mode) {        
        var m = mode.ToLightEstimation();
        if (mode == LightEstimationMode.EnvironmentalHDR) {
            m |= UnityEngine.XR.ARFoundation.LightEstimation.AmbientIntensity;
            m |= UnityEngine.XR.ARFoundation.LightEstimation.AmbientColor;
        }
        aRCameraManager.requestedLightEstimation = m;
    }

    /// <summary>
    /// Gets current rotation
    /// </summary>
    /// <returns>current location</returns>
    public Quaternion GetRotation() {
        return earthManager.CameraGeospatialPose.EunRotation;
    }

}
