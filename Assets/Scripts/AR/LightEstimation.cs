using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;


//This script is taken from https://github.com/Unity-Technologies/arfoundation-samples/blob/main/Assets/Scripts/Runtime/HDRLightEstimation.cs 
//and is very much changed.
//offical ArFoundation examples

/// <summary>
/// A component that can be used to access the most recently received HDR light estimation information
/// for the physical environment as observed by an AR device.
/// </summary>
    public class LightEstimation : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events containing light estimation information.")]
        ARCameraManager m_CameraManager;
        Color baseColor;
        /// <summary>
        /// Get or set the <c>ARCameraManager</c>.
        /// </summary>
        public ARCameraManager cameraManager
        {
            get { return m_CameraManager; }
            set
            {
                if (m_CameraManager == value)
                    return;

                if (m_CameraManager != null)
                    m_CameraManager.frameReceived -= FrameChanged;

                m_CameraManager = value;

                if (m_CameraManager != null & enabled)
                    m_CameraManager.frameReceived += FrameChanged;
            }
        }

        /// <summary>
        /// The estimated brightness of the physical environment, if available.
        /// </summary>
        public float? brightness { get; private set; }


        void Awake()
        {
         baseColor = RenderSettings.ambientLight;
        }

        void OnEnable()
        {
            if (m_CameraManager != null)
                m_CameraManager.frameReceived += FrameChanged;
        }

        void OnDisable()
        {
            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= FrameChanged;
        }



    /// <summary>
    /// Takes the estimated values and uses them
    /// </summary>
    /// <param name="args">estimated values</param>
    void FrameChanged(ARCameraFrameEventArgs args)
    {
        if (args.lightEstimation.averageBrightness.HasValue)
        {

            brightness = args.lightEstimation.averageBrightness.Value;
            RenderSettings.ambientLight = baseColor * brightness.Value;
        }
        else
        {

            RenderSettings.ambientLight = baseColor;

            brightness = null;
        }
        
    }
}
