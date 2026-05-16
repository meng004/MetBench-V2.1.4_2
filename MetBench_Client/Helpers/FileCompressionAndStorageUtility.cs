//using LiteDB;
using MetBench_BLL;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;

namespace MetBench_Client.Helpers
{
    public class FileCompressionAndStorageUtility
    {
        ApplicationService _applicationRSerive ;

        // 解压文件的存储路径（默认系统的Temp路径）
        string SystemTemppath = Path.GetTempPath() + "\\MetBench";

        // 存储路径下创建的存储文件夹名
        string folderunderpath = "MR_SUT";
        static byte[] CodeFile = null;

        // 构造函数
        public FileCompressionAndStorageUtility(ApplicationService applicationSerive) 
        {
            _applicationRSerive = applicationSerive;
        }

        // 上传文件
        public void ProcessFileData(string filePath, string CodeName)
        {
            // 创建确认对话框并显示给用户
            //var confirmResult = MessageBox.Show("确认上传文件吗？", "确认上传", MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Tips",
                Content = "确认上传文件吗？",
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.CloseButtonText = "No";
            uiMessageBox.IsPrimaryButtonEnabled = true;
            uiMessageBox.PrimaryButtonAppearance = 0;
            uiMessageBox.HorizontalAlignment = HorizontalAlignment.Center;
            uiMessageBox.PrimaryButtonText = "Yes";

            var messageResult = uiMessageBox.ShowDialogAsync().Result.ToString();
            //primary为第一个按钮
            var confirmResult = messageResult == "Primary" ? true : false;

            //if (confirmResult == MessageBoxResult.Yes)
            if(confirmResult)
            {
                // 检查文件扩展名是否已经是 .zip
                if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // 存储压缩文件
                    //ProcessZipFileData(application, filePath, CodeName);
                    byte[] zipfiledata = ProcessZipFileData(filePath, CodeName);
                    CodeFile = zipfiledata;
                }
                else
                {
                    // 压缩并存储普通文件
                    byte[] filedata = ProcessNormalFileData(filePath, CodeName);
                    CodeFile = filedata;
                }
            }
            //else
            //{
            //    //用户取消上传，可以选择执行其他操作或不执行任何操作
            //}
        }

        // 撤回上传
        public void BacktoProcess(MetBench_Domain.Application application)
        {
            // 测回之前上传到据库的数据
            _applicationRSerive.BackUpdateCode(application);

            // 弹出撤销成功的消息窗口

            var message = "撤销上传成功！";
            var title = "Tips";
            showMessage(message, title);

            // MessageBox.Show("撤销上传成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 存储普通文件,打包成zip压缩包
        public byte[] ProcessNormalFileData(string filePath, string CodeName)
        {
            // 压缩文件为 .zip文件
            string zipFilePath = CompressFile(filePath);

            // 将压缩文件数据读取为字节数组
            byte[] zipFileData = File.ReadAllBytes(zipFilePath);

            // 删除临时生成的压缩文件
            File.Delete(zipFilePath);

            if (zipFileData!=null) 
            {
                var message = "文件上传成功！";
                var title = "Tips";
                showMessage(message, title);
            }
            return zipFileData;
        }

        //存储压缩文件
        public byte[] ProcessZipFileData(string filePath, string CodeName)
        {
            //ApplicationRSerive applicationRSerive = new ApplicationRSerive();
            // 将 ZIP 文件数据读取为字节数组
            byte[] zipFileData = File.ReadAllBytes(filePath);

            var message = "文件上传成功！";
            var title = "Tips";
            showMessage(message, title);
            //// 弹出上传成功的消息窗口
            //MessageBox.Show("文件上传成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            return zipFileData;
        }

        // 压缩文件为 .zip
        static string CompressFile(string filePath)
        {
            string zipFilePath = Path.ChangeExtension(filePath, ".zip");

            // 检查目标路径是否已经存在同名文件
            if (File.Exists(zipFilePath))
            {
                // 如果存在，可以选择删除或重命名该文件，或者选择一个不同的目标路径
                // 这里选择在文件名后面添加一个数字来重命名
                int count = 1;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(zipFilePath);
                string extension = Path.GetExtension(zipFilePath);
                do
                {
                    zipFilePath = Path.Combine(Path.GetDirectoryName(zipFilePath), $"{fileNameWithoutExtension}_{count}{extension}");
                    count++;
                }
                while (File.Exists(zipFilePath));
            }

            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
            }

            return zipFilePath;
        }

