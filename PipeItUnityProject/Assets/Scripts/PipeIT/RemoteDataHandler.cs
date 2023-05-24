using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Text;
using UnityEngine.Events;


/// <summary>
/// Takes care of communication with the remote server
/// </summary>
public class RemoteDataHandler : Singleton<RemoteDataHandler>
{

    public enum RecieveDataState { NoDataIsBeingProcessed = 0, GettingIDsFromBin = 1, GettingDBSData = 2, GettingSHPData = 3, AllDataRecieved = 4 }

    struct AreaToLoad {
        public int areaLonIndex, areaLatIndex, zoneLonIndex, zoneLatIndex;
    }

    //here just for deserialization
    public class IdsModel
    {
        public int[] Ids;
    }


    //Data recieved for a pipe
    public struct PipeData {
        public int id;
        public List<string> dbfData;
        public List<Vector3D> pointsInWGS;
    }

    //To know when to start the initialization
    GeoSpatialManager geoManager;
    //the latitude and longitude used for the current request
    [SerializeField]
    double currentLatitude = 50.088549124, currentLongitude = 14.441936928;

    //update controls
    bool initializationComplete = false;
    [SerializeField]
    bool startInitializtaion = false;
    bool preprocessingDone = false;
    bool isTherePendingRequest = false;


    //the url to the server
    [SerializeField]
    string requestURL = "http://35.240.23.46:81/api/PipeItServer/";

    //Server comunication variables
    UnityWebRequest lastRequest;
    AsyncOperation asyncOp;
    DateTime requestStartTime;
    TimeSpan requestTooLongWarningTimeout = TimeSpan.FromSeconds(2);
    public RecieveDataState state;

    //max amounts of given types to store in memory
    [SerializeField]
    int maxAmountOfPipes = 2000;
    [SerializeField]
    int maxAmountOfAreas = 4000;

    //the loaded data
    Dictionary<int, PipeData> loadedPipes = new Dictionary<int, PipeData>();
    Dictionary<string, List<int>> loadedAreas = new Dictionary<string, List<int>>();
    string[] dbfFieldNames;
    //Creates the binReader for the recieved bin header
    BinReader binReader;

    //used during the loading process
    List<AreaToLoad> areasToLoad = new List<AreaToLoad>();
    List<int> currentIds = new List<int>();
    List<List<Vector3D>> shpData = new List<List<Vector3D>>();
    string[][] dbfData;

    //Byte counters for debbuging purposes
    public long byteCounterSend = 0;
    public long byteCounterRecieved = 0;

    /// <summary>
    /// The dbf field names and bin reader header were succesfully retrieved
    /// </summary>
    public UnityEvent InitializationDone;
    /// <summary>
    /// The last retrieval request was accomplished
    /// </summary>
    public UnityEvent RetrievalDone;
    /// <summary>
    /// The retireval ran out of time
    /// </summary>
    public UnityEvent<string> TimeRunOut;

    /// <summary>
    /// Gets the list of ids for a given area
    /// </summary>
    /// <param name="name">name of the area</param>
    /// <returns>List of ideas</returns>
    public List<int> GetIdsForArea(string name) {
        if (loadedAreas.ContainsKey(name))
        {
            return loadedAreas[name];
        }
        return null;
    }
    /// <summary>
    /// Gets the Pipe data for a given pipe
    /// </summary>
    /// <param name="num">Pipe id</param>
    /// <returns>data for the pipe</returns>
    public PipeData? GetPipeDataByID(int num) {
        if (loadedPipes.ContainsKey(num))
        {
            return loadedPipes[num];
        }
        return null;
    }
    
    /// <summary>
    /// Gets the bin reader
    /// </summary>
    /// <returns>the bin reader with the loaded header</returns>
    public BinReader GetBinReader() {
        return binReader;
    }

    /// <summary>
    /// Gets the dbf field names
    /// </summary>
    /// <returns>list of the dbf field names</returns>
    public string[] GetDBFFieldNames()
    {
        return dbfFieldNames;
    }


    private void Start()
    {
        geoManager = GeoSpatialManager.Instance;
        byteCounterSend = 0;
        byteCounterRecieved = 0;
        geoManager.InitializationDone.AddListener(StartInit);
    }

