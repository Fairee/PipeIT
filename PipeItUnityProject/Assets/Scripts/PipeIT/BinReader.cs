using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]


/// <summary>
/// This class take care of parsing the file we have created for quick grid access
/// It also takes case of just calculating stuff based on the header if we get just the header and not the whole file
/// </summary>
public class BinReader
{

    public struct BinHeader
    {
        public double meterLat { get; set; }
        public double meterLong { get; set; }
        public int zoneSizeInMeters { get; set; }
        public int areaSizeInMeters { get; set; }
        public double fromLon { get; set; }
        public double toLon { get; set; }
        public double fromLat { get; set; }
        public double toLat { get; set; }
        public int amountOfZonesLon { get; set; }
        public int amountOfZonesLat { get; set; }
        public int amountOfAreasLon { get; set; }
        public int amountOfAreasLat { get; set; }
    }


    string fileName;
    int amountOfZonesLon;
    int amountOfZonesLat;
    int amountOfAreasLon;
    int amountOfAreasLat;
    int headerSize = 72;
    int packetSize = 5;
    int intSize = 4;
    BinHeader binHeader;
    bool wasParsed;

    /// <summary>
    /// Create a Bin reader from the .bin file
    /// </summary>
    /// <param name="path">the whole path to the file</param>
    public BinReader(string path) {
        fileName = path;
        ParseHeader();
        wasParsed = true;
    }
    /// <summary>
    /// Creates a Bin reader just from its header
    /// </summary>
    /// <param name="header">The header of the bin reader</param>
    public BinReader(BinHeader header) {
        binHeader = header;
        wasParsed = false;

        amountOfZonesLon = Mathf.FloorToInt((float)((binHeader.toLon - binHeader.fromLon) / (binHeader.meterLong * binHeader.zoneSizeInMeters))) + 1;
        amountOfZonesLat = Mathf.FloorToInt((float)((binHeader.toLat - binHeader.fromLat) / (binHeader.meterLat * binHeader.zoneSizeInMeters))) + 1;

        amountOfAreasLon = binHeader.zoneSizeInMeters / binHeader.areaSizeInMeters;
        amountOfAreasLat = binHeader.zoneSizeInMeters / binHeader.areaSizeInMeters;
    }
    /// <summary>
    /// Getter for the binHeader
    /// </summary>
    /// <returns>the bin Header</returns>
    public BinHeader GetBinHeader() {
        return binHeader;
    }
    /// <summary>
    /// Getter for the amount of zones longitude-wise
    /// </summary>
    /// <returns>the amount of zones longitude-wise</returns>
    public int GetAmountOfZonesLon()
    {
        return amountOfZonesLon;
    }

