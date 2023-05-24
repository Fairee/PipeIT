using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace pipeIT
{
    public class SHPreader
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



        private ShapeFileHeader header;

        string shapeFileName = "C:\\data\\TMISPRUBEH_L.shp";
        string indexFileName;
        int headerSize = 100;
        int indexRecordSize = 8;

        public SHPreader()
        {
            indexFileName = shapeFileName.Substring(0, shapeFileName.Length - 3) + "shx";
            ParseHeader();
        }

        /// <summary>
        /// Get the records for the given ids
        /// </summary>
        /// <param name="ids">list of ids</param>
        /// <returns>the records</returns>
        public List<ShapeFileRecord> GetRecords(int[] ids)
        {
            //find the offsets in the .shx file
            Tuple<int, int>[] seekInfo = FindOffsets(ids);
            List<ShapeFileRecord> records = new List<ShapeFileRecord>();
            using (FileStream fileStream = new FileStream(shapeFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int currentposition = 0;
                //based on the offsets extract the records
                foreach (Tuple<int, int> info in seekInfo)
                {
                    byte[] data = new byte[info.Item2];
                    int toSkip = info.Item1 + 8 - currentposition;
                    fileStream.Seek(toSkip, SeekOrigin.Current);
                    fileStream.Read(data, 0, info.Item2);
                    records.Add(ParseRecord(data));
                    currentposition = info.Item1 + info.Item2 + 8;
                    currentposition = (int)fileStream.Position;
                }
            }

            return records;
        }

        /// <summary>
        /// Parses one record from the byte sequence
        /// </summary>
        /// <param name="data">data for the record</param>
        /// <returns>parsed record</returns>
        ShapeFileRecord ParseRecord(byte[] data)
        {
            ShapeFileRecord rec = new ShapeFileRecord();
            switch (header.shapeType)
            {
                case 13: 
                    //Look into the offical shapefile documentation
                    //This just extracts the information based on it
                    int shapeType = BitConverter.ToInt32(data, 0);
                    int numParts = BitConverter.ToInt32(data, 36);
                    int numPoints = BitConverter.ToInt32(data, 40);


                    rec.parts = new List<int>();
                    rec.points = new List<Vector3D>();

                    int endOfParts = 44 + 4 * numParts;
                    int endOfPoints = endOfParts + 16 * numPoints;

                    for (int i = 0; i < numParts; i++)
                    {
                        rec.parts.Add(BitConverter.ToInt32(data, 44 + i * 4));
                    }

                    for (int i = 0; i < numPoints; i++)
                    {
                        double x = BitConverter.ToDouble(data, endOfParts + i * 16);
                        double y = BitConverter.ToDouble(data, endOfParts + i * 16 + 8);
                        double z = BitConverter.ToDouble(data, endOfPoints + 16 + i * 8);
                        rec.points.Add(new Vector3D(x, y, z));
                    }


                    break;
                default:
                    break;
            }
            return rec;
        }

        /// <summary>
        /// Retrieves offsets from the .shx file
        /// </summary>
        /// <param name="ids">ids that we want the offsets for</param>
        /// <returns>tuples of offsets and the length of the record there</returns>
        public Tuple<int, int>[] FindOffsets(int[] ids)
        {
            int offset = 0, lenght = 0;
            Tuple<int, int>[] tuples = new Tuple<int, int>[ids.Length];
            using (FileStream filestream = new FileStream(indexFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int lastID = 0;
                filestream.Seek(headerSize, SeekOrigin.Current);
                int position = 0;
                //find the appropriate position in the .shx file and extract the offset and length
                foreach (int id in ids)
                {
                    int distanceFromTheLastID = indexRecordSize * (id - (lastID) - 1);
                    filestream.Seek(distanceFromTheLastID, SeekOrigin.Current);
                    byte[] recordOffset = new byte[indexRecordSize / 2];
                    byte[] recordLenght = new byte[indexRecordSize / 2];
                    filestream.Read(recordOffset, 0, indexRecordSize / 2);
                    filestream.Read(recordLenght, 0, indexRecordSize / 2);
                    offset = BitConverter.ToInt32(recordOffset.Reverse().ToArray(), 0) * 2;
                    lenght = BitConverter.ToInt32(recordLenght.Reverse().ToArray(), 0) * 2;

                    tuples[position++] = Tuple.Create(offset, lenght);

                    lastID = id;
                }
            }
            return tuples;
        }


        /// <summary>
        /// Parses the shapefile header
        /// </summary>
        void ParseHeader()
        {
            using (FileStream fileStream = new FileStream(shapeFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] fileHeader = new byte[100];
                fileStream.Read(fileHeader, 0, 100);
                header.shapeType = BitConverter.ToInt32(fileHeader, 32);
                header.mins = new Vector3D(BitConverter.ToDouble(fileHeader, 36), BitConverter.ToDouble(fileHeader, 44), BitConverter.ToDouble(fileHeader, 68));
                header.maxs = new Vector3D(BitConverter.ToDouble(fileHeader, 52), BitConverter.ToDouble(fileHeader, 60), BitConverter.ToDouble(fileHeader, 76));
            }
        }

    }
}