    /// <summary>
    /// Start the process of retrieving dbf fields and bin header
    /// </summary>
    private void StartInit() {
        startInitializtaion = true;
    }


    /// <summary>
    /// Initializate a new data retrievel for given position
    /// </summary>
    /// <param name="latitude">the latitude of the position</param>
    /// <param name="longitude">the longitude of the postion</param>
    /// <returns>true if the retireval was initiated</returns>
    public bool retrieveData(double latitude, double longitude) {
        if (state == RecieveDataState.NoDataIsBeingProcessed)
        {
            currentLatitude = latitude;
            currentLongitude = longitude;
            StateChange(RecieveDataState.GettingIDsFromBin);
            return true;
        }
        else {
            return false;
        }
    }



    private void Update()
    {
        //Check wheter we already retireved the binheader and dbf fields
        if (!initializationComplete) {
            if (startInitializtaion)
            {
                if (binReader == null)
                {
                    GetBinHeader();
                }
                else {
                    GetDBFFields();
                }
                if (binReader != null && dbfFieldNames.Length > 0)
                {
                    InitializationDone.Invoke();
                }
            }
            return;
        }

        //see in which state of retrievel we currently are
        switch (state) {
            case RecieveDataState.NoDataIsBeingProcessed:
                return;
            case RecieveDataState.GettingDBSData:
                GetDBFData();
                break;
            case RecieveDataState.GettingIDsFromBin:
                GetIds();
                break;
            case RecieveDataState.GettingSHPData:
                GetSHPData();
                break;
            case RecieveDataState.AllDataRecieved:
                CreatePipesDataFromGatheredData();
                break;
        }
    }


    /// <summary>
    /// Make a request to the server for the bin header
    /// </summary>
    private void GetBinHeader() {
        if (!isTherePendingRequest)
        {
            lastRequest = UnityWebRequest.Get(requestURL + "BinHeader");
            asyncOp = lastRequest.SendWebRequest();
            isTherePendingRequest = true;
            requestStartTime = DateTime.Now;
        }
        if (!asyncOp.isDone) {
            //check if its taking too long
            if (DateTime.Now - requestStartTime > requestTooLongWarningTimeout)
            {
                TimeRunOut.Invoke("The BinHeader is taking way too much time.");
            }
            return;
        }

        if (lastRequest.result == UnityWebRequest.Result.Success)
        {
            byte[] text = lastRequest.downloadHandler.data;

            byteCounterRecieved += lastRequest.downloadHandler.nativeData.Length;
            byteCounterSend += System.Text.Encoding.UTF8.GetByteCount(lastRequest.url);
            //parse the data
            string json = System.Text.Encoding.UTF8.GetString(text);
            BinReader.BinHeader binHeader = JsonConvert.DeserializeObject<BinReader.BinHeader>(json);
            binReader = new BinReader(binHeader);
            Debug.Log("Bin header recieved succesfully");
        }
        else
        {
            TimeRunOut.Invoke("The request to obtain the BinHeader failed with an error: " + lastRequest.error);
            Debug.Log(lastRequest.error);
        }
        isTherePendingRequest = false;
    }
    /// <summary>
    /// Make a request for the DBF fields
    /// </summary>
    private void GetDBFFields() {
        if (!isTherePendingRequest)
        {
            lastRequest = UnityWebRequest.Get(requestURL + "DBFfieldNames");
            asyncOp = lastRequest.SendWebRequest();
            isTherePendingRequest = true;
            requestStartTime = DateTime.Now;
        }
        if (!asyncOp.isDone)
        {
            if (DateTime.Now - requestStartTime > requestTooLongWarningTimeout)
            {
                TimeRunOut.Invoke("The DBFfieldNames is taking way too much time.");
            }
            return;
        }

        if (lastRequest.result == UnityWebRequest.Result.Success)
        {
            byte[] text = lastRequest.downloadHandler.data;
            string json = System.Text.Encoding.UTF8.GetString(text);
            byteCounterRecieved += lastRequest.downloadHandler.nativeData.Length;
            byteCounterSend += System.Text.Encoding.UTF8.GetByteCount(lastRequest.url);
            //parse the data
            dbfFieldNames = JsonConvert.DeserializeObject<string[]>(json);
            Debug.Log("DBFfieldNames recieved succesfully");
        }
        else
        {
            TimeRunOut.Invoke("The request to obtain the DBFfieldNames failed with an error: " + lastRequest.error);
            Debug.Log(lastRequest.error);
        }
        //the intialization has been completed, there is no more request pending and no data is being processed
        isTherePendingRequest = false;
        initializationComplete = true;
        StateChange(RecieveDataState.NoDataIsBeingProcessed);
    }

