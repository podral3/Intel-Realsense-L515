using Intel.RealSense;

namespace L515_Realsense_App
{
    public class Program
    {
        static int Main()
        {

            L515 device = new L515();
            device.OpenConnection(streamType.color);
            device.GetVideoFrame();
            return 0;
        }
    }
}
