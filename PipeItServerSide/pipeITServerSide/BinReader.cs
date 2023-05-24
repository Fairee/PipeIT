using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace pipeIT
{
    [Serializable]
    public struct binHeader
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

    public class BinReader
    {
        binHeader header;
        string fileName = "C:\\data\\testOther.bin";
        int amountOfZonesLon;
        int amountOfZonesLat;
        int amountOfAreasLon;
        int amountOfAreasLat;
        int headersize = 72;
        int packetSize = 5;
        int intSize = 4;




        public BinReader()
        {
            header = new binHeader();
            ParseHeader();
        }

        public binHeader GetHeader()
        {
            return header;
        }
        /// <summary>
        /// Parses the header based on the specifications in the documentation (BinReader.PDF)
        /// </summary>
        private void ParseHeader()
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] headerByte = new byte[headersize];
                fileStream.Read(headerByte, 0, headersize);
                header.meterLat = BitConverter.ToDouble(headerByte, 0);
                header.meterLong = BitConverter.ToDouble(headerByte, 8);
                header.zoneSizeInMeters = BitConverter.ToInt32(headerByte, 16);
                header.areaSizeInMeters = BitConverter.ToInt32(headerByte, 20);
                header.fromLon = BitConverter.ToDouble(headerByte, 24);
                header.toLon = BitConverter.ToDouble(headerByte, 32);
                header.fromLat = BitConverter.ToDouble(headerByte, 40);
                header.toLat = BitConverter.ToDouble(headerByte, 48);
                header.amountOfZonesLon = BitConverter.ToInt32(headerByte, 56);
                header.amountOfZonesLat = BitConverter.ToInt32(headerByte, 60);
                header.amountOfAreasLon = BitConverter.ToInt32(headerByte, 64);
                header.amountOfAreasLat = BitConverter.ToInt32(headerByte, 68);

                amountOfZonesLon = header.amountOfZonesLon;
                amountOfZonesLat = header.amountOfZonesLat;

                amountOfAreasLon = header.amountOfAreasLon;
                amountOfAreasLat = header.amountOfAreasLat;
            }

        }

        /// <summary>
        /// retrieves the ids that lies in the cell based on the cells indexes
        /// </summary>
        /// <param name="zoneLatIndex">cell zone latitude index</param>
        /// <param name="zoneLonIndex">cell zone longitude index</param>
        /// <param name="areaLatIndex">cell area latitude index</param>
        /// <param name="areaLonIndex">cell area longitude index</param>
        /// <returns></returns>
        public List<int> GetByIndexes( int zoneLatIndex, int zoneLonIndex, int areaLatIndex, int areaLonIndex)
        {
            //Check the documentation to understand this better
            List<int> ids = new List<int>();
            if (zoneLonIndex >= amountOfZonesLon || zoneLatIndex >= amountOfZonesLat || areaLonIndex >= amountOfAreasLon || areaLatIndex >= amountOfAreasLat ||
                zoneLonIndex < 0 || zoneLatIndex < 0 || areaLonIndex < 0 || areaLatIndex < 0)
            {
                return ids;
            }


            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // find the offset of the zone
                int offset = (zoneLonIndex * amountOfZonesLat + zoneLatIndex) * packetSize + headersize;
                fileStream.Seek(offset, SeekOrigin.Current);


                byte[] packet = new byte[packetSize];
                //checks the first byte to see if there are any pipes leading through the zone
                fileStream.Read(packet, 0, packetSize);
                if (packet[0] == 0)
                {
                    return ids;
                }
                //if there are, get the offset for the areas
                int areaOffset = BitConverter.ToInt32(packet, 1);
                //skip area packets that are before the one we are looking for
                int inAreaOffset = (areaLonIndex * amountOfAreasLat + areaLatIndex) * packetSize; 
                //get the total offset
                int totalAreaOffset = areaOffset + inAreaOffset;
                //count by how much do I have to move to get to the new position (-packetSize for the packet ive just read)
                int nextOffset = totalAreaOffset - offset - packetSize;
                fileStream.Seek(nextOffset, SeekOrigin.Current);
                //checks the first byte to see if there are any pipes leading through the area
                fileStream.Read(packet, 0, packetSize);
                if (packet[0] == 0)
                {
                    return ids;
                }
                //if there are, get the offset for the areas
                int IDsOffset = BitConverter.ToInt32(packet, 1);
                //count by how much do I have to move to get to the new position (-packetSize for the packet ive just read)
                nextOffset = IDsOffset - totalAreaOffset - packetSize; 

                //extract the ids based on the offset
                byte[] length = new byte[intSize];
                fileStream.Seek(nextOffset, SeekOrigin.Current);

                fileStream.Read(length, 0, intSize);

                int IDsLenght = BitConverter.ToInt32(length, 0);

                byte[] idsByte = new byte[IDsLenght * intSize];
                fileStream.Read(idsByte, 0, IDsLenght * intSize);
                for (int i = 0; i < IDsLenght; i++)
                {
                    ids.Add(BitConverter.ToInt32(idsByte, i * 4));
                }

            }
            return ids;
        }
    }
}
