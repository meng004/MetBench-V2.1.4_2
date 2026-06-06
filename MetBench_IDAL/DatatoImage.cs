
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.IO;


namespace MetBench_IDAL
{
    public class DatatoImage
    {
        //解压文件的存储路径（默认系统的Temp路径）
        string SystemTemppath = Path.GetTempPath();
        //存储路径下创建的存储文件夹名
        string folderunderpath = "temp_image";
        public string ConvertByteArrayToImage(byte[] imageData)
        {
            if (!(imageData is byte[]))
            {
                Console.WriteLine("图片数据不是字节数组。");
                return string.Empty;
            }
            if (imageData == null || imageData.Length == 0)
            {
                Console.WriteLine("图片数据为空。");
                return string.Empty;
            }

            // 为图片创建一个唯一的文件名
            string fileName = Guid.NewGuid().ToString() + ".png";

            // 获取Temp目录的路径
            string tempDirectory = Path.Combine(SystemTemppath, folderunderpath);

            // 如果Image文件夹不存在，则创建它
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            // 构造完整的文件路径
            string imagePath = Path.Combine(tempDirectory, fileName);

            try
            {
                // 将图片数据写入文件
                File.WriteAllBytes(imagePath, imageData);

                Console.WriteLine("图片保存成功： " + imagePath);
                return imagePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("保存图片时出错： " + ex.Message);
                return string.Empty;
            }
        }
    }
}
