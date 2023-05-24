using System;
using System.Collections.Generic;
using System.IO;


namespace BinGenerator
{
    /// <summary>
    /// The generator of the .bin file
    /// </summary>
    public class Generator {

        //Zone data:
        //the first partition into smaller cells:
        //amount of cells
        public int zoneSizeInMeters = 1000;

        //how many degrees is one meter for Prague approximately
        private double meterLat = 0.000009; //Latitude in Prague is around 50 -- Thus meterLat coresponds to Y
        private double meterLong = 0.000012; // Longitude in Prague is around 14 -- Thus meterLong coresponds to X

        //the size of the smallest partition in meters (will be approximated);
        //areaSizeInMeters HAS TO be able to divide the zoneSizeInMeters without any leftover.
        private int areaSizeInMeters = 20;

        public string filename;

        public List<Zone> zones;
        List<ShapeFileRecord> records;
        List<int>[,] zoneSorted;
        ShapeFileParser dataS;
        CellData zoneData;
        // Start is called before the first frame update
        public void extractRawData(string name) {

            dataS = new ShapeFileParser(name);
            dataS.ParseAllData();
            records = dataS.GetRecords();
            return;
        }



        /// <summary>
        /// Generates the grid and fills it
        /// </summary>
        /// <param name="zoneSizeMeters">the size of a zone in meters</param>
        /// <param name="areaSizeMeters">the size of an area in meters</param>
        public void generateGrid(int zoneSizeMeters, int areaSizeMeters)
        {
            if (zoneSizeMeters % areaSizeMeters != 0)
            {
                Console.WriteLine("The zone size isnt divisable by area size without leftover. Please choose different values.");
            }
            zoneSizeInMeters = zoneSizeMeters;
            areaSizeInMeters = areaSizeMeters;

            ShapeFileHeader header = dataS.GetHeader();

            zoneData = new CellData();

            //data for the whole zone
            zoneData.fromLon = header.mins.x;
            zoneData.fromLat = header.mins.y;
            zoneData.toLon = header.maxs.x;
            zoneData.toLat = header.maxs.y;

            zoneData.stepLon = zoneSizeInMeters * meterLong;
            zoneData.stepLat = zoneSizeInMeters * meterLat;

            //find uniform scale for the cells
            //Add one for the rest of the area ... Lets say we have area of 10.1 meters and want to divide it into areas of 1 meter ... without the one it would return 10 and thus creating 10 cells
            // leaving the 0.1 without a cell. With the +1 I create a new cell for the 0.1 and even though it will also include a bit of area outside of the real given area, it will be empty anyway
            // so it doesnt really matter
            zoneData.numberOfCellsLon = (int)Math.Floor((zoneData.toLon - zoneData.fromLon) / zoneData.stepLon) + 1;
            zoneData.numberOfCellsLat = (int)Math.Floor((zoneData.toLat - zoneData.fromLat) / zoneData.stepLat) + 1;



            zoneSorted = sort(zoneData, records);


            zones = new List<Zone>();

            //first partitioning by zones -- first grid division
            for (int longitudeIndex = 0; longitudeIndex < zoneData.numberOfCellsLon; longitudeIndex++)
            {
                for (int latitudeIndex = 0; latitudeIndex < zoneData.numberOfCellsLat; latitudeIndex++)
                {
                    //Discard empty zones
                    if (zoneSorted[longitudeIndex, latitudeIndex].Count == 0)
                    {
                        continue;
                    }

                    Zone z = new Zone();
                    z.records = new List<ShapeFileRecord>();
                    z.fromLon = zoneData.fromLon + longitudeIndex * zoneData.stepLon;
                    z.toLon = zoneData.fromLon + (longitudeIndex + 1) * zoneData.stepLon;
                    z.fromLat = zoneData.fromLat + latitudeIndex * zoneData.stepLat;
                    z.toLat = zoneData.fromLat + (latitudeIndex + 1) * zoneData.stepLat;
                    foreach (int id in zoneSorted[longitudeIndex, latitudeIndex])
                    {
                        z.records.Add(records[id - 1]);
                    }

                    zones.Add(z);

                }
            }



            //second division
            for (int i = 0; i < zones.Count; i++)
            {
                Zone zone = zones[i];
                CellData areaData = new CellData();

                areaData.fromLon = zone.fromLon;
                areaData.fromLat = zone.fromLat;
                areaData.toLon = zone.toLon;
                areaData.toLat = zone.toLat;

                areaData.stepLon = areaSizeInMeters * meterLong;
                areaData.stepLat = areaSizeInMeters * meterLat;

                areaData.numberOfCellsLon = zoneSizeInMeters / areaSizeInMeters;
                areaData.numberOfCellsLat = zoneSizeInMeters / areaSizeInMeters;

                List<int>[,] areaSorted = sort(areaData, zone.records);
                zone.ids = areaSorted;
                zones[i] = zone;

            }

            return;

        }






