using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace pipeIT
{
    public class Vector3D
    {
        public double x, y, z;

        public Vector3D()
        {
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

        public static Vector3D operator -(Vector3D A, Vector3D B)
        {
            Vector3D ret = new Vector3D();
            ret.x = A.x - B.x;
            ret.y = A.y - B.y;
            ret.z = A.z - B.z;
            return ret;
        }

        public override string ToString()
        {
            return "x: " + x + " y: " + y + " z: " + z + "\n";
        }

    }
}