        //从数据库中获取选中行的压缩文件并解压至系统盘temp文件下新建的一个MR_SUT文件夹中
        public void ExtractAllZipFilesFromDatabase(MetBench_Domain.Application application)
        {
            //ApplicationRSerive applicationRSerive = new ApplicationRSerive();
            // 创建缓存目录
            string cacheFolderPath = Path.Combine(SystemTemppath, folderunderpath);
            Directory.CreateDirectory(cacheFolderPath);

            // 获取选中的 application 的压缩文件
            byte[] zipFileData = _applicationRSerive.GetZipCodeFileData(application);
            if (zipFileData != null)
            {
                // 创建临时文件路径以保存压缩文件数据
                string tempZipFilePath = Path.GetTempFileName();

                // 将压缩文件数据写入临时文件
                File.WriteAllBytes(tempZipFilePath, zipFileData);

                // 解压缩文件到缓存目录
                using (var zipArchive = ZipFile.OpenRead(tempZipFilePath))
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        string entryOutputFilePath = Path.Combine(cacheFolderPath, entry.FullName);
                        entry.ExtractToFile(entryOutputFilePath, true);
                        Console.WriteLine("文件成功解压并保存到路径：" + entryOutputFilePath);
                    }
                }

                // 删除临时生成的压缩文件
                File.Delete(tempZipFilePath);
                var message = "文件解压成功！";
                var title = "Tips";
                showMessage(message, title);
                //// 弹出上传成功的消息窗口
                //MessageBox.Show("文件解压成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Console.WriteLine("没有代码文件记录！");
            }
        }

        //执行蜕变关系自动解压所关联的文件
        public void ExtractZipFilesFromDatabase(int applicationid)
        {
            //ApplicationRSerive applicationRSerive = new ApplicationRSerive();
            // 创建缓存目录
            string cacheFolderPath = Path.Combine(SystemTemppath, folderunderpath);
            Directory.CreateDirectory(cacheFolderPath);

            // 获取选中的 application 的压缩文件
            byte[] zipFileData = _applicationRSerive.GetZipCodeFileDataById(applicationid);
            if (zipFileData != null)
            {
                // 创建临时文件路径以保存压缩文件数据
                string tempZipFilePath = Path.GetTempFileName();

                // 将压缩文件数据写入临时文件
                File.WriteAllBytes(tempZipFilePath, zipFileData);

                // 解压缩文件到缓存目录
                using (var zipArchive = ZipFile.OpenRead(tempZipFilePath))
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        string entryOutputFilePath = Path.Combine(cacheFolderPath, entry.FullName);
                        entry.ExtractToFile(entryOutputFilePath, true);
                        Console.WriteLine("文件成功解压并保存到路径：" + entryOutputFilePath);
                    }
                }

                // 删除临时生成的压缩文件
                File.Delete(tempZipFilePath);
            }
            else
            {
                Console.WriteLine("没有被测程序的代码文件记录！");
            }
        }
        //获取被测程序目录下所有同一程序名的文件
        public string[] GetFilesWithPrefix( string prefix)
        {
            string directoryPath = Path.Combine(SystemTemppath, folderunderpath);
            string[] matchingFiles = Array.Empty<string>();

            try
            {
                // 获取指定目录下的所有文件
                string[] allFiles = Directory.GetFiles(directoryPath);

                // 使用 LINQ 过滤出以指定前缀开头的文件，并提取文件名
                matchingFiles = allFiles
                    .Where(file => Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                                   && IsValidFileExtension(file))
                    .Select(file => Path.GetFileName(file))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }

            return matchingFiles;
        }
        private bool IsValidFileExtension(string filePath)
        {
            // 定义有效的文件后缀
            string[] validExtensions = { ".c", ".java", ".py" };


            // 获取文件后缀
            string fileExtension = Path.GetExtension(filePath);

            // 检查文件后缀是否在有效的文件后缀列表中
            return validExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }
        public byte[] GetFileData()
        {
            if (CodeFile != null)
            {
                return CodeFile;
            }
            else
            {
                return null;
            }
        }

        // 提示信息弹窗
        public bool showMessage(string message, string title)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
            };
            uiMessageBox.CloseButtonAppearance = 0;
            uiMessageBox.CloseButtonText = "OK";
            var messageResult = uiMessageBox.ShowDialogAsync().ToString();
            //primary为第一个按钮
            return messageResult == "Primary" ? true : false;
        }
    }

}