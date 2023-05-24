using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.XR.ARCoreExtensions;

public class AreaManager : Singleton<AreaManager>
{
    //Representation of the area
    public struct Area {
        public string name;
        public GameObject area;
        public List<Pipe> pipes;
        public double top, bot, left, right;
        public int zoneLatIndex, zoneLonIndex, areaLatIndex, areaLonIndex;
    }

    //boundaries of the current area
    private double currentAreaMinLon = 0, currentAreaMaxLon = 0, currentAreaMinLat = 0, currentAreaMaxLat = 0;

    //last position used to check whether we need to regenerate data
    Vector3D lastPosition;
    //the requested position of the last request data call
    Vector3D requestedPosition;

    //This sets how much the user have to move so that the we try to request new data
    public double distanceTreshold = 4;

    //for the stages in the update
    public bool accuracyReached = false;
    public bool accuracyCoroutineFinished = false;
    bool requestMade = false;
    bool requestFullfilled = false;
    public bool hasEnoughAccuracy = false;

    //To be able to retrieve data
    RemoteDataHandler remoteDataHandler;
    // To be able to retireve users current position
    GeoSpatialManager geoSpatialManager;
    // To be able to hide/show pipes base on the settings
    PipesSettingsManager pipesSettingManager;
    // To be able to retrieve the altitude at which we want to spawn the objects and the threshold used
    SettingsManager settingsManager;
    //After retrieving the header from the server, we can calculate our area indexes with this
    BinReader binReader = null;
    //To create pipes
    PipeCreator pipeCreator;

    //The max size of stored not active areas
    public int maxNotActiveSize = 10;

    //currently active areas (if not on the edge of prague this should be always 9)
    private Dictionary<string, Area> activeRegion;
    // the areas that were created but we moved out of them and are no longer being visualized, but we still keep them in the memory
    private Dictionary<string, Area> notActiveAreas;

    //Check for big altitude jumps
    public  double altitudeTolerance = 0.2;

    //The altitude at which we are generating the pipes for this moment
    private double currentAltitude = 0;



    //the altitude difference at which the pipes are spawning
    private float pipeAltitudeDistanceFromCamera = 1.3f;

    //this forces the lines to be in 2D .. makes us ignore their height
    private bool makeItPlanar = true;

    // our current position
    public Vector3D position;

    //for TESTING

    //For debbuging puproses, when we want to put the data from Prague to some other place
    //Left here so that if someone faces the same problem, they do not need to program it again
    public bool useOffset = false;
    // this makes the pipes appear at Celakovice (first value is Florence, second is place in Celakovice)
    // when applied everything is created with the offset
    Vector3D OffSet = new Vector3D(50.08954376 - 50.161053, 14.44197302 - 14.744601, 0);


    // Start is called before the first frame update
    void Start()
    {
        pipeCreator = PipeCreator.Instance;
        geoSpatialManager = GeoSpatialManager.Instance;
        remoteDataHandler = RemoteDataHandler.Instance;
        pipesSettingManager = PipesSettingsManager.Instance;
        settingsManager = SettingsManager.Instance;

        activeRegion = new Dictionary<string, Area>();
        notActiveAreas = new Dictionary<string, Area>();
        lastPosition = new Vector3D(0, 0, 0);

        //listnes we can start the visualization
        geoSpatialManager.AccuracyReached.AddListener(StartVizualizing);
        geoSpatialManager.AccuracyChanged.AddListener(AccuracyChanged);
        geoSpatialManager.AccuracyCoroutineFinished.AddListener(AccuracyCoroutineFinished);

        //listens if the data is ready to be loaded
        remoteDataHandler.InitializationDone.AddListener(SetBinReader);
        remoteDataHandler.RetrievalDone.AddListener(DataIsReady);

        //listen to any changes done in the pipes settings
        pipesSettingManager.PipeTypeChanged.AddListener(ManagePipes);
        pipesSettingManager.SubTypeChanged.AddListener(ManagePipes);
        pipesSettingManager.TypesResetMade.AddListener(ResetMade);

        //listen to any changes done in the app settings 
        settingsManager.PipeDistanceChanged.AddListener(ChangePipeDistance);
        settingsManager.AltitudeThresholdChanged.AddListener(ChangeAltitudeThreshold);
        settingsManager.PipeSizeChanged.AddListener(ChangePipeSize);

        //get the settings from the last time
        pipeAltitudeDistanceFromCamera = settingsManager.GetPipeDistance();
        altitudeTolerance = settingsManager.GetThreshold();

        //set the radius for the pipe generation
        pipeCreator.SetRadius(settingsManager.GetPipeSize());

        //sets the last position to zero so that the first round goes through the distance check
        lastPosition = new Vector3D(0, 0, 0);
    }



