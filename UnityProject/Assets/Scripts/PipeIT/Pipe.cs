using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe :MonoBehaviour
{
    public int id;
    public PipesSettingsManager.PipeType pipeType;
    public PipesSettingsManager.SubType subType;
    public List<string> dbfData;
    public List<Vector3D> pointsInWGS;
    public List<Vector3D> pointsInPlanar;
    public int currentlyUsing = 0;
    public Vector3D firstPointInUTM;
    public Vector3D anchorPointInWGS;



    /// <summary>
    /// Takes a pipe and returns segments that are going through the given area
    /// </summary>
    /// <param name="pointsInWGS">Points of the pipe in WGS</param>
    /// <param name="bottom">bottom boundary of the area in WGS</param>
    /// <param name="top">top boundary of the area in WGS</param>
    /// <param name="left">left boundary of the area in WGS</param>
    /// <param name="right">right boundary of the area in WGS</param>
    /// <returns></returns>
    public static List<List<Vector3D>> AdjustPointsToFitInArea(List<Vector3D> pointsInWGS, double bottom, double top, double left, double right) {
        List<Vector3D> points = new List<Vector3D>();
        List<int> segments = new List<int>();
        for (int i = 0; i < pointsInWGS.Count - 1; i++) {
            //get rid of those segments that are clearly out of the area
            if (pointsInWGS[i].x < left && pointsInWGS[i + 1].x < left)
            {
                continue;
            }
            else if (pointsInWGS[i].x > right && pointsInWGS[i + 1].x > right) {
                continue;
            }
            else if (pointsInWGS[i].y < bottom && pointsInWGS[i + 1].y < bottom)
            {
                continue;
            }
            else if (pointsInWGS[i].y > top && pointsInWGS[i + 1].y > top)
            {
                continue;
            }
            //find intersections with the boundaries
            double[] intersection = new double[8];
            (intersection[0], intersection[1]) = GetLineIntersection(pointsInWGS[i].x, pointsInWGS[i].y, pointsInWGS[i + 1].x, pointsInWGS[i + 1].y, left, bottom, left, top);
            (intersection[2], intersection[3]) = GetLineIntersection(pointsInWGS[i].x, pointsInWGS[i].y, pointsInWGS[i + 1].x, pointsInWGS[i + 1].y, left, top, right, top);
            (intersection[4], intersection[5]) = GetLineIntersection(pointsInWGS[i].x, pointsInWGS[i].y, pointsInWGS[i + 1].x, pointsInWGS[i + 1].y, right, top, right, bottom);
            (intersection[6], intersection[7]) = GetLineIntersection(pointsInWGS[i].x, pointsInWGS[i].y, pointsInWGS[i + 1].x, pointsInWGS[i + 1].y, right, bottom, left, bottom);

            List<int> validIntersections = new List<int>();
            //finds how many intersection were found and which of them were found
            if (!double.IsNaN(intersection[0])) {
                validIntersections.Add(0);
            }
            if (!double.IsNaN(intersection[2])) {
                validIntersections.Add(1);
            }
            if (!double.IsNaN(intersection[4])) {
                validIntersections.Add(2);
            }
            if (!double.IsNaN(intersection[6])) {
                validIntersections.Add(3);
            }

            //One point is inside the area
            if (validIntersections.Count == 1)
            {
                //first point is inside the area -> first the point then intersection -> leaving an area, finish segment
                if (bottom < pointsInWGS[i].y && top > pointsInWGS[i].y && left < pointsInWGS[i].x && right > pointsInWGS[i].x)
                {
                    //if this is the first point to be added, then it means that it was an endpoint and thus we should add both
                    if (points.Count == 0)
                    {
                        points.Add(pointsInWGS[i]);
                    }
                    double x = intersection[validIntersections[0] * 2];
                    double y = intersection[validIntersections[0] * 2 + 1];
                    points.Add(new Vector3D(x, y, GetInterpolatedAltitude(x, y, pointsInWGS[i], pointsInWGS[i + 1])));
                    segments.Add(points.Count);
                }
                //second point is inside the area -> first the intersection then point
                else
                {
                    double x = intersection[validIntersections[0] * 2];
                    double y = intersection[validIntersections[0] * 2 + 1];
                    points.Add(new Vector3D(x, y, GetInterpolatedAltitude(x, y,pointsInWGS[i], pointsInWGS[i + 1])));
                    points.Add(pointsInWGS[i + 1]);

                }
            }
            //the line goes through the whole area -> its one whole segment
            else if (validIntersections.Count == 2)
            {
                double x = intersection[validIntersections[0] * 2];
                double y = intersection[validIntersections[0] * 2 + 1];
                points.Add(new Vector3D(x, y, GetInterpolatedAltitude(x, y, pointsInWGS[i], pointsInWGS[i + 1])));
                x = intersection[validIntersections[1] * 2];
                y = intersection[validIntersections[1] * 2 + 1];
                points.Add(new Vector3D(x, y, GetInterpolatedAltitude(x, y, pointsInWGS[i], pointsInWGS[i + 1])));
                segments.Add(points.Count);
            }
            //In rare cases, its possible that there will be more validIntersection (the line goes through a corner)
            //based on the order of the intersection test take first and third valid intersection!
            else if (validIntersections.Count > 2)
            {
                double x = intersection[validIntersections[0] * 2];
                double y = intersection[validIntersections[0] * 2 + 1];
                points.Add(new Vector3D(x, y, GetInterpolatedAltitude(x, y, pointsInWGS[i], pointsInWGS[i + 1])));
                x = intersection[validIntersections[2] * 2];
                y = intersection[validIntersections[2] * 2 + 1];
                points.Add(new Vector3D(x, y, GetInterpolatedAltitude(x, y, pointsInWGS[i], pointsInWGS[i + 1])));
                segments.Add(points.Count);
            }
            //both points are either outside or inside
            else {
                //if one point is inside, then the second is inside too
                if (bottom < pointsInWGS[i].y && top > pointsInWGS[i].y && left < pointsInWGS[i].x && right > pointsInWGS[i].x)
                {
                    //if this is the first point to be added, then it means that it was an endpoint and thus we should add both
                    if (points.Count == 0)
                    {
                        points.Add(pointsInWGS[i]);
                    }
                    points.Add(pointsInWGS[i + 1]);
                }
            }
        }
        pointsInWGS = points;
        segments.Add(pointsInWGS.Count);
        return Split(pointsInWGS, segments);
    }
    /// <summary>
    /// Gets intersection between two lines
    /// </summary>
    /// <param name="firstPointX">The X coordinate of the first point of the first line</param>
    /// <param name="firstPointY">The Y coordinate of the first point of the first line</param>
    /// <param name="secondPointX">The X coordinate of the second point of the first line</param>
    /// <param name="secondPointY">The Y coordinate of the second point of the first line</param>
    /// <param name="thirdPointX">The X coordinate of the first point of the second line</param>
    /// <param name="thirdPointY"> The Y coordinate of the first point of the second line </param>
    /// <param name="fourthPointX">The X coordinate of the second point of the second line </param>
    /// <param name="fourthPointY">The Y coordinate of the second point of the second line </param>
    /// <returns></returns>
    private static (double x, double y) GetLineIntersection(double firstPointX, double firstPointY, double secondPointX, double secondPointY, double thirdPointX, double thirdPointY, double fourthPointX, double fourthPointY) {
        double firstLineX, firstLineY, secondLineX, secondLineY;
        firstLineX = secondPointX - firstPointX;
        firstLineY = secondPointY - firstPointY;
        secondLineX = fourthPointX - thirdPointX;
        secondLineY = fourthPointY - thirdPointY;

        double denominator = firstLineX * secondLineY - firstLineY * secondLineX;


        if (denominator == 0)
        {
            // The line segments are parallel or coincident
            return (double.NaN, double.NaN);
        }
        //find parameter
        double t1 = ((thirdPointX - firstPointX) * secondLineY - (thirdPointY - firstPointY) * secondLineX) / denominator;
        double t2 = ((thirdPointX - firstPointX) * firstLineY - (thirdPointY - firstPointY) * firstLineX) / denominator;

        //check if the point is between the ending points
        if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
        {
            double intersectionX = firstPointX + t1 * firstLineX;
            double intersectionY = firstPointY + t1 * firstLineY;
            return (intersectionX, intersectionY);
        }

        return (double.NaN, double.NaN);
    }
    /// <summary>
    ///  Find the altitude for the new point by interpolation
    /// </summary>
    /// <param name="x">x value of the new point</param>
    /// <param name="y">y value of the new point</param>
    /// <param name="startPoint">one of the points on the line</param>
    /// <param name="endPoint">second of the points on the line</param>
    /// <returns></returns>
    static double GetInterpolatedAltitude(double x, double y, Vector3D startPoint, Vector3D endPoint)
    {
        double t = (x - startPoint.x) / (endPoint.x - startPoint.x);
        //if the x values are the same, try interpolating with y value
        if (endPoint.x - startPoint.x == 0) {
            t = (y - startPoint.y) / (endPoint.y - startPoint.y);
        }
        double z = startPoint.z + t * (endPoint.z - startPoint.z);
        return z;
    }
    /// <summary>
    /// Splits the points into segemnets 
    /// </summary>
    /// <param name="pointsInWGS">A list of the points</param>
    /// <param name="segments">A list of indexes where to make a cut</param>
    /// <returns></returns>
    private static List<List<Vector3D>> Split(List<Vector3D>pointsInWGS, List<int> segments) {
        List<List<Vector3D>> ret = new List<List<Vector3D>>();
        List<Vector3D> pipe = new List<Vector3D>();
        int segmentPos = 0;
        for (int i = 0; i < pointsInWGS.Count; i++) {
            if (i == segments[segmentPos]) {
                segmentPos++;
                ret.Add(pipe);
                pipe = new List<Vector3D>();
            }
            pipe.Add(pointsInWGS[i]);
        }
        ret.Add(pipe);
        return ret;
    }

}