    /// <summary>
    /// Changes the current date retrieval state
    /// </summary>
    /// <param name="newState">the state to change to</param>
    private void StateChange(RecieveDataState newState) {
        state = newState;
        //if we are done with our last retrieval, notify the world about it
        if (newState == RecieveDataState.NoDataIsBeingProcessed) {
            RetrievalDone.Invoke();
        }
    
    }

    /// <summary>
    /// Retrieves the list of ids from the server
    /// </summary>
    private void GetIds() {
        if (!preprocessingDone)
        {
            //check if we are out of our memory limits
            if (loadedAreas.Count > maxAmountOfAreas) {
                loadedAreas.Clear();
            }
            if (loadedPipes.Count > maxAmountOfPipes) {
                loadedPipes.Clear();
            }

            currentIds.Clear();
            int zoneLon, zoneLat, areaLon, areaLat;
            int maxAreaIndexLon = binReader.GetAmountOfAreasLon() - 1;
            int maxAreaIndexLat = binReader.GetAmountOfAreasLat() - 1;
            int maxZoneIndexLon = binReader.GetAmountOfZonesLon() - 1;
            int maxZoneIndexLat = binReader.GetAmountOfZonesLat() - 1;

            if (!binReader.GetIndexesByPosition(currentLatitude, currentLongitude, out zoneLat, out zoneLon, out areaLat, out areaLon))
            {
                StateChange(RecieveDataState.NoDataIsBeingProcessed);
                return;
            }


            for (int x = areaLon - 1; x <= areaLon + 1; x++)
            {
                for (int y = areaLat - 1; y <= areaLat + 1; y++)
                {
                    FillAreasToLoad(zoneLat, zoneLon, y, x);
                }
            }
            


            //First check if the Area wasn't recently loaded and thus is in the memory
            currentIds = CheckIfAreasAreLoaded(areasToLoad);
            preprocessingDone = true;
        }

        //One after one retrieve data about all of the areas
        if (!isTherePendingRequest && areasToLoad.Count > 0) {
            AreaToLoad area = areasToLoad[areasToLoad.Count - 1];
            string url = requestURL + "GetIds?zoneLatIndex=" + area.zoneLatIndex + "&zoneLonIndex=" + area.zoneLonIndex + "&areaLatIndex=" + area.areaLatIndex + "&areaLonIndex=" + area.areaLonIndex;
            lastRequest = UnityWebRequest.Get(url);
            asyncOp = lastRequest.SendWebRequest();
            requestStartTime = DateTime.Now;
            isTherePendingRequest = true;
        }
        if (!asyncOp.isDone)
        {
            if (DateTime.Now - requestStartTime > requestTooLongWarningTimeout)
            {
                TimeRunOut.Invoke("The request to obtain the ids from BinFile is taking a lot of time.");
            }
            return;
        }

        if (lastRequest.result == UnityWebRequest.Result.Success)
        {
            AreaToLoad area = areasToLoad[areasToLoad.Count - 1];
            areasToLoad.RemoveAt(areasToLoad.Count - 1);
            Debug.Log("RecievedIncreased");
            byteCounterRecieved += lastRequest.downloadHandler.nativeData.Length;
            byteCounterSend += System.Text.Encoding.UTF8.GetByteCount(lastRequest.url);
            byte[] text = lastRequest.downloadHandler.data;
            string json = System.Text.Encoding.UTF8.GetString(text);
            List<int> ids = JsonConvert.DeserializeObject<int[]>(json).ToList();
            string areaName = BinReader.GetAreaName(area.zoneLatIndex, area.zoneLonIndex, area.areaLatIndex, area.areaLonIndex);
            loadedAreas[areaName] = ids;
            AddIds(ids, currentIds);
        }
        else
        {
            TimeRunOut.Invoke("The request to obtain the ids from BinFile failed with error: " + lastRequest.error);
        }
        isTherePendingRequest = false;
        //when all areas are loaded, go to next step
        if (areasToLoad.Count == 0) {
            currentIds.Sort();
            StateChange(RecieveDataState.GettingDBSData);
            preprocessingDone = false;
            Debug.Log("Ids are loaded");
        }
    }

