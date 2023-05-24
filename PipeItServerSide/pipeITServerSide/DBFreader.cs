using System.Globalization;
using System.Text;

namespace pipeIT
{
    public class DBFreader
    {
        private int headerSize;
        private int recordSize;
        private int numberOfRecords;
        private string filename = "C:\\data\\TMISPRUBEH_L.dbf";
        Encoding enc = Encoding.UTF8;
        public List<DBFfield> fieldinfo = new List<DBFfield>();

        public DBFreader()
        {
            ParseHeader();
            ParseFieldData();
        }

        public struct DBFfield
        {
            public string name;
            public int size;
            public char type;
        }

        public struct DBFPacket
        {
            public int id;
            public List<string> data;
        }
        /// <summary>
        /// Get records from the dbf file that correspond to the provided ids
        /// </summary>
        /// <param name="ids">ids of the pipes we want to get the data about</param>
        /// <returns>the data of the pipe</returns>
        public List<string>[] GetRecordsData(int[] ids)
        {
            List<string>[] data = new List<string>[ids.Length];
            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int lastID = 0;
                //skip header
                fileStream.Seek(headerSize, SeekOrigin.Current);
                int position = 0;
                //retireve the data for each id
                foreach (int id in ids)
                {
                    int distanceFromTheLastID = recordSize * (id - (lastID) - 1);
                    fileStream.Seek(distanceFromTheLastID, SeekOrigin.Current);
                    byte[] record = new byte[recordSize];
                    fileStream.Read(record, 0, recordSize);
                    data[position++] = ExtractData(record);
                    lastID = id;
                }
            }
            return data;
        }
        /// <summary>
        /// Parses the DBF header
        /// </summary>
        private void ParseHeader()
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] header = new byte[32];
                fileStream.Read(header, 0, 32);
                numberOfRecords = BitConverter.ToInt32(header, 4);
                headerSize = BitConverter.ToInt16(header, 8);
                recordSize = BitConverter.ToInt16(header, 10);
            }
        }

        /// <summary>
        /// Parses the information about the field data
        /// </summary>
        private void ParseFieldData()
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int toSkip = 32;
                fileStream.Seek(toSkip, SeekOrigin.Current);
                int amountOfFields = (headerSize - 32) / 32;
                byte[] fieldData = new byte[32];
                for (int i = 0; i < amountOfFields; i++)
                {
                    fileStream.Read(fieldData, 0, 32);
                    DBFfield f = new DBFfield();
                    f.name = enc.GetString(fieldData, 0, 11).TrimEnd('\0');
                    f.type = (char)fieldData[11];
                    f.size = (int)fieldData[16];
                    fieldinfo.Add(f);
                }
            }
        }
        /// <summary>
        /// Extract data from the byte sequence of one record
        /// </summary>
        /// <param name="record">byte sequence of the record</param>
        /// <returns>list of the individual attributes</returns>
        private List<string> ExtractData(byte[] record)
        {
            List<string> data = new List<string>();
            if (record[0] == 0x2A)
            {
                return null;
            }

            int offset = 1;
            foreach (DBFfield field in fieldinfo)
            {
                string value;
                //check whats inside based on the type
                // this isnt really necessary as we are returning everything as a string anyway
                // but I decided to keep it if its ever necessary
                switch (field.type)
                {
                    case 'C':
                        value = enc.GetString(record, offset, field.size).TrimEnd('\0');
                        break;
                    case 'D':
                        value = enc.GetString(record, offset, field.size).TrimEnd('\0');
                        // Convert the value to a DateTime object
                        DateTime dateValue;
                        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                        {
                            value = dateValue.ToString("yyyy-MM-dd");
                        }
                        break;
                    case 'F':
                        value = enc.GetString(record, offset, field.size).TrimEnd('\0');
                        // Convert the value to a float
                        float floatValue;
                        if (float.TryParse(value, out floatValue))
                        {
                            value = floatValue.ToString();
                        }
                        break;
                    case 'N':
                        value = enc.GetString(record, offset, field.size).TrimEnd('\0');
                        // Convert the value to a decimal
                        decimal decimalValue;
                        if (decimal.TryParse(value, out decimalValue))
                        {
                            value = decimalValue.ToString();
                        }
                        break;
                    case 'L':
                        value = enc.GetString(record, offset, field.size).TrimEnd('\0');
                        // Convert the value to a boolean
                        bool boolValue;
                        if (value == "T" || value == "t" || value == "Y" || value == "y")
                        {
                            boolValue = true;
                        }
                        else
                        {
                            boolValue = false;
                        }
                        value = boolValue.ToString();
                        break;
                    case 'M':
                        // Memo data is not supported 
                        value = "[memo]";
                        break;
                    default:
                        value = "";
                        break;
                }

                data.Add(value);
                offset += field.size;
            }
            return data;
        }


    }
}
