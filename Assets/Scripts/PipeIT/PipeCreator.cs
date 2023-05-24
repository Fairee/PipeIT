using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeCreator : Singleton<PipeCreator>
{
    //to convert from WGS to UTM
    WGSConverter converter;
    //to get the data for the pipe
    RemoteDataHandler remoteDataHandler;
    //to get the material and whether the pipe should be visible or not
    PipesSettingsManager pipesSettingManager;
    //takes care of the mesh generation
    PipeMeshCreator pipeGenerator;
    //necesary for the anchors
    GeoSpatialManager geoSpatialManager;
    //to get the pipe radius
    SettingsManager settings;

    private float pipeRadius = 0.035f;

    /// <summary>
    /// Sets the radius that is used to create the pipes
    /// </summary>
    /// <param name="radius">Radius of the pipes</param>
    public void SetRadius(float radius) {
        pipeRadius = radius;
    }


    // Start is called before the first frame update
    void Start()
    {
        converter = new WGSConverter();
        remoteDataHandler = RemoteDataHandler.Instance;
        pipesSettingManager = PipesSettingsManager.Instance;
        pipeGenerator = PipeMeshCreator.Instance;
        settings = SettingsManager.Instance;
        geoSpatialManager = GeoSpatialManager.Instance;
        //get the pipe radius from the loaded settings
        pipeRadius = settings.GetPipeSize();
    }

    /// <summary>
    /// Creates the pipe in a given area and links it to the area
    /// </summary>
    /// <param name="id">ID of the pipe that should be created</param>
    /// <param name="area">The area that the pipe is being created for</param>
    /// <param name="makeItPlanar">Ignores the altitude of the pipes if true</param>
    /// <param name="currentAltitude">The altitude into which we want to spawn the pipe</param>
    /// <param name="offset">An offset to move the pipe by (for debbuging)</param>
    public void CreatePipe(int id, AreaManager.Area area, bool makeItPlanar, double currentAltitude, Vector3D offset = null)
    {
        if (offset == null) {
            offset = new Vector3D(0, 0, 0);
        }
        

        var datatest = remoteDataHandler.GetPipeDataByID(id);
        RemoteDataHandler.PipeData data;

        //Check whether the data exists
        if (datatest.HasValue)
        {
            data = datatest.Value;
        }
        //Right now we just ignore the pipe - might be better to do something else .. like request it to load
        else {
            return;
        }

        //Checks what parts of the pipe are inside the Area, if the pipe goes out and then back in it will create two pipes instead
        List<List<Vector3D>> segments = Pipe.AdjustPointsToFitInArea(data.pointsInWGS, area.bot, area.top, area.left, area.right);

        //create pipe for each of the segment
        for (int i = 0; i < segments.Count; i++)
        {

            //create new game object and set all its values
            GameObject pipeGameObject= new GameObject(id.ToString() + "/" + i.ToString());
            
            Pipe pipe = pipeGameObject.AddComponent<Pipe>();
            pipe.pointsInWGS = segments[i];
            pipe.dbfData = data.dbfData;
            pipe.id = id;
            pipe.pipeType = PipesSettingsManager.GetPipeType(pipe.dbfData[0]);
            pipe.subType = PipesSettingsManager.GetPipeSubType(pipe.dbfData[0]);
            pipeGameObject.transform.SetParent(area.area.transform);


            //Account for the offset (debugging)
            for (int j = 0; j < pipe.pointsInWGS.Count; j++)
            {
                pipe.pointsInWGS[j] -= offset;
            }


            List<Vector3D> pointsInPlanar = new List<Vector3D>();
            //Get the points from WGS to UTM
            foreach (Vector3D point in pipe.pointsInWGS)
            {
                if (makeItPlanar)
                {
                        pointsInPlanar.Add(converter.TransformUTM(point.x, point.y, 0));
                }
                else
                {
                        pointsInPlanar.Add(converter.TransformUTM(point.x, point.y, point.z));
                }
            }

            //Take the mid point between the first and last point -- it will be where the anchor is set
            Vector3D midPoint = pipe.pointsInWGS[0] + pipe.pointsInWGS[pipe.pointsInWGS.Count - 1];           
            midPoint = midPoint / 2;
            pipe.anchorPointInWGS = midPoint;


            //Get the anchor position in UTM
            Vector3D anchorPoint;
            anchorPoint = converter.TransformUTM(pipe.anchorPointInWGS.x, pipe.anchorPointInWGS.y, 0);

            
            //This will make the anchor be in the (0,0,0) of the mesh so that you don't need to set adjust it's position to fit into the real world
            for (int p = 0; p < pointsInPlanar.Count; p++)
            {
                pointsInPlanar[p] -= anchorPoint;
            }

            pipe.pointsInPlanar = pointsInPlanar;

            //convert points from double to float so they can be used for the mesh creation
            List<Vector3> pointsToRender = new List<Vector3>();
            foreach (Vector3D point in pointsInPlanar)
            {
                pointsToRender.Add(point.ToFloat());
            }

        
            //Retrive the appropriate material
            Material material = pipesSettingManager.GetMaterial(pipe.subType);



            //Generate the mesh of the pipe
            pipeGenerator.GeneratePipe(pipeGameObject, pointsToRender, material, pipeRadius);


            pipeGameObject.AddComponent<MeshCollider>();

            //create an anchor and tie it to the pipe - the anchors do not work in unity editor
#if !UNITY_EDITOR
            var anchor = geoSpatialManager.GetAnchor(pipe.anchorPointInWGS.x, pipe.anchorPointInWGS.y, currentAltitude);
            pipe.transform.SetParent(anchor.transform);
            anchor.transform.SetParent(area.area.transform);
#endif
            //check whether the pipe should be visible or not
            if (!pipesSettingManager.GetSubtypeSwitch(pipe.subType))
            {
                pipe.gameObject.SetActive(false);
            }
            if (!pipesSettingManager.GetTypeSwitch(pipe.pipeType))
            {
                pipe.gameObject.SetActive(false);
            }

            //assign the pipe to the area
            area.pipes.Add(pipe);

            //Left here if the terrain anchors ever become vaiable.. this would be used instead of the anchor creation before

            // ResolveAnchorOnTerrainPromise promise = geoSpatialManager.AnchorManager.ResolveAnchorOnTerrainAsync(pipe.pointsInWGS[0].y, pipe.pointsInWGS[0].x, 42.53, Quaternion.identity);
            // StartCoroutine(TerrainAnchoring(promise, pipeGameObject));
        }
    }


    /// <summary>
    /// Takes a pipe and reanchor it to new anchor based on the given altitude
    /// </summary>
    /// <param name="pipe">The pipe that will be reanchored</param>
    /// <param name="altitude">The altitude into which it will be anchored</param>
    public void ReAnchor(Pipe pipe, double altitude) {
        GameObject lastAnchor = pipe.transform.parent.gameObject;
        //reset the pipes to intial values
        pipe.transform.SetParent(null);
        pipe.transform.position = Vector3.zero;
        pipe.transform.rotation = Quaternion.identity;
        pipe.transform.localScale = Vector3.one;
        //create new anchor
        var anchor = geoSpatialManager.GetAnchor(pipe.anchorPointInWGS.x, pipe.anchorPointInWGS.y, altitude);
        pipe.transform.SetParent(anchor.transform);
        anchor.transform.SetParent(lastAnchor.transform.parent);
        //get rid of the old anchor
        Destroy(lastAnchor);
    }

    /// <summary>
    /// Regenerate the pipes geometry
    /// </summary>
    /// <param name="pipe">the pipe to be regenerated</param>
    public void ReCreate(Pipe pipe) {
        List<Vector3> pointsToRender = new List<Vector3>();
        //transform the points to float
        foreach (Vector3D point in pipe.pointsInPlanar)
        {
            pointsToRender.Add(point.ToFloat());
        }
        Material material = pipesSettingManager.GetMaterial(pipe.subType);
        //create the new mesh
        pipeGenerator.GeneratePipe(pipe.gameObject,pointsToRender, material, pipeRadius);
    
    }

}