        /// <summary>
        /// Initialze the list
        /// </summary>
        /// <param name="numberOfCellsLon">amount of cells along the X axis</param>
        /// <param name="numberOfCellsLat">amount of cells along the Y axis</param>
        /// <returns></returns>
        private List<int>[,] InitializeList(int numberOfCellsLon, int numberOfCellsLat) {
            List<int>[,] list = new List<int>[numberOfCellsLon, numberOfCellsLat];

            for (int longitudeIndex = 0; longitudeIndex < numberOfCellsLon; longitudeIndex++)
            {
                for (int latitudeIndex = 0; latitudeIndex < numberOfCellsLat; latitudeIndex++)
                {
                    list[longitudeIndex, latitudeIndex] = new List<int>();
                }
            }

            return list;
        }


        /// <summary>
        /// Sort the records into the subcells of a grid
        /// </summary>
        /// <param name="cellData">zone or area data</param>
        /// <param name="records">the records which are ment to be inside</param>
        /// <returns></returns>
        public List<int>[,] sort(CellData cellData, List<ShapeFileRecord> records) {


            //intialize the list
            List<int>[,] ids = InitializeList(cellData.numberOfCellsLon, cellData.numberOfCellsLat);

            foreach (ShapeFileRecord rec in records) {
                if (rec.points.Count == 0) { continue; }
                //find the cell into which the point falls
                int firstPointIndexX = (int)Math.Floor(((rec.points[0].x - cellData.fromLon) / cellData.stepLon));
                int firstPointIndexY = (int)Math.Floor(((rec.points[0].y - cellData.fromLat) / cellData.stepLat));
                //Iterate over all the pieces of the pipe and check into which cell they belong 
                for (int i = 1; i < rec.points.Count; i++) {
                    //find the cell into which the point falls
                    int secondPointIndexX = (int)Math.Floor(((rec.points[i].x - cellData.fromLon) / cellData.stepLon));
                    int secondPointIndexY = (int)Math.Floor(((rec.points[i].y - cellData.fromLat) / cellData.stepLat));

                    //get the mins and maxs (so that it can be used for the for cycles)
                    int minIndexLon = Math.Min(firstPointIndexX, secondPointIndexX);
                    int maxIndexLon = Math.Max(firstPointIndexX, secondPointIndexX);
                    int minIndexLat = Math.Min(firstPointIndexY, secondPointIndexY);
                    int maxIndexLat = Math.Max(firstPointIndexY, secondPointIndexY);

                    //Prevent special case where the maxes are equal to the gridCellNumber which would be out of range for the array
                    if (maxIndexLon >= cellData.numberOfCellsLon) { maxIndexLon = cellData.numberOfCellsLon - 1; }
                    if (maxIndexLat >= cellData.numberOfCellsLat) { maxIndexLat = cellData.numberOfCellsLat - 1; }

                    if (minIndexLon < 0) { minIndexLon = 0; }
                    if (minIndexLat < 0) { minIndexLat = 0; }

                    //this will quickly throw away the cells in which the pipe isn't (creates a bounding box and check just the cells in the bounding box)
                    for (int longitudeIndex = minIndexLon; longitudeIndex <= maxIndexLon; longitudeIndex++) {
                        for (int latitudeIndex = minIndexLat; latitudeIndex <= maxIndexLat; latitudeIndex++) {
                            //check if the pipe wasn't already added
                            if (!ids[longitudeIndex, latitudeIndex].Contains(rec.id))
                            {
                                //counts the borders of the area
                                double bottom = cellData.fromLat + latitudeIndex * cellData.stepLat;
                                double top = cellData.fromLat + (latitudeIndex + 1) * cellData.stepLat;
                                double left = cellData.fromLon + longitudeIndex * cellData.stepLon;
                                double right = cellData.fromLon + (longitudeIndex + 1) * cellData.stepLon;


                                //get the data of the points
                                double firstPointLon = rec.points[i - 1].x;
                                double firstPointLat = rec.points[i - 1].y;

                                double secondPointLon = rec.points[i].x;
                                double secondPointLat = rec.points[i].y;

                                //tests if the line is or intersects the area
                                if (ContainmentTest(bottom, top, left, right, firstPointLon, firstPointLat, secondPointLon, secondPointLat)) {
                                    ids[longitudeIndex, latitudeIndex].Add(rec.id);
                                }

                            }
                        }
                    }
                    //continue with the next part
                    firstPointIndexX = secondPointIndexX;
                    firstPointIndexY = secondPointIndexY;
                }
            }

            return ids;
        }