    /// <summary>
    /// Given an index of an area, check if it exists and add it to the  areas to load
    /// </summary>
    /// <param name="thisZoneLonIndex">the area zone longitude index</param>
    /// <param name="thisZoneLatIndex">the area zone latitude index</param>
    /// <param name="thisAreaLonIndex">the area area longitude index</param>
    /// <param name="thisAreaLatIndex">the area area latitude index</param>
    private void FillAreasToLoad(int thisZoneLatIndex, int thisZoneLonIndex,  int thisAreaLatIndex, int thisAreaLonIndex) {
        if (binReader.getValidIndex(thisZoneLatIndex, thisZoneLonIndex, thisAreaLatIndex, thisAreaLonIndex, out thisZoneLatIndex, out thisZoneLonIndex, out thisAreaLatIndex, out thisAreaLonIndex))
        {
            AreaToLoad area = new AreaToLoad();
            area.areaLonIndex = thisAreaLonIndex;
            area.areaLatIndex = thisAreaLatIndex;
            area.zoneLonIndex = thisZoneLonIndex;
            area.zoneLatIndex = thisZoneLatIndex;
            areasToLoad.Add(area);
        }
    }

    /// <summary>
    /// Checks whether  area is already loaded in the memory and remove them from the list
    /// </summary>
    /// <param name="areasToLoad">List of areas to be loaded</param>
    /// <returns>List of ids from the memory for the loaded areas</returns>
    private List<int> CheckIfAreasAreLoaded(List<AreaToLoad> areasToLoad)
    {
        List<int> ids = new List<int>();
        List<int> toBeAdded = new List<int>();
        for (int i = areasToLoad.Count - 1; i >=0; i--)
        {
            AreaToLoad area = areasToLoad[i];
            string areaName = BinReader.GetAreaName( area.zoneLatIndex, area.zoneLonIndex,  area.areaLatIndex,area.areaLonIndex);
            if (loadedAreas.ContainsKey(areaName))
            {
                //if the area is loaded remove it from the list to load
                areasToLoad.RemoveAt(i);
                toBeAdded = loadedAreas[areaName];
                AddIds(toBeAdded, ids);
            }
        }

        return ids;

    }

