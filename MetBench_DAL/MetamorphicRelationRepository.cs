using LiteDB;
using MetBench_Domain;
using MetBench_IDAL;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.Text;

namespace MetBench_DAL
{
    public class MetamorphicRelationRepository : IMetamorphicRelationRepository
    {

        //数据库连接字符串 
        private string _conn;
        private DbConfig _dbConfig;
        //对象集合
        private ILiteCollection<MetamorphicRelation> MetamorphicRelations;
        private ILiteCollection<Application> Applications;

        public DatatoImage datatoImage { get ; set ; }

        public MetamorphicRelationRepository()
        {
            //获得DbConfig对象
            _dbConfig = DbConfig.Instance;
            _conn = _dbConfig._conn;
            datatoImage = new DatatoImage();
        }

        public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX()
        {
            using (var db = new LiteDatabase(_conn))
            {
                // 构建内存字典加速查询
                var appDict = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key)
                              .FindAll()
                              .ToDictionary(app => app.Name);

                var domainDict = db.GetCollection<Domain>(_dbConfig.Domains_Collection_Key)
                               .FindAll()
                               .ToDictionary(d => d.Name);


                // 链式LINQ查询
                var results = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key)
                              .FindAll()
                              .Where(mr => !string.IsNullOrEmpty(mr.ApplicationName))
                              .SelectMany(mr => mr.ApplicationName.Split(':')
                                  .Where(appName => appDict.ContainsKey(appName))
                                  .Select(appName => appDict[appName])
                                  .SelectMany(app =>
                                      (app.DomainName?.Split(':') ?? Array.Empty<string>())
                                      .Where(domainName => domainDict.ContainsKey(domainName))
                                      .Select(domainName => new {
                                          MetamorphicRelation = mr,
                                          Application = app,
                                          DomainName = domainName
                                      })
                                  ))
                              .Select(x => new MetamorphicRelations_QueryResultData
                              {
                                  IdMR = x.MetamorphicRelation.IdMR,
                                  Description = x.MetamorphicRelation.Description,
                                  Context = x.MetamorphicRelation.Context,
                                  Constraint = x.MetamorphicRelation.Constraint,
                                  OrderOfMR = x.MetamorphicRelation.OrderOfMR,
                                  InputPattern = x.MetamorphicRelation.InputPattern,
                                  OutputPattern = x.MetamorphicRelation.OutputPattern,
                                  DimensionOfInputPattern = x.MetamorphicRelation.DimensionOfInputPattern,
                                  DimensionOfOutputPattern = x.MetamorphicRelation.DimensionOfOutputPattern,
                                  Granularity = x.MetamorphicRelation.Granularity,
                                  Hierarchy = x.MetamorphicRelation.Hierarchy,
                                  Operator = x.MetamorphicRelation.Operator,
                                  Expression = x.MetamorphicRelation.Expression,

                                  ApplicationName = x.Application.Name,
                                  CodeName = x.Application.CodeName,
                                  DomainName = x.DomainName ?? string.Empty,

                                  InputPatternImageData = x.MetamorphicRelation.InputPatternImageData,
                                  OutputPatternImageData = x.MetamorphicRelation.OutputPatternImageData,
                                  InputPatternImagepath = datatoImage.ConvertByteArrayToImage(x.MetamorphicRelation.InputPatternImageData),
                                  OutputPatternImagepath = datatoImage.ConvertByteArrayToImage(x.MetamorphicRelation.OutputPatternImageData)
                              })
                              .ToList();

