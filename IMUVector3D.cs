using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace L515_Realsense_App
{
    public class IMUVector3D
    {
        public float X; public float Y; public float Z;
        public IMUVector3D(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
        public IMUVector3D()
        {
            X = 0; Y = 0; Z = 0;
        }
        public IMUVector3D(IMUVector3D vector)
        {
            X = vector.X; Y = vector.Y; Z = vector.Z;
        }
    }
}