    /// <summary>
    /// Goes through ids and finds out whether they have to be loaded or not
    /// </summary>
    /// <param name="toBeAdded">list of all the requeired ids</param>
    /// <param name="ids">list of the ids that have to be loaded</param>
    private void AddIds(List<int> toBeAdded, List<int> ids) {
        foreach (int id in toBeAdded)
        {
            //if the pipe is already generated in memory
            if (CheckIfPipeGenerated(id))
            {
                continue;
            }
            //if the pipe isn't in memory check if it isnt on the list to be generated and add it or incease its number by one
            else if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

    }
    /// <summary>
    /// Checks if the pipe with the given id is already loaded
    /// </summary>
    /// <param name="id">id of the pipe</param>
    /// <returns>true if its loaded </returns>
    private bool CheckIfPipeGenerated(int id)
    {
        if (loadedPipes.ContainsKey(id))
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// Gets DBF Data from remote server based on the current ids list
    /// </summary>
    private void GetDBFData() {
        if (!isTherePendingRequest) {
            string url = requestURL + "DBFPostData";

            IdsModel model = new IdsModel
            {
                Ids = currentIds.ToArray()
            };
            //prepares the data for the body of the request 
            string json = JsonConvert.SerializeObject(model);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            byteCounterSend += bodyRaw.Length;
            byteCounterSend += System.Text.Encoding.UTF8.GetByteCount(lastRequest.url);

            UnityWebRequest uwr = UnityWebRequest.Post(url, "");
            //puts the data into the body of the request 
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.SetRequestHeader("Content-Type", "application/json");

            lastRequest = uwr;
            asyncOp = lastRequest.SendWebRequest();
            requestStartTime = DateTime.Now;
            isTherePendingRequest = true;
        }
        if (!asyncOp.isDone)
        {
            if (DateTime.Now - requestStartTime > requestTooLongWarningTimeout)
            {
                TimeRunOut.Invoke("The request to obtain the ids from BinFile is taking a lot of time.");
            }
            return;
        }

        if (lastRequest.result == UnityWebRequest.Result.Success)
        {
            lastRequest.uploadHandler.Dispose();
            byte[] text = lastRequest.downloadHandler.data;
           
            Debug.Log("RecievedIncreased");
            byteCounterRecieved += lastRequest.downloadHandler.nativeData.Length;
            //deserializes the data
            string json = System.Text.Encoding.UTF8.GetString(text);
            dbfData = JsonConvert.DeserializeObject<string[][]>(json);
        }
        else
        {
            lastRequest.uploadHandler.Dispose();
            TimeRunOut.Invoke("The request to obtain the ids from BinFile failed with error: " + lastRequest.error);
        }
        StateChange(RecieveDataState.GettingSHPData);
        preprocessingDone = false;
        isTherePendingRequest = false;
        Debug.Log("Dbf is loaded");
    }

    /// <summary>
    /// Retrieves SHP data from remote server (based on the currentId list)
    /// </summary>
    private void GetSHPData() {
        if (!isTherePendingRequest)
        { 
            string url = requestURL + "SHPPostData";

            IdsModel model = new IdsModel
            {
                Ids = currentIds.ToArray()
            };
            //prepares the data for the body of the request 
            string json = JsonConvert.SerializeObject(model);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            byteCounterSend += bodyRaw.Length;
            byteCounterSend += System.Text.Encoding.UTF8.GetByteCount(lastRequest.url);

            UnityWebRequest uwr = UnityWebRequest.Post(url, "");
            //put the data into the body
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.SetRequestHeader("Content-Type", "application/json");

            lastRequest = uwr;
            asyncOp = lastRequest.SendWebRequest();
            
            requestStartTime = DateTime.Now;
            isTherePendingRequest = true;
        }
        if (!asyncOp.isDone)
        {
            if (DateTime.Now - requestStartTime > requestTooLongWarningTimeout)
            {
                TimeRunOut.Invoke("The request to obtain the ids from DBFFile is taking a lot of time.");
            }
            return;
        }

        if (lastRequest.result == UnityWebRequest.Result.Success)
        {
            lastRequest.uploadHandler.Dispose();

            byte[] text = lastRequest.downloadHandler.data;
            string json = System.Text.Encoding.UTF8.GetString(text);
            double[][] shp = JsonConvert.DeserializeObject<double[][]>(json);
            Debug.Log("RecievedIncreased");
            byteCounterRecieved += lastRequest.downloadHandler.nativeData.Length;
            //deserialize recieved data and make the Vectors from it
            foreach (double[] data in shp) {
                List<Vector3D> list = new List<Vector3D>();
                for (int i = 0; i < data.Length / 3; i++) {
                    list.Add(new Vector3D(data[i * 3 + 1], data[i * 3], data[i * 3 + 2]));
                }
                shpData.Add(list);
            }

        }
        else
        {
            lastRequest.uploadHandler.Dispose();
            TimeRunOut.Invoke("The request to obtain the ids from SHPFile failed with error: " + lastRequest.error);
        }

        //All done, ready to move to other stage
        StateChange(RecieveDataState.AllDataRecieved);
        preprocessingDone = false;
        isTherePendingRequest = false;
        Debug.Log("SHP is loaded");
    }


    /// <summary>
    /// Takes all of the recieved data and puts them into the PipeData structure
    /// </summary>
    private void CreatePipesDataFromGatheredData() {
        for (int i = 0; i < currentIds.Count; i++) {
            PipeData pipe = new PipeData();
            pipe.pointsInWGS = shpData[i];
            pipe.dbfData = dbfData[i].ToList();
            pipe.id = currentIds[i];
            loadedPipes[pipe.id] = pipe;
            dbfData[i] = null;
        }
        currentIds.Clear();
        shpData.Clear();
        dbfData = null;
        StateChange(RecieveDataState.NoDataIsBeingProcessed);
    }

}
