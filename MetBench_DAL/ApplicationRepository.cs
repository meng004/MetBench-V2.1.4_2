// 本文件是 v1 legacy method-level MT 的应用程序仓库；意识地保留对 [Obsolete]
// 字段 Application.DomainName / MetamorphicRelation.ApplicationName 的读兼容
// （v2 schema 由 ApplicationDomains / MRBindings junction 表取代）。
// 所有 CS0618 都是上述兼容路径所致；用 file-level pragma 隔离，不混入真警告。
// 棘轮 (P3 step2 maturity remediation plan) 据此把 BLL.Core 之外的层归零。
#pragma warning disable CS0618 // Type or member is obsolete — intentional v1 read-compat per attribute
using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;
using System.Text;


namespace MetBench_DAL
{
    public class ApplicationRepository : IApplicationRepository
    {
        //数据库连接字符串 
        private string _conn;
        private DbConfig _dbConfig;

        // 映射实体集合：每个 LiteDB 操作方法在打开 db 后赋值（lazy per-method），
        // 字段在 ctor 不初始化是有意为之；用 null! 抑制 CS8618 而保留 lazy 模式。
        private ILiteCollection<MetamorphicRelation> MetamorphicRelations = null!;
        private ILiteCollection<Application> Applications = null!;
        private ILiteCollection<Domain> Domains = null!;

        public ApplicationRepository()
        {
            _dbConfig = DbConfig.Instance;
            _conn = _dbConfig._conn;
        }

        /// <summary>
        /// 获得应用程序的Id
        /// </summary>
        /// <param name="Name">应用程序的名称</param>
        /// <returns>应用程序的Id</returns>
        //返回id
        public int Get(string Name)
        {
            using (var db = new LiteDatabase(_conn))
            {
                //返回0 表示应用名称为Name的应用程序在数据表中不存在
                var name = Name.Trim();
                var id = 0;
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                var applications = Applications.FindAll().ToList();
                var applicationDictionary = applications.ToDictionary(app => app.Name);

                if (applicationDictionary.TryGetValue(Name, out var app))
                {
                    if (app != null) 
                    {
                        id = app.IdApplication;
                    }
                }

                return id;
            }
        }

        //public ObservableCollection<Applications_QueryResultData> GetAll_MIX()
        //{
        //    using (var db = new LiteDatabase(_conn))
        //    {
        //        // 数据集合，相当于数据表  
        //        MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
        //        Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
        //        Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);

        //        // 将数据表的全部记录保存到集合中  
        //        var metamorphicRelations = MetamorphicRelations.FindAll().ToList();
        //        var applications = Applications.FindAll().ToList();
        //        var domains = Domains.FindAll().ToList();

        //        // MetamorphicRelation与Application的中间类集合  
        //        var metamorphicRelationApplications = new List<MetamorphicRelationApplication>();
        //        foreach (var relation in metamorphicRelations)
        //        {
        //            string str = relation.ApplicationName;
        //            if (string.IsNullOrEmpty(str))
        //            {
        //                // 蜕变关系至少对应着一个应用程序   
        //                return new ObservableCollection<Applications_QueryResultData>();
        //            }

        //            string[] strarray = str.Split(':');
        //            foreach (var name in strarray)
        //            {
        //                metamorphicRelationApplications.Add(new MetamorphicRelationApplication
        //                {
        //                    IdMR = relation.IdMR,
        //                    ApplicationName = name.Trim() // 去除多余空格  
        //                });
        //            }
        //        }

        //        // Application与Domain中间类集合  
        //        var applicationDomains = new List<ApplicationDomain>();
        //        foreach (var application in applications)
        //        {
        //            string str = application.DomainName;
        //            if (!string.IsNullOrEmpty(str))
        //            {
        //                string[] strarray = str.Split(':');
        //                foreach (var domainName in strarray)
        //                {
        //                    applicationDomains.Add(new ApplicationDomain
        //                    {
        //                        ApplicationName = application.Name,
        //                        DomainName = domainName.Trim() // 去除多余空格  
        //                    });
        //                }
        //            }
        //        }

        //        // 保存查询结果集合  
        //        var result = new List<Applications_QueryResultData>();