                return new ObservableCollection<MetamorphicRelations_QueryResultData>(results);
            }
        }

        public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable()
        {
            using (var db = new LiteDatabase(_conn))
            {
                // 获取集合引用
                var mrCollection = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                var appCollection = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);

                    // 构建应用字典（Name -> Application）
                    var appDictionary = appCollection.FindAll()
                    .ToDictionary(app => app.Name, app => app);

                // 查询并转换结果
                var results = mrCollection.FindAll()
                    .Where(mr => !string.IsNullOrEmpty(mr.ApplicationName))
                    .SelectMany(mr => mr.ApplicationName.Split(':')
                        .Where(appName => appDictionary.ContainsKey(appName))
                        .Select(appName => new MetamorphicRelations_QueryResultData
                        {
                            // MetamorphicRelation 数据
                            IdMR = mr.IdMR,
                            Description = mr.Description,
                            Context = mr.Context,
                            Constraint = mr.Constraint,
                            OrderOfMR = mr.OrderOfMR,
                            InputPattern = mr.InputPattern,
                            OutputPattern = mr.OutputPattern,
                            InputPatternImageData = mr.InputPatternImageData,
                            OutputPatternImageData = mr.OutputPatternImageData,
                            DimensionOfInputPattern = mr.DimensionOfInputPattern,
                            DimensionOfOutputPattern = mr.DimensionOfOutputPattern,
                            Granularity = mr.Granularity,
                            Hierarchy = mr.Hierarchy,
                            Operator = mr.Operator,
                            Expression = mr.Expression,
                            // Application 数据
                            ApplicationName = appDictionary[appName].Name,
                            CodeName = appDictionary[appName].CodeName,
                            DomainName = appDictionary[appName].DomainName ?? string.Empty,

                            // 图片转换
                            InputPatternImagepath = datatoImage.ConvertByteArrayToImage(mr.InputPatternImageData),
                            OutputPatternImagepath = datatoImage.ConvertByteArrayToImage(mr.OutputPatternImageData)
                        }))
                    .ToList();

                return new ObservableCollection<MetamorphicRelations_QueryResultData>(results);
            }
        }

        public ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation filterParams = null, string domainName = null)
        {
            if (filterParams == null && string.IsNullOrEmpty(domainName))
            {
                // 无任何查询条件时返回空集合
                return new ObservableCollection<MetamorphicRelations_QueryResultData>();
            }
            using (var db = new LiteDatabase(_conn))
            {
                // 获取集合
                var mrCollection = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                var appCollection = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);

                // 添加单独索引加速查询
                mrCollection.EnsureIndex(x => x.OrderOfMR);
                mrCollection.EnsureIndex(x => x.DimensionOfInputPattern);
                mrCollection.EnsureIndex(x => x.DimensionOfOutputPattern);
                mrCollection.EnsureIndex(x => x.ApplicationName);
                appCollection.EnsureIndex(x => x.Name, unique: true);
                appCollection.EnsureIndex(x => x.DomainName);
                // 添加复合索引加速查询
                mrCollection.EnsureIndex("MulQuery", x => new { x.DimensionOfInputPattern, x.DimensionOfOutputPattern, x.ApplicationName });

                // 构建基础查询（不立即执行）
                var query = mrCollection.FindAll().AsQueryable();

                // 动态添加条件（仅当参数有值时）
                if (filterParams != null)
                {
                    if (!string.IsNullOrEmpty(filterParams.OrderOfMR))
                        query = query.Where(mr => mr.OrderOfMR.Contains(filterParams.OrderOfMR));
                    if (!string.IsNullOrEmpty(filterParams.DimensionOfInputPattern))
                        query = query.Where(mr => mr.DimensionOfInputPattern.Contains(filterParams.DimensionOfInputPattern));
                    if (!string.IsNullOrEmpty(filterParams.DimensionOfOutputPattern))
                        query = query.Where(mr => mr.DimensionOfOutputPattern.Contains(filterParams.DimensionOfOutputPattern));
                    if (!string.IsNullOrEmpty(filterParams.ApplicationName))
                        query = query.Where(mr => mr.ApplicationName.Contains(filterParams.ApplicationName));
                }
                // 执行主查询
                var filteredMRs = query.ToList();

                // 处理关联查询
                var results = new List<MetamorphicRelations_QueryResultData>();
                foreach (var mr in filteredMRs)
                {
                    if (string.IsNullOrEmpty(mr.ApplicationName)) continue;

                    foreach (var appName in mr.ApplicationName.Split(':'))
                    {
                        var app = appCollection.FindOne(a => a.Name == appName);
                        if (app == null) continue;

                        // 检查DomainName条件（可选）
                        if (!string.IsNullOrEmpty(domainName) &&
                            (app.DomainName == null || !app.DomainName.Contains(domainName)))
                        {
                            continue;
                        }

                        results.Add(new MetamorphicRelations_QueryResultData
                        {
                            IdMR = mr.IdMR,
                            Description = mr.Description,
                            Context = mr.Context,
                            Constraint = mr.Constraint,
                            OrderOfMR = mr.OrderOfMR,
                            InputPattern = mr.InputPattern,
                            OutputPattern = mr.OutputPattern,
                            InputPatternImageData = mr.InputPatternImageData,
                            OutputPatternImageData = mr.OutputPatternImageData,
                            DimensionOfInputPattern = mr.DimensionOfInputPattern,
                            DimensionOfOutputPattern = mr.DimensionOfOutputPattern,
                            Granularity = mr.Granularity,
                            Hierarchy = mr.Hierarchy,
                            Operator = mr.Operator,
                            Expression = mr.Expression,
                            ApplicationName = app.Name,
                            CodeName = app.CodeName,
                            DomainName = app.DomainName ?? string.Empty,
                            InputPatternImagepath = datatoImage.ConvertByteArrayToImage(mr.InputPatternImageData),
                            OutputPatternImagepath = datatoImage.ConvertByteArrayToImage(mr.OutputPatternImageData)
                        });
                    }
                }

                return new ObservableCollection<MetamorphicRelations_QueryResultData>(results);
            }
        }

        public ObservableCollection<MetamorphicRelation> GetAll()
        {
            using (var db = new LiteDatabase(_conn))
            {
                MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                var result = new ObservableCollection<MetamorphicRelation>(MetamorphicRelations.FindAll());
                return result;
            }
        }

        public MetamorphicRelation Get(int id)
        {
            using (var db = new LiteDatabase(_conn))
            {
                MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                var metamorphicRelation = MetamorphicRelations.FindById(id);
                return metamorphicRelation;
            }
        }

        public bool Add(MetamorphicRelation metamorphicRelation)
        {
            // 标识： MetamorphicRelation.Application没有的话 ApplicationName为String.Empty;  
            using (var db = new LiteDatabase(_conn))
            {
                // 如果 IdMR 已存在，直接返回 false  
                if (metamorphicRelation.IdMR != 0)
                {
                    return false;
                }
                MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                // 将 Applications 转换为字典，以便快速查找  通过索引为Name 通过Name可以找到对应的应用程序
                var applicationDictionary = Applications.FindAll().ToDictionary(app => app.Name);
                // 保存关联的应用程序的名称  
                var str = metamorphicRelation.ApplicationName;
                // 对应用程序名称进行分割，分割符为 ":"  
                var strarray = str.Split(":");
                var newstr = new StringBuilder();
                for (int i = 0; i < strarray.Length; i++)
                {
                    var name = strarray[i];
                    // 检查应用程序是否存在  
                    if (applicationDictionary.ContainsKey(name))
                    {
                        if (i > 0)
                        {
                            newstr.Append(":");
                        }
                        newstr.Append(name);
                    }
                    else
                    {
                        // 应用程序不存在，需先将其加入表中  
                        return false;
                    }
                }
                // 更新 ApplicationName  
                metamorphicRelation.ApplicationName = newstr.ToString();
                var _id = MetamorphicRelations.Insert(metamorphicRelation);
                return _id > 0;
            }
        }

        public bool Modify(MetamorphicRelation metamorphicRelation)
        {
            using (var db = new LiteDatabase(_conn))
            {
                // 检查 IdMR 是否存在  
                var existingRelation = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key).FindById(metamorphicRelation.IdMR);
                if (existingRelation == null)
                {
                    return false; // IdMR 不存在，返回 false  
                }

                Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
              
                // 将 Applications 转换为字典，以便快速查找  
                var applicationDictionary = Applications.FindAll().ToDictionary(app => app.Name);

                // 保存关联的应用程序的名称  
                var str = metamorphicRelation.ApplicationName;
                if (str == string.Empty || str == null) 
                {
                    return false;
                }
                // 对应用程序名称进行分割，分割符为 ":"  
                var strarray = str.Split(":");
                var newstr = new StringBuilder();

                for (int i = 0; i < strarray.Length; i++)
                {
                    var name = strarray[i];
                    // 检查应用程序是否存在  
                    if (applicationDictionary.ContainsKey(name))
                    {
                        if (i > 0)
                        {
                            newstr.Append(":");
                        }
                        newstr.Append(name);
                    }
                    else
                    {
                        // 应用程序不存在，需先将其加入表中  
                        return false;
                    }
                }

                // 更新 ApplicationName  
                metamorphicRelation.ApplicationName = newstr.ToString();
                // 直接更新  
                return MetamorphicRelations.Update(metamorphicRelation);
            }
        }

        public bool Remove(MetamorphicRelation metamorphicRelation)
        {
            using (var db = new LiteDatabase(_conn))
            {
                if (db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key).FindById(metamorphicRelation.IdMR) != null)
                {

                    MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                    Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);
                    var result = MetamorphicRelations.Delete(metamorphicRelation.IdMR);

                    return result;
                }
                else
                {
                    return false;

                }
            }
        }

        //暂时不需要
        public ObservableCollection<MetamorphicRelation> Get(MetamorphicRelation entity)
        {
            throw new NotImplementedException();
        }

        //public int Add_Return_ID(MetamorphicRelation metamorphicRelation)
        //{
        //    using (var db = new LiteDatabase(_conn))
        //    {
        //        // 检查 IdMR 是否存在  
        //        if (db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key).FindById(metamorphicRelation.IdMR) != null)
        //        {
        //            return 0; // 返回 0 表示添加失败  
        //        }

        //        MetamorphicRelations = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
        //        Applications = db.GetCollection<Application>(_dbConfig.Applications_Collection_Key);

        //        // 检查是否已存在相同的蜕变关系
        //        if (MetamorphicRelations.Exists(x =>
        //            x.InputPattern == metamorphicRelation.InputPattern &&
        //            x.OutputPattern == metamorphicRelation.OutputPattern &&
        //            x.ApplicationName == metamorphicRelation.ApplicationName))
        //        {
        //            return 0;
        //        }
        //        // 将 Applications 转换为字典，以便快速查找  
        //        var applicationDictionary = Applications.FindAll().ToDictionary(app => app.Name);

        //        // 保存关联的应用程序的名称  
        //        var str = metamorphicRelation.ApplicationName;
        //        if (str == string.Empty || str == null) 
        //        {
        //            return 0;
        //        }
        //        // 对应用程序名称进行分割，分割符为 ":"  
        //        var strarray = str.Split(":");
        //        var newstr = new StringBuilder();

        //        for (int i = 0; i < strarray.Length; i++)
        //        {
        //            var name = strarray[i];
        //            // 检查应用程序是否存在  
        //            if (applicationDictionary.ContainsKey(name))
        //            {
        //                if (i > 0)
        //                {
        //                    newstr.Append(":");
        //                }
        //                newstr.Append(name);
        //            }
        //            else
        //            {
        //                // 应用程序不存在，需先将其加入表中  
        //                return 0;
        //            }
        //        }

        //        // 更新 ApplicationName  
        //        metamorphicRelation.ApplicationName = newstr.ToString();
        //        // 插入并返回插入的 ID  
        //        var _id = MetamorphicRelations.Insert(metamorphicRelation);
        //        return _id > 0 ? _id : 0; // 如果插入成功，返回 ID，否则返回 0  
        //    }
        //}

        // 检查是否存在重复的蜕变关系 参数excludeSelf true为排除自身 false不为不排除自身
        // 更新时 excludeSelf = true 新增时 excludeSelf = false

        public bool IsDuplicate(MetamorphicRelation metamorphicRelation, bool excludeSelf = false)
        {
            using (var db = new LiteDatabase(_conn))
            {
                var collection = db.GetCollection<MetamorphicRelation>(_dbConfig.MetamorphicRelations_Collection_Key);
                // 避免没有设置唯一复合索引
                collection.EnsureIndex("MR_Idx", x => new { x.InputPattern, x.OutputPattern, x.ApplicationName }, true);
                var query = Query.And(
                    Query.EQ("InputPattern", metamorphicRelation.InputPattern),
                    Query.EQ("OutputPattern", metamorphicRelation.OutputPattern),
                    Query.EQ("ApplicationName", metamorphicRelation.ApplicationName)
                );

                if (excludeSelf && metamorphicRelation.IdMR != 0)
                {
                    query = Query.And(query, Query.Not("_id", metamorphicRelation.IdMR));
                }

                return collection.Exists(query);
            }
        }
    }
}