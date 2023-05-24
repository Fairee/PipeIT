using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// A converter between coordinate systems
/// </summary>
public class WGSConverter
{
    CoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
    CoordinateSystem utm33n = ProjectedCoordinateSystem.WGS84_UTM(33,true);
    CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
    ICoordinateTransformation transformKrovak;
    ICoordinateTransformation transformUTM;

    private const double EarthRadius = 6371000; // Radius of the Earth in kilometers


    string krovakWKT = "PROJCS[\"S-JTSK / Krovak East North\",GEOGCS[\"S-JTSK\",DATUM[\"System_of_the_Unified_Trigonometrical_Cadastral_Network\", SPHEROID[\"Bessel 1841\", 6377397.155, 299.1528128]," +
           "TOWGS84[589,76,480,0,0,0,0]],  PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]], UNIT[\"degree\",0.0174532925199433," +
         "AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4156\"]],PROJECTION[\"Krovak\"],PARAMETER[\"latitude_of_center\",49.5],PARAMETER[\"longitude_of_center\",24.83333333333333],"+
        "PARAMETER[\"azimuth\",30.2881397527778],PARAMETER[\"pseudo_standard_parallel_1\",78.5],PARAMETER[\"scale_factor\",0.9999],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1,AUTHORITY[\"EPSG\",\"9001\"]]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"5514\"]]";

    /// <summary>
    /// Initialzes all the transfomartions
    /// </summary>
    public WGSConverter()
    {
        CoordinateSystemFactory fac = new CoordinateSystemFactory();
        CoordinateSystem krov = fac.CreateFromWkt(krovakWKT);
        transformUTM = ctfac.CreateFromCoordinateSystems(wgs84, utm33n);
        transformKrovak = ctfac.CreateFromCoordinateSystems(wgs84, krov);
    }
    /// <summary>
    /// Transforms from WGS to Krovak
    /// </summary>
    /// <param name="lat">Latitude of the point</param>
    /// <param name="lon">Longitude of the point</param>
    /// <param name="alt">Altitude of the point</param>
    /// <returns>Vector with Krovaks value, where the Y value points up (Unity notation)</returns>
    public Vector3D TransformKrovak(double lat, double lon, double alt)
    {
        double output1, output2, output3;
        (output1, output2, output3) = transformKrovak.MathTransform.Transform(lon, lat, alt);
        Vector3D ret = new Vector3D(output1, output3, output2);
        return ret;
    }
    /// <summary>
    /// Calculates the angle to allign north with the krovak coordinate system
    /// Not accurate enough!
    /// </summary>
    /// <param name="x">x value of the point</param>
    /// <param name="z">z value of the point (Z because we are working in Unity)</param>
    /// <returns>the angle to allign the krovak coordinate system with north</returns>
    public double GetKrovakRotation(double x, double z) {
        double C = 0.008257 * (z / 1000) + 2.373 * (z / 1000) / (x / 1000);
        return C;
    }

    /// <summary>
    /// Transforms a point from WGS into UTM
    /// </summary>
    /// <param name="lat">Latitude of the point</param>
    /// <param name="lon">Longitude of the point</param>
    /// <param name="alt">Altitude of the point</param>
    /// <returns>Vector with the UTM coordinates, where Y value points up (Unity notation)</returns>
    public Vector3D TransformUTM(double lat, double lon, double alt) {
        double output1, output2, output3;
        (output1, output2, output3) = transformUTM.MathTransform.Transform(lon, lat, alt);
        Vector3D ret = new Vector3D(output1, output3, output2);
        return ret;
    }


    /// <summary>
    /// Calculate distance between two WGS points
    /// </summary>
    /// <param name="lat1">Latitude of the first point</param>
    /// <param name="lon1">Longitude of the first point</param>
    /// <param name="lat2">Latitude of the second point</param>
    /// <param name="lon2">Longitude of the second point</param>
    /// <returns>The distance between the two points</returns>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a = Math.Sin((dLat / 2)) * Math.Sin((dLat / 2)) +
                   Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin((dLon / 2)) * Math.Sin((dLon / 2));

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt((1 - a)));
        double distance = EarthRadius * c;

        return distance;
    }
    /// <summary>
    /// Transforms degrees to radians
    /// </summary>
    /// <param name="degrees">The amount of degrees</param>
    /// <returns>A value in radians</returns>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Mathf.PI / 180;
    }
}
