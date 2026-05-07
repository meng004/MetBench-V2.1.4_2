using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;


namespace MetBench_DAL
{
    public class DomainRepository : IDomainRepository
    {
        //数据库连接字符串 
        private string _conn;
        private DbConfig _dbConfig;

        private ILiteCollection<Application> Applications;
        private ILiteCollection<Domain> Domains;
        public DomainRepository()
        {
            //获得DbConfig对象
            _dbConfig = DbConfig.Instance;
            _conn = _dbConfig._conn;
        }

        /// <summary>
        /// 根据应用领域的名称获取Id
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public int Get(string Name)
        {
            using (var db = new LiteDatabase(_conn))
            {
                var id = 0;
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                var domain = Domains.FindOne(t => t.Name == Name);
                if (domain != null)
                {
                    id = domain.IdDomain;
                }
                return id;
            }
        }

        /// <summary>
        /// 获取全部的应用领域实体
        /// </summary>
        /// <returns> 数据表中全部的应用领域的集合</returns>
        public ObservableCollection<Domain> GetAll()
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                var result = new ObservableCollection<Domain>(Domains.FindAll());
                return result;

            }
        }

        /// <summary>
        /// 根据Id获取应用领域实体
        /// </summary>
        /// <param name="id"></param>
        /// <returns>对应Id的应用领域</returns>
        public Domain Get(int id)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                var domain = Domains.FindById(id);
                return domain;
            }
        }

        /// <summary>
        /// 模糊查询
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public ObservableCollection<Domain> Get(Domain domain)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                var result = new ObservableCollection<Domain>(
                    Domains.FindAll().Where(t => (t.Name.Contains(domain.Name)) && (t.Description.Contains(domain.Description)))
                    );
                return result;
            }
        }

        /// <summary>
        /// 添加应用领域
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool Add(Domain domain)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                if (domain.IdDomain==0)
                {
                    var id = Domains.Insert(domain);
                    return id > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool Modify(Domain domain)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                var existingDomain = Domains.FindById(domain.IdDomain);
                if (existingDomain != null)
                {
                    var beforeDomainName = existingDomain.Name;

                    // 只有在名称变化时才进行修改  
                    if (beforeDomainName != domain.Name)
                    {
                        Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);

                        // 获取所有应用程序并存储在字典中以提高查找效率  
                        var allApplications = Applications.FindAll().ToDictionary(app => app.IdApplication);

                        // 找到所有需要更新的应用程序  
                        var applicationsToUpdate = allApplications.Values
                            .Where(app => app.DomainName != null && app.DomainName.Contains(beforeDomainName))
                            .ToList();

                        foreach (var application in applicationsToUpdate)
                        {
                            var strDomainName = application.DomainName;
                            var strList = strDomainName.Split(":").ToList();

                            // 更新关联的应用领域名称  
                            for (int j = 0; j < strList.Count(); j++)
                            {
                                if (strList[j] == beforeDomainName)
                                {
                                    strList[j] = domain.Name;
                                }
                            }

                            // 使用 String.Join 来构建新的 DomainName  
                            application.DomainName = string.Join(":", strList);
                            Applications.Update(application);
                        }
                    }

                    // 更新域  
                    return Domains.Update(domain);
                }
                else
                {
                    return false;
                }
            }
        }

        public bool Remove(Domain domain)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);

                // 检查应用领域是否存在  
                var existingDomain = Domains.FindById(domain.IdDomain);
                if (existingDomain != null)
                {
                    // 获取所有应用程序并存储在内存中  
                    var allApplications = Applications.FindAll().ToList();

                    // 避免修改了应用邻域名称
                    var OldDomainName = Domains.FindById(domain.IdDomain).Name;

                    // 找到所有需要更新的应用程序
                    var applicationsToUpdate = allApplications
                        .Where(app => app.DomainName != null && app.DomainName.Contains(OldDomainName))
                        .ToList();

                    foreach (var application in applicationsToUpdate)
                    {
                        // 处理关联的应用程序名  
                        var domainNames = application.DomainName.Split(':').ToList();

                        // 移除要删除的域名  
                        domainNames.RemoveAll(name => name == domain.Name);

                        // 更新 DomainName  
                        application.DomainName = string.Join(":", domainNames);
                        Applications.Update(application);
                    }

                    // 删除域  
                    return Domains.Delete(domain.IdDomain);
                }
                else
                {
                    return false;
                }
            }
        }

        public ObservableCollection<Domain> GetByName(string Name)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                var domains = Domains.FindAll();
                var result = new ObservableCollection<Domain>( domains.Where(x => x.Name.Contains(Name)));
                return result;
            }
        }

        /// <summary>
        /// 检查是否存在重复的应用领域
        /// </summary>
        /// <param name="domain">待检查的应用领域</param>
        /// <param name="excludeSelf">是否排除自身（true为排除自身，false为不排除） 修改为true 增加为false</param>
        /// <returns>true表示存在重复，false表示不重复</returns>
        public bool IsDuplicate(Domain domain, bool excludeSelf = false)
        {
            using (var db = new LiteDatabase(_conn))
            {
                var collection = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);
                collection.EnsureIndex("Domain_Id", x => new { x.Name, x.Description }, unique: true);

                // 构建基础查询条件（名称或代码内容重复）
                var query = Query.Or(
                    Query.EQ("Name", domain.Name),
                    Query.And(
                        Query.EQ("Description", domain.Description),
                        Query.EQ("Name", domain.Name)
                    )
                );

                // 如果需要排除自身（更新操作时使用）
                if (excludeSelf && domain.IdDomain != 0)
                {
                    query = Query.And(query, Query.Not("_id", domain.IdDomain));
                }

                return collection.Exists(query);
            }
        }
    }
}
