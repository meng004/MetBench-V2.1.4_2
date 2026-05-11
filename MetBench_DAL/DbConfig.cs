using LiteDB;
using MetBench_Domain;
using System.Configuration;
using System.Reflection;

namespace MetBench_DAL
{
    //数据库配置单例类
    public sealed class DbConfig
    {
        //对象集合的名称
        //key of  collection
        //蜕变关系集合的键
        public readonly string MetamorphicRelations_Collection_Key = "MetamorphicRelations";
        //应用程序集合的键
        public readonly string Applications_Collection_Key = "Applications";
        //应用领域集合的键
        public readonly string Domains_Collection_Key = "Domains";

        /// <summary>
        /// 连接字符串
        /// database connection string
        /// </summary>
        //配置文件读取数据库连接字符串
        public string _conn
        {
            get
            {
                //读取数据库连接字符串
                //read connectionstring.

                Assembly assembly = Assembly.GetEntryAssembly();
                // 获取执行程序集的文件路径
                string assemblyPath = assembly.Location;

                // 获取解决方案的目录路径
                string solutionDirPath = Path.GetDirectoryName(assemblyPath);

                // 循环向上查找解决方案文件（.sln）
                while (!Directory.GetFiles(solutionDirPath, "*.sln").Any())
                {
                    // 获取上级目录路径
                    string parentDirPath = Directory.GetParent(solutionDirPath)?.FullName;

                    // 如果已经到达根目录，则返回空字符串
                    if (parentDirPath == null)
                    {
                        return string.Empty;
                    }

                    solutionDirPath = parentDirPath;
                }

                var db_file = ConfigurationManager.ConnectionStrings["litedb"].ConnectionString;
                //string appName = Assembly.GetEntryAssembly().GetName().Name;//获取应用程序名称
                string appPath = $"{solutionDirPath}\\MetBench_DataBase";//获取应用程序的路径
                Directory.CreateDirectory(appPath); //目录存在则无操作
                var conn = db_file.Replace("|DataDirectory|", appPath);
                return conn;
            }
        }

        //DbConfig实例
         private static DbConfig instance;
        //锁
        private static readonly object _lock = new object();    

        //使用单例模式 完成实体映射数据表
        private DbConfig()
        {
            //实体映射数据表
            using (var db = new LiteDatabase(_conn))
            {
                var mapper = BsonMapper.Global;
                //建立引用
                
                if (!db.CollectionExists(MetamorphicRelations_Collection_Key)) 
                {
                    //配置MetamorphicRelations
                    var collection = db.GetCollection<MetamorphicRelation>(MetamorphicRelations_Collection_Key);
                    //添加唯一复合索引
                    collection.EnsureIndex("MR_Idx", x => new { x.InputPattern, x.OutputPattern, x.ApplicationName }, true);
                    //配置MetamorphicRelations
                    mapper.Entity<MetamorphicRelation>()
                   .Id(x => x.IdMR)
                   .Field(x => x.ApplicationName, "ApplicationName");// 映射属性到字段
                }

                if (!db.CollectionExists(Applications_Collection_Key))
                {
                    //配置Applications
                    var collection = db.GetCollection<Application>(Applications_Collection_Key);
                    // 添加唯一索引
                    collection.EnsureIndex(x => x.Name, unique: true);
                    // 添加唯一复合索引
                    collection.EnsureIndex("App_Id", x => new { x.Name, x.ProgrammingLanguage }, unique: true);
                    mapper.Entity<Application>()
                    .Id(x => x.IdApplication)
                    .Field(x => x.DomainName, "DomainName"); // 映射属性到字段
                }

                if (!db.CollectionExists(Domains_Collection_Key))
                {
                    //配置Domains
                    var collection = db.GetCollection<Domain>(Domains_Collection_Key);
                    // 添加唯一索引
                    collection.EnsureIndex(x => x.Name, unique: true);
                    // 添加唯一复合索引
                    collection.EnsureIndex("Domain_Id", x => new { x.Name, x.Description }, unique: true);
                    mapper.Entity<Domain>()
                    .Id(x => x.IdDomain);
                }
            }
        }

       //加锁
        public static DbConfig Instance
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new DbConfig();
                    }
                    return instance;
                }
            }
        }
    }
}
