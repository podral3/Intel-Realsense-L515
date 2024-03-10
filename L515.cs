using System.Drawing;
using Intel.RealSense;
using System.Linq;
using System.Runtime.InteropServices;
using System.Numerics;

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
        private const double MAXRANGE = 15;
        private float _depth_scale;

        private int _width;
        private int _height;
        private int _framerate;

        private bool _isConnectionOpen = false;

        private float _scale;

        private float[] _fov;

        private Vector3 _gyro;
        private Vector3 _acc;
        private Vector3 _cameraPosition;


        //realsense variables
        private Pipeline _pipe;
        private Config? _config;
        private PipelineProfile? _profile;

        private Intrinsics _intrin;
        //TODO
        //deprojection


        
        public L515(int width = 1024, int heigth = 768, int framerate = 30)
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
                    _config.EnableStream(Intel.RealSense.Stream.Color, _width, _height, Intel.RealSense.Format.Rgba8, _framerate);
                    break; 
                case streamType.depth:
                    _config.EnableStream(Intel.RealSense.Stream.Depth, _width, _height, Intel.RealSense.Format.Z16, _framerate);
                    break;
                case streamType.motion:
                    _config.EnableStream(Intel.RealSense.Stream.Pose, _width, _height, Intel.RealSense.Format.MotionXyz32f, _framerate);
                    break;
            }

            _config.EnableAllStreams();
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
                float[,] normalizedDepthMap = NormalizeDepthMap(depth_map);
                PrintDepthMapMonochrome(normalizedDepthMap);
                Console.WriteLine("heh");
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
        public void StreamVideoFrames()
        {
            if (_pipe != null)
            {
                while (true)
                {
                    if (_pipe.PollForFrames(out FrameSet frameSet))
                    {
                        using (frameSet)
                        {
                            VideoFrame frame = frameSet.ColorFrame;
                            ReadColorFrame(frame);

                        }
                    }
                }

            }
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

                for (int i = 0; i < depth_frame.Height; i++)
                {
                    for (int j = 0; j < depth_frame.Width; j++)
                    {
                        frame2dBytes[j, i] = ReverseBytes(depth_data[counter]);
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

        private float[,] NormalizeDepthMap(float[,] depth_map)
        {
            float[,] normalized = new float[depth_map.GetLength(0), depth_map.GetLength(1)];
            float max = depth_map.Cast<float>().Max();
            float min = depth_map.Cast<float>().Min();
            for (int i = 0; i < depth_map.GetLength(0); i++)
            {
                for (int j = 0; j < depth_map.GetLength(1); j++)
                {
                    float value = (depth_map[i, j] - min) / (max - min);
                    normalized[i, j] = value;
                }
            }
            return normalized;
        }

        private void PrintDepthMapMonochrome(float[,] normalizedDepthMap)
        {
            Bitmap bitmap = new Bitmap(normalizedDepthMap.GetLength(0), normalizedDepthMap.GetLength(1));
            for (int i = 0; i < normalizedDepthMap.GetLength(0); i++)
            {
                for (int j = 0; j < normalizedDepthMap.GetLength(1); j++)
                {
                    int colorVal = (int)(normalizedDepthMap[i, j] * 255.0f);
                    Color pixelColor = Color.FromArgb(255, colorVal, colorVal, colorVal);
                    bitmap.SetPixel(i, j, pixelColor);
                }
            }
            bitmap.Save("Bitmap.png");
        }
        
        private int[,] ReadColorFrame(VideoFrame video_frame)
        {
            int[,] frame2dBytes = new int[video_frame.Width, video_frame.Height];
            Bitmap bitmap = new Bitmap(video_frame.Width, video_frame.Height);
            int counter = 0;
            int stride = video_frame.Stride;
            unsafe
            {
                int* video_data = (int*)video_frame.Data.ToPointer();
                //najpierw wczytać wiersz potem kolumne
                for (int i = 0; i < video_frame.Height; i++) //kolumna 0
                {
                    for (int j = 0; j < video_frame.Width; j++) //wiersz 0
                    {
                        frame2dBytes[j, i] = ReverseBytes(video_data[counter]);
                        byte[] bytes = BitConverter.GetBytes(ReverseBytes(video_data[counter]));
                        Color pixelColor = Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
                        bitmap.SetPixel(j, i, pixelColor);
                        counter++;
                    }
                }
            }
            bitmap.Save("bruh.png");
            return frame2dBytes;
        }

        public void ReadIMUFrame()
        {
            while (true)
            {
                using (var frameset = this._pipe.WaitForFrames())
                {
                    var gyroFrame = frameset.FirstOrDefault<Frame>(Intel.RealSense.Stream.Gyro, Format.MotionXyz32f).DisposeWith(frameset);
                    Vector3 gyroCoordinates = ReadSensorBytes(gyroFrame.Data);
                    this._gyro = new Vector3(gyroCoordinates.X, gyroCoordinates.Y, gyroCoordinates.Z);

                    var accelFrame = frameset.FirstOrDefault<Frame>(Intel.RealSense.Stream.Accel, Format.MotionXyz32f).DisposeWith(frameset);
                    Vector3 accelCoordinates = ReadSensorBytes(accelFrame.Data);
                    this._acc = new Vector3(accelCoordinates.X, accelCoordinates.Y, accelCoordinates.Z);
                }
            }
        }

        public void TryMadgwick()
        {
            MadgwickFilter madgwic = new MadgwickFilter();
            while (true)
            {
                using (var frameset = this._pipe.WaitForFrames())
                {
                    var gyroFrame = frameset.FirstOrDefault<Frame>(Intel.RealSense.Stream.Gyro, Format.MotionXyz32f).DisposeWith(frameset);
                    Vector3 gyroCoordinates = ReadSensorBytes(gyroFrame.Data);
                    this._gyro = new Vector3(gyroCoordinates.X, gyroCoordinates.Y, gyroCoordinates.Z);

                    var accelFrame = frameset.FirstOrDefault<Frame>(Intel.RealSense.Stream.Accel, Format.MotionXyz32f).DisposeWith(frameset);
                    Vector3 accelCoordinates = ReadSensorBytes(accelFrame.Data);
                    this._acc = new Vector3(accelCoordinates.X, accelCoordinates.Y, accelCoordinates.Z);

                    Quaternion q = madgwic.IMU_Filter(_acc.X, _acc.Y, _acc.Z, _gyro.X, _gyro.Y, _gyro.Z);
                    Vector3 eulerAngles = madgwic.EulerAngles(q);
                    Console.WriteLine($"Yaw: {eulerAngles.X} Pitch: {eulerAngles.Y} Roll: {eulerAngles.Z}");
                }
            }
        }
        public void TestMadgwick()
        {
            MadgwickFilter filter = new MadgwickFilter();
            for (int i = 0; i < 1000; i++)
            {
                Quaternion q = filter.IMU_Filter(0.05f, 0.05f, 0.9f, 0, 0, 0);
                Vector3 euler = filter.EulerAngles(q);
                Console.WriteLine($"X: {euler.X} Y: {euler.Y} Z: {euler.Z}");
            }
        }
        private Vector3 ReadSensorBytes(IntPtr data)
        {
            Vector3 coords = new Vector3();
            unsafe
            {
                float* floatPtr = (float*)data; 

                coords.X = *floatPtr;
                coords.Y= *(floatPtr +1);
                coords.Z = *(floatPtr +2);
            }
            return coords;
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
        //NIE ODWRACA POPRAWNIE, COŚ Z CASTOWANIEM
        private int ReverseBytes(int numberToReverse)
        {
            if (!BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(numberToReverse);
                byte[] reversed = bytes.Reverse().ToArray();
                return BitConverter.ToUInt16(reversed, 0);
            }
            return numberToReverse;
        }
    }
}
