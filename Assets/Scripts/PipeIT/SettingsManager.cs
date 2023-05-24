using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARSubsystems;

public class SettingsManager : Singleton<SettingsManager>
{
    struct Settings
    {
        public float pipeSize;
        public float pipeDistanceFromCamera;
        public float latitudeThreshold;
        public bool occlusion;
        public int occlusionType; //0 = fast, 1 = medium, 2 = best
        public int lightingSetting; // 0 = none, 1 = brightness
        public int signType; // 0 = big, 1 = false;
    }


    Settings settings;
    /// <summary>
    /// The pipe size has changed
    /// </summary>
    public UnityEvent<float> PipeSizeChanged;
    /// <summary>
    /// The pipe distance has changed 
    /// </summary>
    public UnityEvent<float> PipeDistanceChanged;
    /// <summary>
    /// the altitude threshold has changed
    /// </summary>
    public UnityEvent<float> AltitudeThresholdChanged;
    /// <summary>
    /// The occlusion state has changed (on/off)
    /// </summary>
    public UnityEvent<bool> OcclusionChanged;
    /// <summary>
    /// The type of the sign has changed (big/small)
    /// </summary>
    public UnityEvent<int> SignTypeChanged;
    /// <summary>
    /// The Lighting model has changed 
    /// </summary>
    public UnityEvent<LightEstimationMode> LightingChanged;
    /// <summary>
    /// The occlusion type has changed 
    /// </summary>
    public UnityEvent<EnvironmentDepthMode, bool> OcclusionTypeChanged;

    // Start is called before the first frame update
    void Awake()
    {

        settings = new Settings();
        settings.pipeSize = 0.035f;
        settings.pipeDistanceFromCamera = 1.3f;
        settings.latitudeThreshold= 0.3f;
        settings.occlusion = false;
        settings.occlusionType = 0;
        settings.lightingSetting = 0;
        settings.signType = 0;

        //loads the data if they exist
        if (PlayerPrefs.HasKey("Settings"))
        {
            string serialized = PlayerPrefs.GetString("Settings");
            settings = JsonConvert.DeserializeObject<Settings>(serialized);
        }

        CheckSettings();
    }

    /// <summary>
    /// Checks if the data was loaded, if not set default values
    /// </summary>
    private void CheckSettings()
    {
        if (settings.pipeSize.Equals(default(float)))
            settings.pipeSize = 0.035f;
        if (settings.pipeDistanceFromCamera.Equals(default(float)))
            settings.pipeDistanceFromCamera = 1.3f;
        if (settings.latitudeThreshold.Equals(default(float)))
            settings.latitudeThreshold = 0.3f;
    }

    /// <summary>
    /// Set the settings and save it based on the recieve values
    /// </summary>
    /// <param name="pipesize">the size of the pipes</param>
    /// <param name="pipeDistance">the distance of the pipe from the camera (altitude-wise)</param>
    /// <param name="threshold">the reanchoring threshold</param>
    /// <param name="occlusionType">the type of occlusion</param>
    /// <param name="lightingType">the type of lighting setting</param>
    public void SetSettingsValues(float pipesize = -1, float pipeDistance = -1, float threshold = -1, int occlusionType = -1, int lightingType = -1) {
        if (pipesize != -1 && settings.pipeSize != pipesize) {
            settings.pipeSize = pipesize;
            PipeSizeChanged.Invoke(pipesize);
        }
        if (pipeDistance != -1 && settings.pipeDistanceFromCamera != pipeDistance) {
            settings.pipeDistanceFromCamera = pipeDistance;
            PipeDistanceChanged.Invoke(pipeDistance);
        }
        if (threshold != -1 && settings.latitudeThreshold != threshold) {
            settings.latitudeThreshold = threshold;
            AltitudeThresholdChanged.Invoke(threshold);
        }
        if (occlusionType != -1 && settings.occlusionType != occlusionType) {
            settings.occlusionType = occlusionType;

            OcclusionTypeChanged.Invoke(GetOcclusionMode(), settings.occlusion);
            
        }
        if (lightingType != -1 && settings.lightingSetting != lightingType) {
            settings.lightingSetting = lightingType;
            LightingChanged.Invoke(GetLightingMode());
        }

        string serialized = JsonConvert.SerializeObject(settings);
        PlayerPrefs.SetString("Settings", serialized);
    }

    /// <summary>
    /// Turns the occlusion on and off and saves it for the next time
    /// </summary>
    /// <param name="switcher">state of the occlusion</param>
    public void SetOcclusionValue(bool switcher) {
        if (settings.occlusion != switcher) {
            settings.occlusion = switcher;
            OcclusionChanged.Invoke(switcher);
        }

        string serialized = JsonConvert.SerializeObject(settings);
        PlayerPrefs.SetString("Settings", serialized);

    }

    /// <summary>
    /// Sets the type of the signs
    /// </summary>
    /// <param name="type">what type should the sign be 0 = big, 1 = small</param>
    public void SetSignType(int type) {
        if (settings.signType != type) {
            settings.signType = type;
            SignTypeChanged.Invoke(type);
        }

        string serialized = JsonConvert.SerializeObject(settings);
        PlayerPrefs.SetString("Settings", serialized);
    }
    
    //---------------Getters-----------------------------------

    public EnvironmentDepthMode GetOcclusionMode() {
        switch (settings.occlusionType) {
            case 0:
                return EnvironmentDepthMode.Fastest;
            case 2:
                return EnvironmentDepthMode.Best;
            default:
                return EnvironmentDepthMode.Medium;
        }
    }

    public LightEstimationMode GetLightingMode() {
        switch (settings.lightingSetting) {
            case 1:
                return LightEstimationMode.AmbientIntensity;
            default:
                return LightEstimationMode.Disabled;
        }
    }
    public int GetLightingModeInt()
    {
        return settings.lightingSetting;
    }

    public int GetOcclusionModeInt() {
        return settings.occlusionType;
    }

    public float GetPipeSize() {
        return settings.pipeSize;
    }

    public float GetPipeDistance() {
        return settings.pipeDistanceFromCamera;
    }

    public float GetThreshold() {
        return settings.latitudeThreshold;
    }

    public bool GetOcclusionSwitch() {
        return settings.occlusion;
    }

    public bool IsOcclusitonSupported() {
        return GeoSpatialManager.Instance.GetOcclusionSupported();
    }

    public int GetSignType() {
        return settings.signType;
    }

}