        /// <summary>
        /// Tests if a line between two points is inside at least partially inside an area
        /// </summary>
        /// <param name="bottom">The minimum Y value of the area</param>
        /// <param name="top">The maximum Y value of the area</param>
        /// <param name="left">The minimal X value of the area</param>
        /// <param name="right">The maximum X value of the area</param>
        /// <param name="firstPointX">X coord of first point</param>
        /// <param name="firstPointY">Y coord of first point</param>
        /// <param name="secondPointX">X coord of second point</param>
        /// <param name="secondPointY">Y coord of second point</param>
        /// <returns></returns>
        bool ContainmentTest(double bottom, double top, double left, double right, double firstPointX, double firstPointY, double secondPointX, double secondPointY) {
            //check if the first point is inside the area (this will take care of the whole line being inside)
            if (bottom < firstPointY && top > firstPointY && left < firstPointX && right > firstPointX)
            {
                return true;
            }
            else if (GetLineIntersection(firstPointX, firstPointY, secondPointX, secondPointY, left, bottom, left, top) ||
                 GetLineIntersection(firstPointX, firstPointY, secondPointX, secondPointY, left, top, right, top) ||
                 GetLineIntersection(firstPointX, firstPointY, secondPointX, secondPointY, right, top, right, bottom) ||
                 GetLineIntersection(firstPointX, firstPointY, secondPointX, secondPointY, right, bottom, left, bottom))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Tests if two lines intersect
        /// </summary>
        /// <param name="firstPointX">X value of first point of first line</param>
        /// <param name="firstPointY">Y value of first point of first line</param>
        /// <param name="secondPointX">X value of second point of first line</param>
        /// <param name="secondPointY">Y value of second point of first line</param>
        /// <param name="thirdPointX">X value of first point of second line</param>
        /// <param name="thirdPointY">Y value of first point of second line</param>
        /// <param name="fourthPointX">X value of second point of second line</param>
        /// <param name="fourthPointY">Y value of second point of second line</param>
        /// <returns></returns>
        bool GetLineIntersection(double firstPointX, double firstPointY, double secondPointX, double secondPointY, double thirdPointX, double thirdPointY, double fourthPointX, double fourthPointY)
        {
            double firstLineX, firstLineY, secondLineX, secondLineY;
            firstLineX = secondPointX - firstPointX;
            firstLineY = secondPointY - firstPointY;
            secondLineX = fourthPointX - thirdPointX;
            secondLineY = fourthPointY - thirdPointY;

            double denominator = firstLineX * secondLineY - firstLineY * secondLineX;


            if (denominator == 0)
            {
                // The line segments are parallel or coincident
                return false;
            }

            double t1 = ((thirdPointX - firstPointX) * secondLineY - (thirdPointY - firstPointY) * secondLineX) / denominator;
            double t2 = ((thirdPointX - firstPointX) * firstLineY - (thirdPointY - firstPointY) * firstLineX) / denominator;

            if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates the bin file based on the processed data
        /// </summary>
        /// <param name="name">name of the file</param>
        public void CreateSearchFile(string name) {
            using (BinaryWriter writer = new BinaryWriter(File.Open(name, FileMode.Create))) {

                int areaSizeLon = zoneSizeInMeters / areaSizeInMeters;
                int areaSizeLat = zoneSizeInMeters / areaSizeInMeters;

                int zoneSizeLon = (int)Math.Floor((zoneData.toLon - zoneData.fromLon) / zoneData.stepLon) + 1;
                int zoneSizeLat = (int)Math.Floor((zoneData.toLat - zoneData.fromLat) / zoneData.stepLat) + 1;

                int written = 0;
                writer.Write(meterLat); // 8 bytes
                writer.Write(meterLong); // 8 bytes
                writer.Write(zoneSizeInMeters); // 4 bytes 
                writer.Write(areaSizeInMeters); // 4 bytes
                writer.Write(zoneData.fromLon); // 8 bytes
                writer.Write(zoneData.toLon); // 8 bytes
                writer.Write(zoneData.fromLat); // 8 bytes
                writer.Write(zoneData.toLat); // 8 bytes
                writer.Write(zoneSizeLon); // 4 bytes
                writer.Write(zoneSizeLat); // 4 bytes
                writer.Write(areaSizeLon); // 4 bytes
                writer.Write(areaSizeLat); // 4 bytes
                int headersize = 8 * 6 + 4 * 6;
                written += headersize;
                bool towrite = false;

                //calculate where will be the end of the zones data
                int offset = zoneSorted.GetLength(0) * zoneSorted.GetLength(1) * 5 + headersize; //  4 - offset int + 1 - bool
                //write for all of the zones
                for (int x = 0; x < zoneSizeLon; x++) {
                    for (int y = 0; y < zoneSizeLat ; y++)
                    {
                        towrite = false;
                        if (zoneSorted[x, y].Count > 0)
                        {
                            towrite = true;
                            writer.Write(towrite);
                            writer.Write(offset);
                            //add to the offset for what will be writen there, so we are able to point to the right location
                            offset += areaSizeLon * areaSizeLat * 5; //  4 - offset int + 1 - bool
                        }
                        else
                        {
                            writer.Write(towrite);
                            writer.Write(offset);
                        }
                        written += 5;
                    }
                }
                //calculate where will be the end of the areas data
                //amount of zones  * 5(each zone 5 bytes) + size of header  + number of zones that have something in them * amount of areas * 5(each area 5 bytes)
                offset = zoneSorted.GetLength(0) * zoneSorted.GetLength(1) * 5 + headersize + zones.Count * areaSizeLon * areaSizeLat * 5;
                //write areas for the zones that have at least one pipe leading through them
                foreach (Zone zone in zones) {
                    for (int x = 0; x < areaSizeLon; x++) {
                        for (int y = 0; y < areaSizeLat; y++) {
                            towrite = false;
                            if (zone.ids[x, y].Count > 0)
                            {
                                towrite = true;
                                writer.Write(towrite);
                                writer.Write(offset);
                                //add to the offset for what will be written there, so we are able to point to the right location
                                offset += 4 + 4 * zone.ids[x, y].Count; // 4(int) - the lenght +  4(int) id * number of Ids;
                            }
                            else
                            {
                                writer.Write(towrite);
                                writer.Write(offset);
                            }
                            written += 5;
                        }
                    }
                }
                //finally write the ids for each of the area
                foreach (Zone zone in zones) {
                    for (int x = 0; x < areaSizeLon; x++)
                    {
                        for (int y = 0; y < areaSizeLat; y++)
                        {
                            if (zone.ids[x, y].Count > 0)
                            {
                                int size = zone.ids[x, y].Count;
                                writer.Write(size);
                                written += 4;
                                foreach (int num in zone.ids[x, y]) {
                                    writer.Write(num);
                                    written += 4;
                                }
                            }

                        }
                    }
                }

            }

        }

    }
}