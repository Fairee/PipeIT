using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


/// <summary>
/// A Vector class that uses doubles
/// </summary>
public class Vector3D
{
    // x = latitude, y = longitude, z = altitude
    public double x, y, z;

    public Vector3D() {
        x = 0;
        y = 0;
        z = 0;
    }

    public Vector3D(double a, double b, double c)
    {
        x = a;
        y = b;
        z = c;
    }

    public static Vector3D operator +(Vector3D A, Vector3D B)
    {
        Vector3D ret = new Vector3D();
        ret.x = A.x + B.x;
        ret.y = A.y + B.y;
        ret.z = A.z + B.z;
        return ret;
    }

    public static Vector3D operator *(Vector3D A, double b) {
        Vector3D ret = new Vector3D();
        ret.x = A.x * b;
        ret.y = A.y * b;
        ret.z = A.z * b;
        return ret;
    }

    public static Vector3D operator *(double b,Vector3D A)
    {
        Vector3D ret = new Vector3D();
        ret.x = A.x * b;
        ret.y = A.y * b;
        ret.z = A.z * b;
        return ret;
    }

    public static Vector3D operator /(Vector3D A, double b)
    {
        Vector3D ret = new Vector3D();
        ret.x = A.x / b;
        ret.y = A.y / b;
        ret.z = A.z / b;
        return ret;
    }


    public static Vector3D operator -(Vector3D A, Vector3D B)
    {
        Vector3D ret = new Vector3D();
        ret.x = A.x - B.x;
        ret.y = A.y - B.y;
        ret.z = A.z - B.z;
        return ret;
    }

    public double Distance() {
        return Math.Sqrt(x*x + y*y +z*z);    
    }

    public double Distance(Vector3D other) {
        Vector3D t = this - other;
        return t.Distance();
    }

    public void Normalize() {
        double dist = Distance();
        x = x / dist;
        y = y / dist;
        z = z / dist;
    }


    public Vector3 ToFloat()
    {
        return new Vector3((float)x, (float)y, (float)z);
    }


    public override string ToString()
    {
        return "x: "+ x + " y: " + y + " z: " + z + "\n";
    }

    public Vector3D CrossProduct(Vector3D other) {
        double nx = y * other.z - z * other.y;
        double ny = z * other.x - x * other.z;
        double nz = x * other.y - y * other.x;

        return new Vector3D(nx, ny, nz);
    
    }

    public float DotProduct(Vector3D other) {
        return (float)(x * other.x + y * other.y + z * other.z);
    }

}