        //        // 当 applicationDomains 这一中间类集合成员为 0，三表联查结果没有元素  
        //        if (applicationDomains.Count > 0)
        //        {
        //            var Applications_Query = from relation in metamorphicRelations
        //                                     join relationApp in metamorphicRelationApplications on relation.IdMR equals relationApp.IdMR
        //                                     join app in applications on relationApp.ApplicationName equals app.Name
        //                                     join appDomain in applicationDomains on app.Name equals appDomain.ApplicationName
        //                                     join domain in domains on appDomain.DomainName equals domain.Name
        //                                     select new
        //                                     {
        //                                         application = app,
        //                                         domain = domain
        //                                     };

        //            foreach (var application_query in Applications_Query)
        //            {
        //                var application = application_query.application;
        //                var applicationResult = new Applications_QueryResultData
        //                {
        //                    IdApplication = application.IdApplication,
        //                    Name = application.Name,
        //                    Description = application.Description,
        //                    ProgrammingLanguage = application.ProgrammingLanguage,
        //                    LinesOfCode = application.LinesOfCode,
        //                    Code = application.Code,
        //                    CodeName = application.CodeName,
        //                    SourceTestCase = application.SourceTestCase,
        //                    DOI = application.DOI,
        //                    Url = application.Url,
        //                    DomainName = application_query.domain.Name
        //                };
        //                result.Add(applicationResult);
        //            }
        //        }

        //        return new ObservableCollection<Applications_QueryResultData>(result);
        //    }
        //}

        public ObservableCollection<Applications_QueryResultData> GetAll_MIX()
        {
            using (var db = new LiteDatabase(_conn))
            {
                // 1. 获取数据并构建字典（提升查询性能）
                var appsDict = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key)
                               .FindAll()
                               .ToDictionary(a => a.Name);

                var domainsDict = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key)
                                 .FindAll()
                                 .ToDictionary(d => d.Name);

                // 2. 主查询处理
                var results = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key)
                             .FindAll()
                             .Where(mr => !string.IsNullOrEmpty(mr.ApplicationName))
                             .SelectMany(mr => mr.ApplicationName.Split(':')
                                 .Select(appName => appName.Trim())
                                 .Where(appName => appsDict.ContainsKey(appName))
                                 .SelectMany(appName =>
                                 {
                                     var app = appsDict[appName];
                                     var domainNames = app.DomainName?.Split(':') ?? Array.Empty<string>();
                                     return domainNames
                                         .Select(domainName => domainName.Trim())
                                         .Where(domainName => domainsDict.ContainsKey(domainName))
                                         .Select(domainName => (app, domain: domainsDict[domainName]));
                                 }))
                             .Select(x => new Applications_QueryResultData
                             {
                                 IdApplication = x.app.IdApplication,
                                 Name = x.app.Name,
                                 Description = x.app.Description,
                                 ProgrammingLanguage = x.app.ProgrammingLanguage,
                                 LinesOfCode = x.app.LinesOfCode,
                                 Code = x.app.Code,
                                 CodeName = x.app.CodeName,
                                 SourceTestCase = x.app.SourceTestCase,
                                 DOI = x.app.DOI,
                                 Url = x.app.Url,
                                 DomainName = x.domain.Name
                             })
                             .Distinct()
                             .ToList();

                return new ObservableCollection<Applications_QueryResultData>(results);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>数据表中全部的应用程序的集合</returns>
        public ObservableCollection<Application> GetAll()
        {
            using (var db = new LiteDatabase(_conn))
            {              
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                var result = new ObservableCollection<Application>(Applications.FindAll());
                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns>对应Id的应用程序</returns>
        public Application Get(int id)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                var application = Applications.FindById(id);
                return application;
            }
        }


