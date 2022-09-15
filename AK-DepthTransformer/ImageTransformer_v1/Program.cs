using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

// Kinect SDK using
using Microsoft.Azure.Kinect.Sensor;
using Image = Microsoft.Azure.Kinect.Sensor.Image;
// ImageMagick using --> Get Depth Image
using ImageMagick;
// Json using --> Get Azure Kinect Calibration JSON
using Newtonsoft.Json.Linq;


namespace ImageTransformer_v1
{
    internal class Program
    {
        // Save PNG Image Data
        public static void PNGSave(WriteableBitmap wbitmap, string save_path)
        {
            try
            {
                if (save_path.Contains("png"))
                {
                    using (FileStream stream = new FileStream(save_path, FileMode.Create, FileAccess.ReadWrite))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();

                        encoder.Frames.Add(BitmapFrame.Create(wbitmap));
                        encoder.Save(stream);
                    }
                }
            }
            catch
            {
                Thread.Sleep(3);
            }
        }

        // Reform Calibration Data
        public static Calibration ReformCalibration(string kif_path, Calibration calibration)
        {
            string json_str = null;
            using (StreamReader json_sr = new System.IO.StreamReader(kif_path))
            {
                json_str = json_sr.ReadToEnd();
                json_sr.Close();
            }

            JObject jRoot = JObject.Parse(json_str);
            JToken contents_token = jRoot["contents"];

            JToken color_ext_token = contents_token[0];
            JToken color_ext_parms_token = color_ext_token["parameters"][0];
            var color_ext_rotation_array = color_ext_parms_token["rotation"].ToArray();
            var color_ext_translation_array = color_ext_parms_token["translation"].ToArray();

            float[] color_ext_rotation = new float[]
            {
                (float)color_ext_rotation_array[0],
                (float)color_ext_rotation_array[1],
                (float)color_ext_rotation_array[2],
                (float)color_ext_rotation_array[3],
                (float)color_ext_rotation_array[4],
                (float)color_ext_rotation_array[5],
                (float)color_ext_rotation_array[6],
                (float)color_ext_rotation_array[7],
                (float)color_ext_rotation_array[8]
            };
            float[] color_ext_translation = new float[]
            {
                (float)color_ext_translation_array[0],
                (float)color_ext_translation_array[1],
                (float)color_ext_translation_array[2]
            };
            
            JToken color_ins_token = contents_token[1];
            var color_ins_array = color_ins_token["Parameters"][0]["color"].ToArray();
            var depth_ins_array = color_ins_token["Parameters"][1]["depth"].ToArray();

            float[] color_ins = new float[]
            {
                (float)color_ins_array[0],
                (float)color_ins_array[1],
                (float)color_ins_array[2],
                (float)color_ins_array[3],
                (float)color_ins_array[4],
                (float)color_ins_array[5],
                (float)color_ins_array[6],
                (float)color_ins_array[7],
                (float)color_ins_array[8],
                (float)color_ins_array[9],
                (float)color_ins_array[10],
                (float)color_ins_array[11],
                (float)color_ins_array[12],
                (float)color_ins_array[13],
                (float)color_ins_array[14]
            };
            float[] depth_ins = new float[]
            {
                (float)depth_ins_array[0],
                (float)depth_ins_array[1],
                (float)depth_ins_array[2],
                (float)depth_ins_array[3],
                (float)depth_ins_array[4],
                (float)depth_ins_array[5],
                (float)depth_ins_array[6],
                (float)depth_ins_array[7],
                (float)depth_ins_array[8],
                (float)depth_ins_array[9],
                (float)depth_ins_array[10],
                (float)depth_ins_array[11],
                (float)depth_ins_array[12],
                (float)depth_ins_array[13],
                (float)depth_ins_array[14]
            };

            var metric_radius = color_ext_parms_token["metricRadius"].ToString();

            calibration.ColorCameraCalibration.Extrinsics.Rotation = color_ext_rotation;
            calibration.ColorCameraCalibration.Extrinsics.Translation = color_ext_translation;
            calibration.ColorCameraCalibration.MetricRadius = (float)metric_radius[0];
            calibration.ColorCameraCalibration.Intrinsics.Parameters = color_ins;

            calibration.DepthCameraCalibration.Extrinsics.Rotation = new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            calibration.DepthCameraCalibration.Extrinsics.Translation = new float[] { 0, 0, 0 };
            calibration.DepthCameraCalibration.MetricRadius = (float)metric_radius[0];
            calibration.DepthCameraCalibration.Intrinsics.Parameters = depth_ins;

            var device_ext = calibration.DeviceExtrinsics;

            device_ext[1].Rotation = color_ext_rotation;
            device_ext[1].Translation = color_ext_translation;
            
            return calibration;
        }

