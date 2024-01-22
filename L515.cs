using System.Drawing;
using Intel.RealSense;


namespace L515_Realsense_App
{
    public class L515
    {
        private const double MINRANGE = 0.1;
        private const double MAXRANGE = 10;

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

        public void OpenConnection()
        {
            _pipe = new Pipeline();
            _config = new Config();
            _config.EnableStream(Intel.RealSense.Stream.Any, _width, _height, Intel.RealSense.Format.Y16, _framerate);

            _profile = _pipe.Start(_config);

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

        //TODO
        public void InitializeDeprojectionMatrix()
        {
            this.GetIntrinistics();

            //Create deproject row/column vector
        }

        public void GetFrame() //musi robic return
        {
            FrameSet frames = _pipe.WaitForFrames();

            DepthFrame depth_frame = frames.DepthFrame;
            VideoFrame color_frame = frames.ColorFrame;

            if (depth_frame != null)
            {
                Console.WriteLine("depth frame aquired");
                ushort[,] frame2dBytes = ReadDepthFrame(depth_frame);
                //CreateBitmap(frame2dBytes);
            }
            if (color_frame != null)
            {
                Console.WriteLine("Color frame aquired");
                byte[,] colorFrame2dBytes = ReadColorFrame(color_frame);
                //for(int i = 0; i < colorFrame2dBytes.GetLength(0); i++)
                //{
                //    for(int j = 0; j < colorFrame2dBytes.GetLength(1); j++)
                //    {
                        
                //    }
                //}
                CreateImageBitmap(colorFrame2dBytes);
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
                        frame2dBytes[i, j] = depth_data[counter];
                        counter++;
                    }
                }
            }
            return frame2dBytes;
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

    }
}