        public ObservableCollection<Application> Get(Application application)
        {
            ArgumentNullException.ThrowIfNull(application);

            using (var db = new LiteDatabase(_conn))
            {
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                var query = Applications.FindAll().AsEnumerable();

                if (!string.IsNullOrWhiteSpace(application.Name))
                    query = query.Where(app => Contains(app.Name, application.Name));
                if (!string.IsNullOrWhiteSpace(application.Description))
                    query = query.Where(app => Contains(app.Description, application.Description));
                if (!string.IsNullOrWhiteSpace(application.ProgrammingLanguage))
                    query = query.Where(app => Contains(app.ProgrammingLanguage, application.ProgrammingLanguage));
                if (application.LinesOfCode != 0)
                    query = query.Where(app => app.LinesOfCode == application.LinesOfCode);
                if (!string.IsNullOrWhiteSpace(application.CodeName))
                    query = query.Where(app => Contains(app.CodeName, application.CodeName));
                if (!string.IsNullOrWhiteSpace(application.SourceTestCaseName))
                    query = query.Where(app => Contains(app.SourceTestCaseName, application.SourceTestCaseName));
                if (!string.IsNullOrWhiteSpace(application.DOI))
                    query = query.Where(app => Contains(app.DOI, application.DOI));
                if (!string.IsNullOrWhiteSpace(application.Url))
                    query = query.Where(app => Contains(app.Url, application.Url));
                if (!string.IsNullOrWhiteSpace(application.DomainName))
                    query = query.Where(app => Contains(app.DomainName, application.DomainName));
                if (!string.IsNullOrWhiteSpace(application.Version))
                    query = query.Where(app => Contains(app.Version, application.Version));
                if (application.RuntimeId.HasValue)
                    query = query.Where(app => app.RuntimeId == application.RuntimeId);
                if (!string.IsNullOrWhiteSpace(application.RunnerEntryPath))
                    query = query.Where(app => Contains(app.RunnerEntryPath, application.RunnerEntryPath));
                if (!string.IsNullOrWhiteSpace(application.InputParserPath))
                    query = query.Where(app => Contains(app.InputParserPath, application.InputParserPath));
                if (!string.IsNullOrWhiteSpace(application.OutputParserPath))
                    query = query.Where(app => Contains(app.OutputParserPath, application.OutputParserPath));
                if (!string.IsNullOrWhiteSpace(application.Kind))
                    query = query.Where(app => string.Equals(app.Kind, application.Kind, StringComparison.Ordinal));

                return new ObservableCollection<Application>(query.ToList());
            }
        }
        /// <summary>
        ///  通过Name进行模糊查询应用程序
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ObservableCollection<Applications_QueryResultData> GetByName(string Name)
        {
            var applications_queryResultData = GetAll_MIX();

            using (var db = new LiteDatabase(_conn))
            {
                //Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                var app_Querys = applications_queryResultData.Where(x => x.Name.Contains(Name)).ToList();
                //var result = (ObservableCollection<Application>(){ applications});
                var result = new ObservableCollection<Applications_QueryResultData>(app_Querys);
                return result;
            }
        }

