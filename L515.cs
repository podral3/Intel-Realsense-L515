using System.Drawing;
using Intel.RealSense;
using System.Linq;


namespace L515_Realsense_App
{
    public enum streamType
    {
        color,
        depth,
        motion
    }
    public class L515
    {
        private const double MINRANGE = 0.1;
        private const double MAXRANGE = 10;
        private float _depth_scale;

        private int _width;
        private int _height;
        private int _framerate;

        private bool _isConnectionOpen = false;

        private float _scale;

        private float[] _fov;


        //realsense variables
        private Pipeline _pipe;
        private Config? _config;
        private PipelineProfile? _profile;

        private Intrinsics _intrin;
        //TODO
        //deprojection


        
        public L515(int width = 640, int heigth = 480, int framerate = 30)
        {
            _width = width;
            _height = heigth;
            _framerate = framerate;

            _scale = 1;

            _fov = new float[2] { 0, 0 };
        }
        //possible formats:
        //Depth: Z16, Y8, RAW8
        //RGB Camera: YUYV, BRG8, RGBA8, BGRA8, Y8, Y16
        public void OpenConnection(streamType streamType)
        {
            _pipe = new Pipeline();
            _config = new Config();
            switch (streamType)
            {
                case streamType.color:
                    _config.EnableStream(Intel.RealSense.Stream.Color, _width, _height, Intel.RealSense.Format.Rgb8, _framerate);
                    break; 
                case streamType.depth:
                    _config.EnableStream(Intel.RealSense.Stream.Depth, _width, _height, Intel.RealSense.Format.Z16, _framerate);
                    break;
                case streamType.motion:
                    break;
            } 
           

            _profile = _pipe.Start(_config);
            _depth_scale =  _profile.Device.Sensors.First().DepthScale;

            Console.WriteLine($"{_profile.Device.Info.GetInfo(CameraInfo.Name)}");
            _isConnectionOpen = true;
        }

        public void CloseConnection()
        {
            if (_isConnectionOpen)
            {
                _pipe.Stop();

                Console.WriteLine("pipeline stopped");
            }
        }

        //cokolwiek to znaczy xD
        public void GetIntrinistics()
        {
            _profile = _pipe.ActiveProfile;

            _intrin = _profile.GetStream(Intel.RealSense.Stream.Depth).As<VideoStreamProfile>().GetIntrinsics();
            _scale = _profile.Device.Sensors[0].DepthScale;

            //fov w stopniach, chyba trzeba przekonwertować do radianów
            _fov = _intrin.FOV;

        }


        public void GetDepthFrame() //musi robic return
        {
            FrameSet frames = _pipe.WaitForFrames();

            DepthFrame depth_frame = frames.DepthFrame;
            

            if (depth_frame != null)
            {
                Console.WriteLine("depth frame aquired");
                ushort[,] frame2dBytes = ReadDepthFrame(depth_frame);
                float[,] depth_map = CreateDepthMap(frame2dBytes);
            }
        }

        public void StreamFrames()
        {
            if(_pipe != null)
            {
                while (true)
                {
                    if (_pipe.PollForFrames(out FrameSet frameSet))
                    {
                        using (frameSet)
                        {
                            DepthFrame frame = frameSet.DepthFrame;
                            ushort[,] frame2dBytes = ReadDepthFrame(frame);
                            float[,] depth_map = CreateDepthMap(frame2dBytes);
                            WriteMaxDepthInfo(depth_map);

                        }
                    } 
                }
    
            }
        }

        public void GetVideoFrame()
        {
            FrameSet frames = _pipe.WaitForFrames();

            VideoFrame video_frame = frames.ColorFrame;

            if (video_frame != null)
            {
                Console.WriteLine("Video frame aquired");
                byte[,] colorFrame2dBytes = ReadColorFrame(video_frame);
            }
        }

        private void CreateImageBitmap(byte[,] pixels)
        {
            Bitmap bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
            for(int i = 0; i < _width; i++)
            {
                for(int j = 0; j < _height; j++)
                {
                    Color color = Color.FromArgb(pixels[i, j], 0,0);
                    bitmap.SetPixel(i, j, color);
                    
                }
            }
            bitmap.Save("bitmapTest.jpeg");
            Console.WriteLine("Image Saved");
            //return new Bitmap();
        }

        private void WriteMaxDepthInfo(float[,] depth_map)
        {
            float max = depth_map.Cast<float>().Max();
            float min = depth_map.Cast<float>().Min();
            Console.WriteLine($"Min {min} Max {max}");
        }
        
        //need to reverse order of bytes for each pixel
        private ushort[,] ReadDepthFrame(DepthFrame depth_frame)
        {
            ushort[,] frame2dBytes = new ushort[depth_frame.Width, depth_frame.Height];
            int counter = 0;
            unsafe
            {
                ushort* depth_data = (ushort*)depth_frame.Data.ToPointer();

                for (int i = 0; i < depth_frame.Width; i++)
                {
                    for (int j = 0; j < depth_frame.Height; j++)
                    {
                        frame2dBytes[i, j] = ReverseBytes(depth_data[counter]);
                        counter++;
                    }
                }
            }
            return frame2dBytes;
        }
        //bytes are in little endian (reverse order)
        private float[,] CreateDepthMap(ushort[,] depth_frame)
        {
            float[,] depth_map = new float[depth_frame.GetLength(0), depth_frame.GetLength(1)];
            for(int i = 0; i < depth_frame.GetLength(0); i++)
            {
                for(int j = 0; j < depth_frame.GetLength(1); j++)
                {
                    depth_map[i,j] = depth_frame[i,j] * (float)_depth_scale; 
                }
            }
            return depth_map;
        }
        
        private byte[,] ReadColorFrame(VideoFrame video_frame)
        {
            byte[,] frame2dBytes = new byte[video_frame.Width, video_frame.Height];
            int counter = 0;
            unsafe
            {
                byte* depth_data = (byte*)video_frame.Data.ToPointer();

                for (int i = 0; i < video_frame.Width; i++)
                {
                    for (int j = 0; j < video_frame.Height; j++)
                    {
                        frame2dBytes[i, j] = depth_data[counter];
                        counter++;
                    }
                }
            }
            return frame2dBytes;
        }
        //huh raz trzeba odwrócić raz nie, do zbadania
        private ushort ReverseBytes(ushort numberToReverse)
        {
            if (!BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(numberToReverse);
                return BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0); 
            }
            return numberToReverse;
        }
    }
}
