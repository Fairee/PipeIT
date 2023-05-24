using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinGenerator
{
    /// <summary>
    /// Parses a shape file that uses the PolylineZ shapeType 
    /// </summary>
    public class ShapeFileParser
    {
        private ShapeFileHeader header;
        private List<ShapeFileRecord> records;
        string shapeFileName;
        string indexFileName;
        int headerSize = 100;
        int indexRecordSize = 8;

        public List<ShapeFileRecord> GetRecords()
        {
            return records;
        }

        public ShapeFileHeader GetHeader()
        {
            return header;
        }

        public ShapeFileParser(string name)
        {
            this.shapeFileName = name;
            this.indexFileName = name.Substring(0, name.Length - 3) + "shx";
            records = new List<ShapeFileRecord>();
            ParseHeader();

        }

        /// <summary>
        /// Parses one record
        /// </summary>
        /// <param name="data">bytes of the record</param>
        /// <returns>the record parsed into the struct</returns>
        ShapeFileRecord ParseRecord(byte[] data)
        {
            ShapeFileRecord rec = new ShapeFileRecord();
            switch (header.shapeType)
            {
                case 13:
                    int shapeType = BitConverter.ToInt32(data, 0);
                    int numParts = BitConverter.ToInt32(data, 36);
                    int numPoints = BitConverter.ToInt32(data, 40);


                    rec.parts = new List<int>();
                    rec.points = new List<Vector3D>();
                    //This is based on the shapeFile documentation ... look there for more info
                    //We just access the bytes where we know the stuff will be
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
        /// Parses the header of the file
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

        /// <summary>
        /// Parses all of the data inside the shapefile and loads them into memory
        /// </summary>
        public void ParseAllData()
        {
            using (FileStream fileStream = new FileStream(shapeFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int offset = headerSize;
                fileStream.Seek(offset, SeekOrigin.Current);
                //parse records
                while (fileStream.Position < fileStream.Length)
                {
                    byte[] recordNumber = new byte[4];
                    byte[] recordLength = new byte[4];

                    fileStream.Read(recordNumber, 0, 4);
                    fileStream.Read(recordLength, 0, 4);


                    //length is given in 16-bit words ... we want number of words in bytes

                    int length = BitConverter.ToInt32(recordLength.Reverse().ToArray(), 0) * 2;

                    byte[] data = new byte[length];
                    fileStream.Read(data, 0, length);
                    ShapeFileRecord rec = ParseRecord(data);
                    rec.id = BitConverter.ToInt32(recordNumber.Reverse().ToArray(), 0);
                    records.Add(rec);
                }

            }


        }
    }
}