    private void AccuracyCoroutineFinished() {
        accuracyCoroutineFinished = true;
    }

    public void SwitchOffset() {
        Debug.Log("SWITCHED");
        useOffset = ! useOffset;
    }

    private void ChangePipeSize(float size) {
        pipeCreator.SetRadius(size);
        foreach (var area in activeRegion)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                pipeCreator.ReCreate(pipe);
            }
        }

        foreach (var area in notActiveAreas)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                pipeCreator.ReCreate(pipe);
            }
        }

    }

    private void ChangeAltitudeThreshold(float treshold) {
        altitudeTolerance = treshold;
    }

    private void ChangePipeDistance(float dist) {
        pipeAltitudeDistanceFromCamera = dist;
        ReAnchor();
    }

    private void AccuracyChanged(bool accuracy) {
        hasEnoughAccuracy = accuracy;
    }

    private void ResetMade() {
        foreach (var area in activeRegion)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                pipe.gameObject.SetActive(true);
            }
        }

        foreach (var area in notActiveAreas)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                pipe.gameObject.SetActive(true);
            }
        }
    }

    private void ManagePipes(PipesSettingsManager.PipeType type, bool state) {
        foreach (var area in activeRegion) {
            foreach (Pipe pipe in area.Value.pipes) {
                if (pipe.pipeType == type) {
                    pipe.gameObject.SetActive(state);
                }
            }
        }

        foreach (var area in notActiveAreas) {
            foreach (Pipe pipe in area.Value.pipes)
            {
                if (pipe.pipeType == type)
                {
                    pipe.gameObject.SetActive(state);
                }
            }
        }
    }

    private void ManagePipes(PipesSettingsManager.SubType type, bool state)
    {
        foreach (var area in activeRegion)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                if (pipe.subType == type)
                {
                    pipe.gameObject.SetActive(state);
                }
            }
        }

        foreach (var area in notActiveAreas)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                if (pipe.subType == type)
                {
                    pipe.gameObject.SetActive(state);
                }
            }
        }
    }



    void DataIsReady()
    {
        requestFullfilled = true;
    }

    void SetBinReader() {
        binReader = remoteDataHandler.GetBinReader();
       // accuracyReached = true;
    }

    void StartVizualizing() {
        accuracyReached = true;
    }

    void ReAnchor() {
        foreach (var area in activeRegion)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                pipeCreator.ReAnchor(pipe, currentAltitude - pipeAltitudeDistanceFromCamera);
            }
        }

        foreach (var area in notActiveAreas)
        {
            foreach (Pipe pipe in area.Value.pipes)
            {
                pipeCreator.ReAnchor(pipe, currentAltitude - pipeAltitudeDistanceFromCamera);
            }
        }


    }


    // Update is called once per frame
    void Update()
    {
        if (!accuracyReached || binReader == null || !hasEnoughAccuracy) {
            return;
        }
        //The unity editor can't use the Geospatial API so for debugging we set our position by hand
#if UNITY_EDITOR
        position = new Vector3D(50.08954376, 14.44197302, 0);
#else
        position = geoSpatialManager.GetCurrentPosition();
#endif

        double newAltitude = position.z;
        //CHECK IF ALTITUDE CHANGED TOO MUCH
        if (!(currentAltitude + altitudeTolerance > newAltitude && currentAltitude - altitudeTolerance < newAltitude)) {
            currentAltitude = newAltitude;
            ReAnchor();
        }

        //The offset is explained with the bool definition
        if (useOffset)
        {
            position += OffSet;
        }


        //Check distance from the last time we updated the areas
        //Prevents too much loading if person walks back and forward on the edge of an area
        double distance = WGSConverter.CalculateDistance(lastPosition.x, lastPosition.y, position.x, position.y);
        if (distance < distanceTreshold) {
            return;
        }
        //check if the position is within current area
        if (position.y < currentAreaMaxLon && position.y > currentAreaMinLon && position.x < currentAreaMaxLat && position.x > currentAreaMinLat) {
            lastPosition = position;

            return;
        }
        //check if we've already made a request
        if (!requestMade) {
            Debug.Log("Made Request");
            requestedPosition = position;
            //get true if the request was taken and it will be processed. Else keep trying to get the request through
            requestMade = remoteDataHandler.retrieveData(position.x, position.y);
            requestFullfilled = false;
        }
        //until the request is fulfilled we have to wait
        if (!requestFullfilled) {
            return;
        }
        //if the request went through and we have managed to calibrate the device
        if (accuracyCoroutineFinished)
        {
            //get IDS of requested position and current position -> if they are different I've moved too fast and the needed stuff isnt loaded!
            if (!CheckIfTwoPositionsAreInTheSameArea(position, requestedPosition))
            {
                requestMade = false;
                return;
            }

            //generate the new area and move there
            ChangeArea(position);
            requestMade = false;
        }
    }

    /// <summary>
    /// Checks whether two positions are withing the same Area
    /// </summary>
    /// <param name="first">the vecotr of the first position</param>
    /// <param name="second">the vector of the second poisiton</param>
    /// <returns></returns>
    private bool CheckIfTwoPositionsAreInTheSameArea(Vector3D first, Vector3D second) {
        int firstAreaLonIndex, firstAreaLatIndex, firstZoneLonIndex, firstZoneLatIndex;
        int secondAreaLonIndex, secondAreaLatIndex, secondZoneLonIndex, secondZoneLatIndex;
        binReader.GetIndexesByPosition(first.x, first.y, out firstZoneLatIndex, out firstZoneLonIndex, out firstAreaLatIndex, out firstAreaLonIndex);
        binReader.GetIndexesByPosition(second.x, second.y, out secondZoneLatIndex, out secondZoneLonIndex, out secondAreaLatIndex, out secondAreaLonIndex);

        if (firstAreaLatIndex != secondAreaLatIndex || firstAreaLonIndex != secondAreaLonIndex ||
            firstZoneLatIndex != secondZoneLatIndex || firstZoneLonIndex != secondZoneLonIndex) {
            return false;
        }
        return true;

    }

    /// <summary>
    /// Changes the area to a new one
    /// </summary>
    /// <param name="position">Our current position</param>
    private void ChangeArea(Vector3D position) {
        int newAreaLonIndex, newAreaLatIndex, newZoneLonIndex, newZoneLatIndex;
        binReader.GetIndexesByPosition(position.x, position.y, out newZoneLatIndex, out newZoneLonIndex, out newAreaLatIndex, out newAreaLonIndex);

        List<int[]> areasToLoad = new List<int[]>();
        //deactive the current active areas and add them to the notActiveAreas
        foreach (var member in activeRegion) {
            member.Value.area.SetActive(false);
            notActiveAreas[member.Key] = member.Value;
        }
        activeRegion.Clear();

        //Iterate over all the areas in the square around our area
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                //get the real indexes (takes care of when we are on the edge of one zone and another)
                int[] indexes = new int[4];
                if (!binReader.getValidIndex(newZoneLatIndex, newZoneLonIndex, newAreaLatIndex + x, newAreaLonIndex + y, out indexes[0], out indexes[1], out indexes[2], out indexes[3])) {
                    continue;
                }
                string name = BinReader.GetAreaName(indexes[0], indexes[1], indexes[2], indexes[3]);
                //checks whether the area is already created in the notActiveAreas - if yes just activate it and remove it from the notActiveAreas
                if (notActiveAreas.ContainsKey(name))
                {
                    activeRegion[name] = notActiveAreas[name];
                    activeRegion[name].area.SetActive(true);
                    notActiveAreas.Remove(name);
                }
                else
                {
                    //If no add it to the list of areas that have to be created
                    areasToLoad.Add(indexes);
                }
            }
        }
        //checks whether we haven't breached the limit of notActiveAreas, if yes, destroy them all
        if (notActiveAreas.Count > maxNotActiveSize) {
            List<Area> toDestroy = new List<Area>();
            foreach (var member in notActiveAreas) {
                toDestroy.Add(member.Value);
            }
            for (int i = toDestroy.Count - 1; i >= 0; i--) {
                Destroy(toDestroy[i].area);
            }
            notActiveAreas.Clear();
        }

        //go through the areas to load, and load them
        foreach (int[] indexes in areasToLoad)
        {
            string areaName = BinReader.GetAreaName(indexes[0], indexes[1], indexes[2], indexes[3]);
            Area? area = CreateArea(areaName, indexes[0], indexes[1], indexes[2], indexes[3]);
            if (area.HasValue)
            {
                activeRegion.Add(areaName, area.Value);
            }
        }

        string currentAreaName = BinReader.GetAreaName(newZoneLatIndex, newZoneLonIndex, newAreaLatIndex, newAreaLonIndex);
        //check whether the area we are supposed to move into exists
        //TODO generate the areas boundaries and set them no matter if the area is active (this will happen in just edge cases anyway)
        if (activeRegion.ContainsKey(currentAreaName))
        {
            Area currentArea = activeRegion[currentAreaName];
            currentAreaMaxLon = currentArea.top;
            currentAreaMinLon = currentArea.bot;
            currentAreaMaxLat = currentArea.right;
            currentAreaMinLat = currentArea.left;
        }
        else {
            currentAreaMaxLon = 0;
            currentAreaMinLon = 0;
            currentAreaMaxLat = 0;
            currentAreaMinLat = 0;
        }
        lastPosition = position;

    }

    /// <summary>
    /// Tries to create an area if possible
    /// </summary>
    /// <param name="name">the name of the area to be created</param>
    /// <param name="newZoneLatIndex">the zone latitude index of the area</param>
    /// <param name="newZoneLonIndex">the zone longitude index of the area</param>
    /// <param name="newAreaLatIndex">the area latitude index of the area</param>
    /// <param name="newAreaLonIndex">the area longitude index of the area</param>
    /// <returns>The area or null if failed</returns>
    Area? CreateArea(string name, int newZoneLatIndex, int newZoneLonIndex, int newAreaLatIndex, int newAreaLonIndex) {

        //get ids of pipes inside
        List<int> ids = remoteDataHandler.GetIdsForArea(name);
        //if the ids were not loaded by the remote Handler return null
        if (ids == null) {
            return null;
        }

        //get area boundary
        double bot, top, left, right;
        binReader.GetAreasFromAndToByIndex(newZoneLatIndex, newZoneLonIndex, newAreaLatIndex, newAreaLonIndex, out left, out bot, out right, out top);

        Area area = new Area();

        //fill area parameters
        area.pipes = new List<Pipe>();
        area.name = name;
        area.top = top; area.bot = bot; area.left = left; area.right = right;
        area.areaLatIndex = newAreaLatIndex; area.areaLonIndex = newAreaLonIndex;
        area.zoneLatIndex = newZoneLatIndex; area.zoneLonIndex = newZoneLonIndex;

        GameObject areaGameObject = new GameObject(name);
        area.area = areaGameObject;


        //Create the pipes for the area
        foreach (int id in ids)
        {
            if (useOffset)
            {
                pipeCreator.CreatePipe(id, area, makeItPlanar, currentAltitude - pipeAltitudeDistanceFromCamera, OffSet);
            }
            else {
                pipeCreator.CreatePipe(id, area, makeItPlanar, currentAltitude - pipeAltitudeDistanceFromCamera, new Vector3D(0, 0, 0));
            }
        }



        return area;
    }


    







    // USE THIS IF THE PHONE STARTS HAVING THE RIGHT ALTITUDE
    private IEnumerator TerrainAnchoring(ResolveAnchorOnTerrainPromise promise, GameObject pipe) {
        yield return promise;

        var result = promise.Result;
        if (result.TerrainAnchorState == TerrainAnchorState.Success && result.Anchor != null)
        {
            Transform parent = pipe.transform.parent;
            pipe.transform.SetParent(result.Anchor.transform);
            result.Anchor.transform.SetParent(parent);
            Debug.Log("Anchor created," + result.Anchor.name);
            
            Debug.Log(result.Anchor.pose.position.x + " " + result.Anchor.pose.position.y + " " + result.Anchor.pose.position.z);
        }
        else {
            Debug.Log("Anchoring Failed" + result.TerrainAnchorState);
        }


        yield break;
    
    }

}
