using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//This code was taken from https://github.com/federicocasares/unity-plumber and slightly adjusted to fit the wanted function
//Unneccessary functions were deleted and some of them were rewritten.
public class PipeMeshCreator : Singleton<PipeMeshCreator> 
{
    //Unique for each pipe
    private List<Vector3> points;
    private Material pipeMaterial;

    //Same for all of the pipes
    private float pipeRadius = 0.035f;
    private float elbowRadius = 0.035f;
    [Range(4, 16)]
    private int pipeSegments = 8;
    [Range(2, 40)]
    private float elbowAngle = 10;
    
    private float colinearThreshold = 0.001f;


    /// <summary>
    /// Changes the radius for the to be generated pipes
    /// </summary>
    /// <param name="pipeRadius">the radius to be used</param>
    private void SetPipeRadius(float pipeRadius) {
        this.pipeRadius = pipeRadius;
        this.elbowRadius = pipeRadius;
    }
    /// <summary>
    /// Creates a mesh for a pipe based on the points
    /// </summary>
    /// <param name="pipe">The pipe for which the mesh is being generated</param>
    /// <param name="points">The points which are used to generate the mesh</param>
    /// <param name="material">The material used for the mesh</param>
    /// <param name="radius">The radius used to generate the mesh</param>
    public void GeneratePipe(GameObject pipe,List<Vector3> points, Material material, float radius)
    {
        SetPipeRadius(radius);
        this.points = points;
        this.pipeMaterial = material;
        if (points.Count < 2)
        {
            throw new System.Exception("Cannot render a pipe with fewer than 2 points");
        }

        RemoveColinearPoints();

        MeshFilter meshFilter = pipe.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
          meshFilter = pipe.AddComponent<MeshFilter>();
        }
        MeshRenderer meshRenderer = pipe.GetComponent<MeshRenderer>();
        if (meshRenderer == null) { 
            meshRenderer  = pipe.AddComponent<MeshRenderer>();
        }

        Mesh mesh = GenerateMesh();


        meshFilter.mesh = mesh;