        public bool Add(Application application)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);

                // 判断 application 是否已经在库中  
                if (application.IdApplication == 0)
                {
                    // application.DomainName 以 : 为分割符   
                    var str = application.DomainName;
                    if (!string.IsNullOrEmpty(str))
                    {
                        // 对应用程序名称进行分割，分割符为 :  
                        var strarray = str.Split(':');
                        var newstr = new StringBuilder();
                        var domainDictionary = Domains.FindAll().ToDictionary(domain => domain.Name);

                        for (int i = 0; i < strarray.Length; i++)
                        {
                            var name = strarray[i].Trim(); // 去除多余空格  
                                                           // 检查域名是否存在  
                            if (domainDictionary.ContainsKey(name))
                            {
                                if (newstr.Length > 0)
                                {
                                    newstr.Append(":");
                                }
                                newstr.Append(name);
                            }
                        }

                        application.DomainName = newstr.ToString();
                    }

                    var _id = Applications.Insert(application);
                    return _id != null; // 如果插入成功，返回 true  
                }
                else
                {
                    return false; // 如果 IdApplication 不为 0，返回 false  
                }
            }
        }

        public bool Modify(Application application)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                Domains = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key);

                // 判断 application 是否在库中  
                var existingApplication = Applications.FindOne(x => x.IdApplication == application.IdApplication);
                if (existingApplication == null)
                {
                    return false;
                }

                var beforeApplicationName = existingApplication.Name;

                // 外键 Name 进行修改，对应的 MR 关联的 ApplicationName 也需要进行修改  
                if (beforeApplicationName != application.Name)
                {
                    MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                    var metamorphicRelations = MetamorphicRelations.FindAll()
                        .Where(x => x.ApplicationName.Contains(beforeApplicationName))
                        .ToList();

                    foreach (var metamorphicRelation in metamorphicRelations)
                    {
                        // 更新蜕变关系中的应用程序名称  
                        var strAppNames = metamorphicRelation.ApplicationName.Split(':').ToList();
                        for (int j = 0; j < strAppNames.Count; j++)
                        {
                            if (strAppNames[j] == beforeApplicationName)
                            {
                                strAppNames[j] = application.Name; // 修改为更新后的 Application 的 Name  
                            }
                        }

                        metamorphicRelation.ApplicationName = string.Join(":", strAppNames);
                        MetamorphicRelations.Update(metamorphicRelation);
                    }
                }

                // 处理 application.DomainName  
                var domainNameStr = application.DomainName;
                if (!string.IsNullOrEmpty(domainNameStr))
                {
                    var domainDictionary = Domains.FindAll().ToDictionary(domain => domain.Name);
                    var strArray = domainNameStr.Split(':');
                    var newDomainNames = new StringBuilder();

                    for (int i = 0; i < strArray.Length; i++)
                    {
                        var domainName = strArray[i].Trim(); // 去除多余空格  
                        if (domainDictionary.ContainsKey(domainName))
                        {
                            if (newDomainNames.Length > 0)
                            {
                                newDomainNames.Append(":");
                            }
                            newDomainNames.Append(domainName);
                        }
                    }

                    application.DomainName = newDomainNames.ToString();
                }
                return Applications.Update(application); // 更新应用程序并返回结果  
            }
        }

        public bool Remove(Application application)
        {
            using (var db = new LiteDatabase(_conn))
            {
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);

                // 查找要删除的应用程序  
                var existingApplication = Applications.FindOne(x => x.IdApplication == application.IdApplication);
                if (existingApplication == null)
                {
                    return false; // 应用程序不存在  
                }

                var beforeApplicationName = existingApplication.Name;

                // 获取所有关联的蜕变关系  
                var metamorphicRelations = MetamorphicRelations.FindAll()
                    .Where(x => x.ApplicationName.Contains(beforeApplicationName))
                    .ToList();

                // 使用字典进行快速查找  
                var metamorphicRelationDict = metamorphicRelations.ToDictionary(mr => mr.IdMR);

                foreach (var metamorphicRelation in metamorphicRelations)
                {
                    var strArray = metamorphicRelation.ApplicationName.Split(':').ToList();

                    // 移除要删除的应用程序名称  
                    strArray.RemoveAll(name => name == beforeApplicationName);

                    if (strArray.Count > 0)
                    {
                        // 更新蜕变关系中的应用程序名称  
                        metamorphicRelation.ApplicationName = string.Join(":", strArray);
                        MetamorphicRelations.Update(metamorphicRelation);
                    }
                    else
                    {
                        // 删除蜕变关系  
                        MetamorphicRelations.Delete(metamorphicRelation.IdMR);
                    }
                }

                // 删除应用程序  
                return Applications.Delete(application.IdApplication);
            }
        }

        /// <summary>
        /// 检查是否存在重复的应用
        /// </summary>
        /// <param name="application">待检查的应用对象</param>
        /// <param name="excludeSelf">是否排除自身（true为排除自身，false为不排除） 修改为true 增加为false</param>
        /// <returns>true表示存在重复，false表示不重复</returns>
        public bool IsDuplicate(Application application, bool excludeSelf = false)
        {
            using (var db = new LiteDatabase(_conn))
            {
                var collection = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                // 避免没有设置唯一复合索引
                collection.EnsureIndex("App_Id", x => new { x.Name, x.ProgrammingLanguage }, unique: true);
                // 构建基础查询条件（名称或代码内容重复）
                var query = Query.Or(
                    Query.EQ("Name", application.Name),
                    Query.And(
                        Query.EQ("ProgrammingLanguage", application.ProgrammingLanguage),
                        Query.EQ("LinesOfCode", application.LinesOfCode)
                    )
                );

                // 如果需要排除自身（更新操作时使用）
                if (excludeSelf && application.IdApplication != 0)
                {
                    query = Query.And(query, Query.Not("_id", application.IdApplication));
                }

                return collection.Exists(query);
            }
        }

        private static bool Contains(string? value, string expected) =>
            !string.IsNullOrEmpty(value)
            && value.Contains(expected, StringComparison.OrdinalIgnoreCase);

    }
}
