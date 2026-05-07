using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    //应用程序管理服务类
    public class ApplicationSerive
    {
        //数据仓库（实体集合的仓库）
        private IApplicationRepository Application_repository;


        //依赖注入实体
        public ApplicationSerive( IApplicationRepository applicationRepository)
        {
            Application_repository = applicationRepository;
        }

        /// <summary>
        ///获取应用程序的Id 
        /// </summary>
        /// <param name="name">应用程序的名称</param>
        /// <returns>应用程序的Id</returns>
        public int GetApplicationId(string name)
        {
            var id = Application_repository.Get(name);
            return id;
        }

        /// <summary>
        /// 根据Id获取应用程序对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Application GetApplication(int id)
        {
            var application = Application_repository.Get(id);
            return application;
        }

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="metamorphicRelation"></param>
        /// <returns>0 表示失败 1 表示重复  2表示成功 </returns>
        public int AddService(Application application)
        {
            if (Application_repository.IsDuplicate(application, false))
            {
                return 1;
            }
            var result = Application_repository.Add(application);
            if (result)
            {
                return 2;
            }
            else 
            {
                return 0;
            }
        }

        /// <summary>
        /// 删除服务
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public bool DeleteService(Application application)
        {
            var result = Application_repository.Remove(application);
            return result;
        }

        /// <summary>
        /// 获取全部的应用程序实体
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Application> GetApplications()
        {
            var result = Application_repository.GetAll();
            return result;
        }

        /// <summary>
        /// 三表联查
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Applications_QueryResultData> showMultResult()
        {
            var result = Application_repository.GetAll_MIX();
            return result;
        }

        /// <summary>
        /// 更新服务
        /// </summary>
        /// <param name="metamorphicRelation"></param>
        /// <returns>0 表示失败 1 表示重复  2表示成功 </returns>
        public int UpdateService(Application application)
        {
            if (Application_repository.IsDuplicate(application, false))
            {
                return 1;
            }
            var result = Application_repository.Modify(application);
            if (result)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 上传程序压缩文件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public bool UpdateCode(Application application, byte[] codeData, string CodeName)
        {
            if (Application_repository.Get(application.IdApplication) != null)
            {
                application.Code = codeData;
                application.CodeName = CodeName;
                var result = Application_repository.Modify(application);
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 撤回上传程序压缩文件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public bool BackUpdateCode(Application application)
        {
            if (Application_repository.Get(application.IdApplication) != null)
            {
                application.Code = null;
                application.CodeName = string.Empty;
                var result = Application_repository.Modify(application);
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取程序压缩文件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public byte[] GetZipCodeFileData(Application application)
        {
            var existingApplication = Application_repository.Get(application.IdApplication);
            if (Application_repository.Get(application.IdApplication) != null)
            {
                byte[] ZipCodeFile = existingApplication.Code;
                Console.WriteLine(ZipCodeFile);
                return ZipCodeFile;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取程序压缩文件名称
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public string GetZipCodeFileName(Application application)
        {
            if (Application_repository.Get(application.IdApplication) != null)
            {
                string ZipCodeName = application.CodeName;
                return ZipCodeName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 根据Id获取程序压缩文件
        /// </summary>
        /// <param name="applicationid"></param>
        /// <returns></returns>
        public byte[] GetZipCodeFileDataById(int applicationid)
        {
            var existingApplication = Application_repository.Get(applicationid);
            if (Application_repository.Get(applicationid) != null)
            {
                byte[] ZipCodeFile = existingApplication.Code;
                Console.WriteLine(ZipCodeFile);
                return ZipCodeFile;
            }
            else
            {
                return null;
            }

        }
    }
}