        meshRenderer.materials = new Material[1] { pipeMaterial };
    }
    /// <summary>
    /// Generates the mesh 
    /// </summary>
    /// <returns></returns>
    Mesh GenerateMesh()
    {
        Mesh m = new Mesh();
        m.name = "Pipe";
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // for each segment, generate a cylinder
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 initialPoint = points[i];
            Vector3 endPoint = points[i + 1];
            Vector3 direction = (points[i + 1] - points[i]).normalized;

            if (i > 0 )
            {
                // leave space for the elbow that will connect to the previous
                // segment, except on the very first segment
                initialPoint = initialPoint + direction * elbowRadius;
            }

            if (i < points.Count - 2 )
            {
                // leave space for the elbow that will connect to the next
                // segment, except on the last segment
                endPoint = endPoint - direction * elbowRadius;
            }

            // generate two circles with "pipeSegments" sides each and then
            // connect them to make the cylinder
            GenerateCircleAtPoint(vertices, normals, initialPoint, direction);
            GenerateCircleAtPoint(vertices, normals, endPoint, direction);
            MakeCylinderTriangles(triangles, i);
        }

        // for each segment generate the elbow that connects it to the next one

        GenerateElbow(vertices, normals, triangles);
        

        GenerateEndCaps(vertices, triangles, normals);
        

        m.SetVertices(vertices);
        m.SetTriangles(triangles, 0);
        m.SetNormals(normals);
        return m;
    }
    /// <summary>
    /// Removes points that are colinear
    /// </summary>
    private void RemoveColinearPoints() {
        for (int i = points.Count - 2; i > 0; i--) {
            Vector3 direction1 = points[i] - points[i-1];
            Vector3 direction2 = points[i + 1] - points[i];
            if (Vector3.Distance(direction1.normalized, direction2.normalized) < colinearThreshold) {
                points.RemoveAt(i);
            }
        }
    }


    /// <summary>
    /// Generates new vertices alonga circle around a given point and adds the vertices and their normals into provided lists
    /// </summary>
    /// <param name="vertices">A list of vertices of the whole mesh into which new vertices will be added</param>
    /// <param name="normals">A list of normals of the whole mesh into which new normals will be added</param>
    /// <param name="center">The center point of the circle</param>
    /// <param name="direction">the normal to the plane that contains the circle</param>
    void GenerateCircleAtPoint(List<Vector3> vertices, List<Vector3> normals, Vector3 center, Vector3 direction)
    {
 
        float twoPi = Mathf.PI * 2;
        float radiansPerSegment = twoPi / pipeSegments;

        // generate two axes that define the plane with normal 'direction' to have the normals right
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        Vector3 xAxis = Vector3.up;
        Vector3 yAxis = Vector3.right;
        if (plane.GetSide(direction))
        {
            yAxis = Vector3.left;
        }

        // build left-hand coordinate system, with orthogonal and normalized axes
        Vector3.OrthoNormalize(ref direction, ref xAxis, ref yAxis);

        for (int i = 0; i < pipeSegments; i++)
        {
            Vector3 currentVertex = center + (pipeRadius * Mathf.Cos(radiansPerSegment * i) * xAxis) +
                (pipeRadius * Mathf.Sin(radiansPerSegment * i) * yAxis);
            vertices.Add(currentVertex);
            normals.Add((currentVertex - center).normalized);
        }
    }
    /// <summary>
    /// Generates triangles and adds them to the list
    /// </summary>
    /// <param name="triangles">the list for the triangles</param>
    /// <param name="segmentIdx">the index of the segment</param>
    void MakeCylinderTriangles(List<int> triangles, int segmentIdx)
    {
        // connect the two circles corresponding to segment segmentIdx of the pipe
        int offset = segmentIdx * pipeSegments * 2;
        for (int i = 0; i < pipeSegments; i++)
        {
            triangles.Add(offset + (i + 1) % pipeSegments);
            triangles.Add(offset + i + pipeSegments);
            triangles.Add(offset + i);

            triangles.Add(offset + (i + 1) % pipeSegments);
            triangles.Add(offset + (i + 1) % pipeSegments + pipeSegments);
            triangles.Add(offset + i + pipeSegments);
        }
    }
    /// <summary>
    /// Generate triangles for the elbows
    /// </summary>
    /// <param name="vertices">A list of vertices of the whole mesh into which new vertices will be added </param>
    /// <param name="triangles">A list of triangles coresponding to the vertices</param>
    /// <param name="segmentIdx">index of the sexment</param>
    /// <param name="totalElbow">the total amount of segments on the elbow</param>
    void MakeElbowTriangles(List<Vector3> vertices, List<int> triangles, int segmentIdx, int totalElbow)
    {
        // connect the two circles corresponding to segment segmentIdx of an
        // elbow with index elbowIdx
        int offset = (points.Count - 1) * pipeSegments * 2; // all vertices of cylinders
        offset += totalElbow; // all vertices of previous elbows
        offset += segmentIdx * pipeSegments; // the current segment of the current elbow

        // algorithm to avoid elbows strangling under dramatic
        // direction changes... we basically map vertices to the
        // one closest in the previous segment
        Dictionary<int, int> mapping = new Dictionary<int, int>();
            List<Vector3> thisRingVertices = new List<Vector3>();
            List<Vector3> lastRingVertices = new List<Vector3>();

            for (int i = 0; i < pipeSegments; i++)
            {
                lastRingVertices.Add(vertices[offset + i - pipeSegments]);
            }

            for (int i = 0; i < pipeSegments; i++)
            {
                // find the closest one for each vertex of the previous segment
                Vector3 minDistVertex = Vector3.zero;
                float minDist = Mathf.Infinity;
                for (int j = 0; j < pipeSegments; j++)
                {
                    Vector3 currentVertex = vertices[offset + j];
                    float distance = Vector3.Distance(lastRingVertices[i], currentVertex);
                    if (distance < minDist)
                    {
                        minDist = distance;
                        minDistVertex = currentVertex;
                    }
                }
                thisRingVertices.Add(minDistVertex);
                mapping.Add(i, vertices.IndexOf(minDistVertex));
            }
        // build triangles for the elbow segment
        for (int i = 0; i < pipeSegments; i++)
        {
            triangles.Add(mapping[i]);
            triangles.Add(offset + i - pipeSegments);
            triangles.Add(mapping[(i + 1) % pipeSegments]);

            triangles.Add(offset + i - pipeSegments);
            triangles.Add(offset + (i + 1) % pipeSegments - pipeSegments);
            triangles.Add(mapping[(i + 1) % pipeSegments]);
        }
    }
    /// <summary>
    /// Generates an elbow of the pipe
    /// </summary>
    /// <param name="vertices">A list of vertices of the whole mesh into which new vertices will be added</param>
    /// <param name="normals">A list of normals of the whole mesh into which new normals will be added</param>
    /// <param name="triangles">A list of the triangles for the mesh</param>
    void GenerateElbow(List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        int totalSegments = 0;
        for (int p = 0; p < points.Count - 2; p++)
        {
            Vector3 point1 = points[p]; // starting point
            Vector3 point2 = points[p + 1]; // the point around which the elbow will be built
            Vector3 point3 = points[p + 2]; // next point




            // generates the elbow around the area of point2, connecting the cylinders
            // corresponding to the segments point1-point2 and point2-point3
            Vector3 offset1 = (point2 - point1).normalized ;
            Vector3 offset2 = (point3 - point2).normalized ;
            Vector3 startPoint = point2 - offset1 * elbowRadius;
            Vector3 endPoint = point2 + offset2 * elbowRadius;

            // auxiliary vectors to calculate lines parallel to the edge of each
            // cylinder, so the point where they meet can be the center of the elbow
            Vector3 perpendicularToBoth = Vector3.Cross(offset1, offset2);
            Vector3 startDir = Vector3.Cross(perpendicularToBoth, offset1).normalized;
            Vector3 endDir = Vector3.Cross(perpendicularToBoth, offset2).normalized;

            // calculate torus arc center as the place where two lines projecting
            // from the edges of each cylinder intersect
            Vector3 torusCenter1;
            Vector3 torusCenter2;
            ClosestPointsOnTwoLines(out torusCenter1, out torusCenter2, startPoint, startDir, endPoint, endDir);
            Vector3 torusCenter = 0.5f * (torusCenter1 + torusCenter2);

            // calculate actual torus radius based on the calculated center of the 
            // torus and the point where the arc starts
            float actualTorusRadius = (torusCenter - startPoint).magnitude;

            float angle = Vector3.Angle(startPoint - torusCenter, endPoint - torusCenter);
            int elbowSegments = Mathf.FloorToInt(angle / elbowAngle) + 1;
            float radiansPerSegment = (angle * Mathf.Deg2Rad) / elbowSegments;
            Vector3 lastPoint = point2 - startPoint;


            for (int i = 0; i <= elbowSegments; i++)
            {
                // create a coordinate system to build the circular arc
                // for the torus segments center positions
                Vector3 xAxis = (startPoint - torusCenter).normalized;
                Vector3 yAxis = (endPoint - torusCenter).normalized;
                Vector3.OrthoNormalize(ref xAxis, ref yAxis);

                Vector3 circleCenter = torusCenter +
                    (actualTorusRadius * Mathf.Cos(radiansPerSegment * i) * xAxis) +
                    (actualTorusRadius * Mathf.Sin(radiansPerSegment * i) * yAxis);

                Vector3 direction = circleCenter - lastPoint;
                lastPoint = circleCenter;

                if (i == elbowSegments)
                {
                    // last segment should always have the same orientation
                    // as the next segment of the pipe
                    direction = endPoint - point2;
                }
                else if (i == 0)
                {
                    // first segment should always have the same orientation
                    // as the how the previous segmented ended
                    direction = point2 - startPoint;
                }

                GenerateCircleAtPoint(vertices, normals, circleCenter, direction);

                if (i > 0)
                {
                    MakeElbowTriangles(vertices, triangles, i, totalSegments);

                }
            }
            totalSegments += (elbowSegments + 1) * pipeSegments;
        }
    }
    /// <summary>
    /// Generates the ends of the pipe
    /// </summary>
    /// <param name="vertices">A list of vertices of the whole mesh into which new vertices will be added</param>
    /// <param name="normals">A list of normals of the whole mesh into which new normals will be added</param>
    /// <param name="triangles">A list of the triangles for the mesh</param>
    void GenerateEndCaps(List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        // create the circular cap on each end of the pipe
        int firstCircleOffset = 0;
        int secondCircleOffset = (points.Count - 1) * pipeSegments * 2 - pipeSegments;

        vertices.Add(points[0]); // center of first segment cap
        int firstCircleCenter = vertices.Count - 1;
        normals.Add(points[0] - points[1]);

        vertices.Add(points[points.Count - 1]); // center of end segment cap
        int secondCircleCenter = vertices.Count - 1;
        normals.Add(points[points.Count - 1] - points[points.Count - 2]);

        for (int i = 0; i < pipeSegments; i++)
        {
            triangles.Add(firstCircleCenter);
            triangles.Add(firstCircleOffset + (i + 1) % pipeSegments);
            triangles.Add(firstCircleOffset + i);

            triangles.Add(secondCircleOffset + i);
            triangles.Add(secondCircleOffset + (i + 1) % pipeSegments);
            triangles.Add(secondCircleCenter);
        }
    }

    /// <summary>
    /// This function finds points that are closest to oneanother. If the lines are not parallel, the function 
    /// outputs true, otherwise false.
    /// </summary>
    /// <param name="closestPointLine1">the closest point on the first line</param>
    /// <param name="closestPointLine2">the closest point on the second line</param>
    /// <param name="linePoint1">a point on the first line</param>
    /// <param name="lineVec1">a direciton vector of the first line</param>
    /// <param name="linePoint2">a point on the second line</param>
    /// <param name="lineVec2">a direction vector of the second line</param>
    /// <returns></returns>
    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        Vector3 lineSeparation = linePoint2 - linePoint1;
        Vector3 crossProduct = Vector3.Cross(lineVec1, lineVec2);
        float denominator = crossProduct.sqrMagnitude;
        //check if the lines arent parallel
        if (denominator == 0) {
            return false;
        }
        float line1Parameter = Vector3.Dot(Vector3.Cross(lineSeparation, lineVec2), crossProduct) / denominator;
        float line2Parameter = Vector3.Dot(Vector3.Cross(lineSeparation, lineVec1), crossProduct) / denominator;
        closestPointLine1 = linePoint1 + (lineVec1 * line1Parameter);
        closestPointLine2 = linePoint2 + (lineVec2 * line2Parameter);

        return true;
    }

}