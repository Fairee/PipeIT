using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace pipeIT
{
    [Route("api/[controller]")]
    [ApiController]
    public class PipeItServer : ControllerBase
    {
        public class IdsModel
        {
            public int[] Ids { get; set; }
        }

        private readonly BinReader binReader;
        private readonly SHPreader shpReader;
        private readonly DBFreader dbfReader;

        public PipeItServer(BinReader bin, SHPreader shp, DBFreader dbf)
        {
            binReader = bin;
            shpReader = shp;
            dbfReader = dbf;
        }

        /// <summary>
        /// A Post request to get the dbf records based on the provided ids
        /// </summary>
        /// <param name="model">list of ids</param>
        /// <returns>serialized list of records</returns>
        [HttpPost("DBFPostData")]
        public IActionResult Test([FromBody] IdsModel model)
        {
            int[] ids = model.Ids;


            List<string>[] list = dbfReader.GetRecordsData(ids);
            string json = JsonSerializer.Serialize(list);
            return Ok(json);
        }
        /// <summary>
        /// A POST request to get the coordinates of the points for the pipes
        /// </summary>
        /// <param name="model">the list of ids of the pipes</param>
        /// <returns>serialized list of points in fromat [[x0,y0,z0, x1,y1,z1 .... xn,yn,zn], [the same for id2].... [the same for idn]]</returns>
        [HttpPost("SHPPostData")]
        public IActionResult GetSHPData([FromBody] IdsModel model)
        {
            int[] ids = model.Ids;

            List<double>[] points = new List<double>[ids.Length];
            int counter = 0;
            foreach (SHPreader.ShapeFileRecord rec in shpReader.GetRecords(ids))
            {
                points[counter] = new List<double>();
                foreach (Vector3D point in rec.points)
                {
                    points[counter].Add(point.x);
                    points[counter].Add(point.y);
                    points[counter].Add(point.z);
                }
                counter++;
            }
            string json = JsonSerializer.Serialize(points);
            return Ok(json);
        }

        /// <summary>
        /// A GET request to get the field names
        /// </summary>
        /// <returns>serialized list of the field names</returns>
        [HttpGet("DBFfieldNames")]
        public IActionResult GetFieldNames()
        {
            List<string> fieldData = new List<string>();
            foreach (DBFreader.DBFfield field in dbfReader.fieldinfo)
            {
                fieldData.Add(field.name);
            }
            string json = JsonSerializer.Serialize(fieldData);
            return Ok(json);
        }


        /// <summary>
        /// A GET request to get the bin header
        /// </summary>
        /// <returns>serialized bin header</returns>
        [HttpGet("BinHeader")]
        public IActionResult GetBinHeader()
        {
            binHeader header = binReader.GetHeader();
            string json = JsonSerializer.Serialize(header);
            return Ok(json);
        }
        /// <summary>
        /// A GET request for the ids in a given cell
        /// </summary>
        /// <param name="zoneLatIndex">cell zone latitude index</param>
        /// <param name="zoneLonIndex">cell zone longitude index</param>
        /// <param name="areaLatIndex">cell area latitude index</param>
        /// <param name="areaLonIndex">cell area longitude index</param>
        /// <returns></returns>
        [HttpGet("GetIds")]
        public IActionResult GetIds( [FromQuery] int zoneLatIndex, [FromQuery] int zoneLonIndex, [FromQuery] int areaLatIndex, [FromQuery] int areaLonIndex)
        {
            List<int> ids = binReader.GetByIndexes( zoneLatIndex, zoneLonIndex, areaLatIndex, areaLonIndex);
            string json = JsonSerializer.Serialize(ids);
            return Ok(json);

        }


    }
}