    /// <summary>
    /// Getter for the amount of zones latitude-wise
    /// </summary>
    /// <returns>the amount of zones latitude-wise</returns>
    public int GetAmountOfZonesLat()
    {
        return amountOfZonesLat;
    }
    /// <summary>
    /// Getter for the amount of areas longitude-wise
    /// </summary>
    /// <returns>the amount of areas  longitude-wise</returns>
    public int GetAmountOfAreasLon()
    {
        return amountOfAreasLon;
    }
    /// <summary>
    /// Getter for the amount of areas latitude-wise
    /// </summary>
    /// <returns>the amount of areas latitude-wise</returns>
    public int GetAmountOfAreasLat()
    {
        return amountOfAreasLat;
    }
    /// <summary>
    /// Takes care of parsing the header from the bin file
    /// </summary>
    private void ParseHeader() {
        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            //read the file according to the offical description (Provided in the project binreader.pdf)
            binHeader = new BinHeader();
            byte[] header = new byte[headerSize];
            fileStream.Read(header, 0, headerSize);
            binHeader.meterLat = BitConverter.ToDouble(header, 0);
            binHeader.meterLong = BitConverter.ToDouble(header, 8);
            binHeader.zoneSizeInMeters = BitConverter.ToInt32(header, 16);
            binHeader.areaSizeInMeters = BitConverter.ToInt32(header, 20);
            binHeader.fromLon = BitConverter.ToDouble(header, 24);
            binHeader.toLon = BitConverter.ToDouble(header, 32);
            binHeader.fromLat = BitConverter.ToDouble(header, 40);
            binHeader.toLat = BitConverter.ToDouble(header, 48);
            binHeader.amountOfZonesLon = BitConverter.ToInt32(header, 56);
            binHeader.amountOfZonesLat = BitConverter.ToInt32(header, 60);
            binHeader.amountOfAreasLon = BitConverter.ToInt32(header, 64);
            binHeader.amountOfAreasLat = BitConverter.ToInt32(header, 68);

            amountOfZonesLon = Mathf.FloorToInt((float)((binHeader.toLon - binHeader.fromLon) / (binHeader.meterLong * binHeader.zoneSizeInMeters))) + 1;
            amountOfZonesLat = Mathf.FloorToInt((float)((binHeader.toLat - binHeader.fromLat) / (binHeader.meterLat * binHeader.zoneSizeInMeters))) + 1;

            amountOfAreasLon = binHeader.zoneSizeInMeters / binHeader.areaSizeInMeters;
            amountOfAreasLat = binHeader.zoneSizeInMeters / binHeader.areaSizeInMeters;
        }

    }
    /// <summary>
    /// generates a name for the area based on its indexes
    /// </summary>
    /// <param name="zoneLatIndex">latitude index of the zone</param>
    /// <param name="zoneLonIndex">longitude index of the zone</param>
    /// <param name="areaLatIndex">latitude index of the area</param>
    /// <param name="areaLonIndex">longitude index of the area</param>
    /// <returns></returns>
    public static string GetAreaName(int zoneLatIndex, int zoneLonIndex, int areaLatIndex, int areaLonIndex)
    {
        return zoneLonIndex + "-" + zoneLatIndex + "/" + areaLonIndex + "-" + areaLatIndex;
    }

    /// <summary>
    /// Get the indexes by position
    /// </summary>
    /// <param name="latitude">latitude postion</param>
    /// <param name="longitude">longitude position</param>
    /// <param name="zoneLatIndex">OUT zone latitude index</param>
    /// <param name="zoneLonIndex">OUT zone longitude index</param>
    /// <param name="areaLatIndex">OUT area latitude index</param>
    /// <param name="areaLonIndex">OUT area longitude index</param>
    /// <returns>Returns whether the indexes are within the bounds</returns>
    public bool GetIndexesByPosition(double latitude, double longitude,  out int zoneLatIndex, out int zoneLonIndex,  out int areaLatIndex, out int areaLonIndex) {
        zoneLonIndex = -1;
        zoneLatIndex = -1;
        areaLonIndex = -1;
        areaLatIndex = -1;
        if (longitude < binHeader.fromLon || latitude < binHeader.fromLat || longitude > binHeader.toLon || latitude > binHeader.toLat)
        {
            Debug.Log("Given position is not within the cover area");
            return false;
        }
        //get the zone index by finding where the point falls 
        int zoneIndexLon = Mathf.FloorToInt((float)((longitude - binHeader.fromLon) / (binHeader.meterLong * binHeader.zoneSizeInMeters)));
        int zoneIndexLat = Mathf.FloorToInt((float)((latitude - binHeader.fromLat) / (binHeader.meterLat * binHeader.zoneSizeInMeters)));

        //find the boundaries of the zone
        double areasFromLon = binHeader.fromLon + (zoneIndexLon) * (binHeader.meterLong * binHeader.zoneSizeInMeters);
        double areasFromLat = binHeader.fromLat + zoneIndexLat * (binHeader.meterLat * binHeader.zoneSizeInMeters);

        //find the position within the zone
        int areaIndexX = Mathf.FloorToInt((float)((longitude - areasFromLon) / (binHeader.meterLong * binHeader.areaSizeInMeters)));
        int areaIndexY = Mathf.FloorToInt((float)((latitude - areasFromLat) / (binHeader.meterLat * binHeader.areaSizeInMeters)));

        zoneLonIndex = zoneIndexLon;
        zoneLatIndex = zoneIndexLat;
        areaLonIndex = areaIndexX;
        areaLatIndex = areaIndexY;

        return true;
    }
    /// <summary>
    /// Get the boundaries of an area in which the position is
    /// </summary>
    /// <param name="latitude">the latitude position</param>
    /// <param name="longitude">the lognitude position</param>
    /// <param name="fromAreaLat">OUT the minimum latitude boundary</param>
    /// <param name="fromAreaLon">OUT the minimum longitude boundary</param>
    /// <param name="toAreaLat">OUT the maximum latitude boundary</param>
    /// <param name="toAreaLon">OUT the maximum longitude boundary</param>
    /// <returns>Whether the position is within the mapped region</returns>
    public bool GetAreasFromsAndTosPosition(double latitude, double longitude, out double fromAreaLat, out double fromAreaLon, out double toAreaLat, out double toAreaLon) {
        fromAreaLon = 0; fromAreaLat = 0; toAreaLon = 0; toAreaLat = 0;
        if (longitude < binHeader.fromLon || latitude < binHeader.fromLat || longitude > binHeader.toLon || latitude > binHeader.toLat)
        {
            Debug.Log("Given position is not within the cover area");
            return false;
        }
        //get the zone index by finding where the point falls 
        int zoneIndexLon = Mathf.FloorToInt((float)((longitude - binHeader.fromLon) / (binHeader.meterLong * binHeader.zoneSizeInMeters)));
        int zoneIndexLat = Mathf.FloorToInt((float)((latitude - binHeader.fromLat) / (binHeader.meterLat * binHeader.zoneSizeInMeters)));
        
        //find the boundaries of the zone
        double areasFromLon = binHeader.fromLon + (zoneIndexLon) * (binHeader.meterLong * binHeader.zoneSizeInMeters);
        double areasFromLat = binHeader.fromLat + zoneIndexLat * (binHeader.meterLat * binHeader.zoneSizeInMeters);

        //find the position within the zone
        int areaIndexLon = Mathf.FloorToInt((float)((longitude - areasFromLon) / (binHeader.meterLong * binHeader.areaSizeInMeters)));
        int areaIndexLat = Mathf.FloorToInt((float)((latitude - areasFromLat) / (binHeader.meterLat * binHeader.areaSizeInMeters)));

        //find the boundaries of the area
        fromAreaLon = areasFromLon + areaIndexLon * binHeader.meterLong * binHeader.areaSizeInMeters;
        fromAreaLat = areasFromLat + areaIndexLat * binHeader.meterLat * binHeader.areaSizeInMeters;
        toAreaLon = areasFromLon + (areaIndexLon+1) * binHeader.meterLong * binHeader.areaSizeInMeters;
        toAreaLat = areasFromLat + (areaIndexLat+1) * binHeader.meterLat * binHeader.areaSizeInMeters;
        return true;
    }
    /// <summary>
    ///  Found the boundaries of an area based on its indexes
    /// </summary>
    /// <param name="zoneLatIndex"> zone latitude index of the area</param>
    /// <param name="zoneLonIndex"> zone longitude index of the area</param>
    /// <param name="areaLatIndex"> area latitude index of the area</param>
    /// <param name="areaLonIndex"> area longitude index of the area</param>
    /// <param name="fromAreaLat">OUT the minimum latitude boundary</param>
    /// <param name="fromAreaLon">OUT the minimum longitude boundary</param>
    /// <param name="toAreaLat">OUT the maximum latitude boundary</param>
    /// <param name="toAreaLon">OUT the maximum longitude boundary</param>
    /// <returns>Whether the indexes are within the mapped region</returns>
    public bool GetAreasFromAndToByIndex(int zoneLatIndex, int zoneLonIndex, int areaLatIndex, int areaLonIndex,  out double fromAreaLat, out double fromAreaLon, out double toAreaLat, out double toAreaLon) {
        fromAreaLon = 0; fromAreaLat = 0; toAreaLon = 0; toAreaLat = 0;
        if (zoneLonIndex >= amountOfZonesLon || zoneLatIndex >= amountOfZonesLat || zoneLonIndex < 0 || zoneLatIndex < 0)
        {
            Debug.Log("Given zone indexes are out of bounds");
            return false;
        }
        if (areaLonIndex >= amountOfAreasLon || areaLatIndex >= amountOfAreasLat || areaLonIndex < 0 || areaLatIndex < 0) {
            Debug.Log("Given area indexes are out of bounds");
            return false;
        }
        double areasFromLon = binHeader.fromLon + zoneLonIndex * (binHeader.meterLong * binHeader.zoneSizeInMeters);

        double areasFromLat = binHeader.fromLat + zoneLatIndex * (binHeader.meterLat * binHeader.zoneSizeInMeters);

        fromAreaLon = areasFromLon + areaLonIndex * binHeader.meterLong * binHeader.areaSizeInMeters;
        fromAreaLat = areasFromLat + areaLatIndex * binHeader.meterLat * binHeader.areaSizeInMeters;
        toAreaLon = areasFromLon + (areaLonIndex + 1) * binHeader.meterLong * binHeader.areaSizeInMeters;
        toAreaLat = areasFromLat + (areaLatIndex + 1) * binHeader.meterLat * binHeader.areaSizeInMeters;
        return true;
    }

    /// <summary>
    /// Get the max indexes of the zone
    /// </summary>
    /// <returns>(int, int) maximum latitude, maximum longitude zone index</returns>
    public (int zoneMaxLat, int zoneMaxLon) GetZoneMaxIndexes() {
        return (amountOfZonesLon-1, amountOfZonesLat-1);
    }
    /// <summary>
    /// Get the max indexes of the area
    /// </summary>
    /// <returns>(int, int) maximum latitude, maximum longitude area index</returns>
    public (int areaMaxLat, int areaMaxLon) GetAreaMaxIndexes() {
        return (amountOfAreasLon-1, amountOfAreasLat-1);
    }

    /// <summary>
    /// Get the list of ids in the area based on position
    /// </summary>
    /// <param name="latitude">latitude positon</param>
    /// <param name="longitude">longitude position</param>
    /// <returns></returns>
    public List<int> GetAtPosition(double latitude, double longitude) {
        //if the header was recieved, not parsed, we cant do it as we dont have the file
        if (!wasParsed) {
            Debug.Log("The header was not obtained by parsing!");
            return null;
        }

        List<int> ids = new List<int>();

        if (longitude < binHeader.fromLon || latitude < binHeader.fromLat || longitude > binHeader.toLon || latitude > binHeader.toLat)
        {
            Debug.Log("Given position is not within the cover area");
            return ids;
        }

        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            //get the zone index by finding where the point falls 
            int zoneIndexLon = Mathf.FloorToInt((float)((longitude - binHeader.fromLon) / (binHeader.meterLong * binHeader.zoneSizeInMeters)));
            int zoneIndexLat = Mathf.FloorToInt((float)((latitude - binHeader.fromLat) / (binHeader.meterLat * binHeader.zoneSizeInMeters)));

            //find the offset to read the information for the appropriate zone
            int offset = (zoneIndexLon * amountOfZonesLat + zoneIndexLat) * packetSize + headerSize;
            fileStream.Seek(offset, SeekOrigin.Current);

            byte[] packet = new byte[packetSize];
            //check whats written there, if 0 there is no pipes leading through the zone
            fileStream.Read(packet, 0, packetSize);
            if (packet[0] == 0) {
                return ids;
            }
            //if 1 there are some pipes in the zone, read the offset for the zone
            int areaOffset = BitConverter.ToInt32(packet, 1);

            //find the boundaries of the zones
            double zoneFromLon = binHeader.fromLon + (zoneIndexLon) * (binHeader.meterLong * binHeader.zoneSizeInMeters);
            double zoneFromLat = binHeader.fromLat + zoneIndexLat * (binHeader.meterLat * binHeader.zoneSizeInMeters);

            //find the area indexes
            int areaIndexLon = Mathf.FloorToInt((float)((longitude - zoneFromLon) / (binHeader.meterLong * binHeader.areaSizeInMeters)));
            int areaIndexLat = Mathf.FloorToInt((float)((latitude - zoneFromLat) / (binHeader.meterLat * binHeader.areaSizeInMeters)));

            //skip area packets that are before the one we are looking for
            int inAreaOffset = (areaIndexLon * amountOfAreasLat + areaIndexLat) * packetSize; 

            //take the offset we read and add the offset within the zone
            int totalAreaOffset = areaOffset + inAreaOffset;
            //count by how much do I have to move to get to the new position (-5 for the packet ive just read)
            int nextOffset = totalAreaOffset - offset - packetSize; 
            fileStream.Seek(nextOffset, SeekOrigin.Current);
            //read the data and check whether the area has something in it, if 0 there is nothing
            fileStream.Read(packet, 0, packetSize);
            if (packet[0] == 0) {
                return ids;
            }
            //get the offset for the ids
            int IDsOffset = BitConverter.ToInt32(packet, 1);
            //count by how much do I have to move to get to the new position (-5 for the packet ive just read)
            nextOffset = IDsOffset - totalAreaOffset - packetSize;
            
            byte[] length = new byte[intSize];
            fileStream.Seek(nextOffset, SeekOrigin.Current);

            fileStream.Read(length, 0, intSize);
            //read and parse the wanted data
            int IDsLenght = BitConverter.ToInt32(length,0);

            byte[] idsByte = new byte[IDsLenght*intSize];
            fileStream.Read(idsByte, 0, IDsLenght * intSize);
            for (int i = 0; i < IDsLenght; i++) {
                ids.Add(BitConverter.ToInt32(idsByte, i * 4));
            }
            return ids;
        }
    }

    /// <summary>
    /// Get the list of ids in the area based on the indexes
    /// </summary>
    /// <param name="zoneLatIndex"> zone latitude index of the area</param>
    /// <param name="zoneLonIndex"> zone longitude index of the area</param>
    /// <param name="areaLatIndex"> area latitude index of the area</param>
    /// <param name="areaLonIndex"> area longitude index of the area</param>
    /// <returns>List of ids of the pipes going through the area</returns>
    public List<int> GetByIndexes( int zoneLatIndex, int zoneLonIndex, int areaLatIndex, int areaLonIndex) {
        if (!wasParsed)
        {
            Debug.Log("The header was not obtained by parsing!");
            return null;
        }

        List<int> ids = new List<int>();
        if (zoneLonIndex >= amountOfZonesLon || zoneLatIndex >= amountOfZonesLat || areaLonIndex >= amountOfAreasLon || areaLatIndex >= amountOfAreasLat ||
            zoneLonIndex < 0 || zoneLatIndex < 0 || areaLonIndex < 0 || areaLatIndex < 0) {
            Debug.Log("Indexes were out of range!");
            return ids;
        }


        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            //find the offset to read the information for the appropriate zone
            int offset = (zoneLonIndex * amountOfZonesLat + zoneLatIndex) * packetSize + headerSize;
            fileStream.Seek(offset, SeekOrigin.Current);


            byte[] packet = new byte[packetSize];
            //check whats written there, if 0 there is no pipes leading through the zone
            fileStream.Read(packet, 0, packetSize);
            if (packet[0] == 0)
            {
                return ids;
            }
            //if 1 there are some pipes in the zone, read the offset for the zone
            int areaOffset = BitConverter.ToInt32(packet, 1);
            //skip area packets that are before the one we are looking for
            int inAreaOffset = (areaLonIndex * amountOfAreasLat + areaLatIndex) * packetSize;
            //take the offset we read and add the offset within the zone
            int totalAreaOffset = areaOffset + inAreaOffset;
            //count by how much do I have to move to get to the new position (-packetSize for the packet ive just read)
            int nextOffset = totalAreaOffset - offset - packetSize; 
            fileStream.Seek(nextOffset, SeekOrigin.Current);
            //read the data and check whether the area has something in it, if 0 there is nothing
            fileStream.Read(packet, 0, packetSize);
            if (packet[0] == 0)
            {
                return ids;
            }
            //get the offset for the ids
            int IDsOffset = BitConverter.ToInt32(packet, 1);
            //count by how much do I have to move to get to the new position (-packetSize for the packet ive just read)
            nextOffset = IDsOffset - totalAreaOffset - packetSize;

            byte[] length = new byte[intSize];
            fileStream.Seek(nextOffset, SeekOrigin.Current);

            fileStream.Read(length, 0, intSize);

            int IDsLenght = BitConverter.ToInt32(length, 0);
            //read and parse the wanted data
            byte[] idsByte = new byte[IDsLenght * intSize];
            fileStream.Read(idsByte, 0, IDsLenght * intSize);
            for (int i = 0; i < IDsLenght; i++)
            {
                ids.Add(BitConverter.ToInt32(idsByte, i * 4));
            }
          
        }
        return ids;
    }

    /// <summary>
    /// Get a valid index withingthe zone boundaries
    /// </summary>
    /// <param name="zoneLatInd">zone latitude index of the area</param>
    /// <param name="zoneLonInd">zone longitude index of the area</param>
    /// <param name="areaLatInd">area latitude index of the area</param>
    /// <param name="areaLonInd">area longitude index of the area</param>
    /// <param name="newZoneLatInd">OUT new zone latitude index of the area</param>
    /// <param name="newZoneLonInd">OUT new zone longitude index of the area</param>
    /// <param name="newAreaLatInd">OUT new area latitude index of the area</param>
    /// <param name="newAreaLonInd">OUT new area longitude index of the area</param>
    /// <returns>False when its not possible</returns>
    public bool getValidIndex(int zoneLatInd, int zoneLonInd, int areaLatInd, int areaLonInd, out int newZoneLatInd, out int newZoneLonInd, out int newAreaLatInd, out int newAreaLonInd) {
        newZoneLatInd = 0;  newZoneLonInd = 0; newAreaLatInd = 0; newAreaLonInd = 0;
        if (areaLatInd == amountOfAreasLat)
        {
            areaLatInd = 0;
            zoneLatInd += 1;
        }
        else if (areaLatInd < 0) {
            areaLatInd = amountOfAreasLat - 1;
            zoneLatInd -= 1;
        }
        if (areaLonInd == amountOfAreasLon)
        {
            areaLonInd = 0;
            zoneLonInd += 1;
        }
        else if (areaLonInd < 0) {
            areaLonInd = amountOfAreasLon - 1;
            zoneLonInd -= 1;
        }
        if (zoneLatInd < 0 || zoneLatInd == amountOfZonesLat ||
            zoneLonInd < 0 || zoneLonInd == amountOfZonesLon) {
            return false;
        }

        newZoneLatInd = zoneLatInd;
        newZoneLonInd = zoneLonInd;
        newAreaLatInd = areaLatInd;
        newAreaLonInd = areaLonInd;
        return true;
    }
}