        // Azure Kinect Open & Setting
        public static Calibration AzureKinectReady()
        {
            Console.WriteLine("Device Open...");

            int kinect_cnt = Device.GetInstalledCount();
            Calibration calibration = new Calibration();
            if (kinect_cnt > 0)
            {
                Device kinect_device = Device.Open(0);
                DeviceConfiguration device_config = new DeviceConfiguration();
                device_config.DepthMode = DepthMode.NFOV_Unbinned;
                device_config.ColorResolution = ColorResolution.R1536p;
                device_config.CameraFPS = FPS.FPS15;

                calibration = kinect_device.GetCalibration(device_config.DepthMode, device_config.ColorResolution);

                Console.WriteLine("Successfully Open Azure Kinect Device!");
            }
            else
            {
                Console.WriteLine("Fail to Open Azure Kinect Device!\nCheck Device Connection!");
                Environment.Exit(0);
            }
            return calibration;
        }

        static void Main(string[] args)
        {
            // Program Target Setting
            var root_path = @"D:\AzureKinectData";
            var cap_date = "220708";
            var subject_id = "c0062";
            var record_num = "00";
            var kinect_loc = "kf";
            var di_folder = "2_DepthImage";
            var ki_folder = "6_CameraInfo";
            var trg_folder = "7_TransformedDepthImage";

            var dif_path = Path.Combine(root_path, cap_date, subject_id, record_num, di_folder, kinect_loc);
            var tif_path = Path.Combine(root_path, cap_date, subject_id, record_num, trg_folder, kinect_loc);
            var kif_path = Path.Combine(root_path, cap_date, subject_id, record_num, ki_folder);
            var kid_path = Path.Combine(kif_path, subject_id + "_" + cap_date + "_" + kinect_loc + "_" + record_num + "_camerainfo.json");

            DirectoryInfo ki_di = new DirectoryInfo(tif_path);
            if (!ki_di.Exists)
            {
                ki_di.Create();
            }

            // Azure Kinect Setting
            Calibration calibration = AzureKinectReady();
            Calibration re_calibration = ReformCalibration(kid_path, calibration);
            Transformation tr = new Transformation(re_calibration);

            // Transformation Working
            Console.WriteLine("Working Transformation\nPlease Wait few minute...");

            DirectoryInfo di = new DirectoryInfo(@dif_path);
            FileInfo[] depth_imgs = di.GetFiles("*.png");

            Parallel.ForEach(depth_imgs, depth_img =>
            {
                string d_img = depth_img.ToString();
                var dimg_path = Path.Combine(dif_path, d_img);

                var trdepth_img = d_img.Replace("Depth", "TrDepth");
                var trimg_path = Path.Combine(tif_path, trdepth_img);

                MagickImage org_depth_image = new MagickImage(dimg_path);
                ushort[] pixelValues = org_depth_image.GetPixels().ToArray();
                int stride = org_depth_image.Width * sizeof(UInt16);
                Image depth_kinect_image = new Image(ImageFormat.Depth16, org_depth_image.Width, org_depth_image.Height, stride);
                int sizeX = org_depth_image.Width;
                int sizeY = org_depth_image.Height;

                int count = 0;
                for (int y = 0; y < sizeY; y++) {
                    for (int x = 0; x < sizeX; x++) {

                        depth_kinect_image.SetPixel(y, x, pixelValues[count]);
                        count++;
                    }
                }

                Image tr_img = tr.DepthImageToColorCamera(depth_kinect_image);
                byte[] tr_buff = tr_img.Memory.ToArray();

                Int32Rect rect = new Int32Rect(0, 0, tr_img.WidthPixels, tr_img.HeightPixels);

                WriteableBitmap wbitmap = new WriteableBitmap(
                    tr_img.WidthPixels, tr_img.HeightPixels,
                    96.0, 96.0,
                    System.Windows.Media.PixelFormats.Gray16, null);

                wbitmap.WritePixels(rect, tr_buff, tr_img.StrideBytes, 0, 0);
                wbitmap.Freeze();

                PNGSave(wbitmap, trimg_path);
            });
        }
    }
}
