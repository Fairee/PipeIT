using System.Collections;
using System.Collections.Generic;

using System;
namespace BinGenerator
{
    public struct ShapeFileHeader
    {
        public int shapeType;
        public Vector3D mins;
        public Vector3D maxs;
    }

    public struct ShapeFileRecord
    {
        public int id;
        public List<int> parts;
        public List<Vector3D> points;
    }

    public struct CellData
    {
        public double stepLon;
        public double stepLat;
        public double fromLon;
        public double fromLat;
        public double toLon;
        public double toLat;
        public int numberOfCellsLon;
        public int numberOfCellsLat;
    }

    public struct Zone
    {
        public double fromLon;
        public double fromLat;
        public double toLon;
        public double toLat;
        public List<ShapeFileRecord> records;
        public List<int>[,] ids;
    }



}