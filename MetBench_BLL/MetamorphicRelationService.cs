using MetBench_Domain;
using MetBench_IDAL;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public class MetamorphicRelationService
    {
        private IMetamorphicRelationRepository MR_repository ;
        private IApplicationRepository Application_repository ;

        //依赖注入实体
        public MetamorphicRelationService(IMetamorphicRelationRepository metamorphicRelationRepository, IApplicationRepository applicationRepository) 
        {
            MR_repository = metamorphicRelationRepository;
            Application_repository = applicationRepository;
        }

        /// <summary>
        /// 获取蜕变关系
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MetamorphicRelation GetMRById(int id)
        {
            var result = MR_repository.Get(id);
            return result;
        }

        /// <summary>
        /// 添加服务
        /// </summary>
        /// <param name="metamorphicRelation"></param>
        /// <returns>0 表示失败 1 表示重复  2表示成功 </returns>
        public async Task<int> AddService(MetamorphicRelation metamorphicRelation)
        {
            // 查重
            var isexist = MR_repository.IsDuplicate(metamorphicRelation, false);
            if (isexist) 
            {
                return 1;
            }
            // 转换Sympy格式
            var latextosympy_Await = new Latextosympy_Await();
            metamorphicRelation.InputPatterntosympy = await latextosympy_Await.LatextosympyAsync(metamorphicRelation.InputPattern);
            string inputPatternimagepath = await latextosympy_Await.LatextoImageAsync(metamorphicRelation.InputPattern);
            metamorphicRelation.InputPatternImageData = latextosympy_Await.ConvertImageToByteArray(inputPatternimagepath);
            // 转换Sympy格式
            metamorphicRelation.OutputPatterntosympy = await latextosympy_Await.LatextosympyAsync(metamorphicRelation.OutputPattern);
            string outputPatternimagepath = await latextosympy_Await.LatextoImageAsync(metamorphicRelation.OutputPattern);
            metamorphicRelation.OutputPatternImageData = latextosympy_Await.ConvertImageToByteArray(outputPatternimagepath);
            // 添加
            var result = MR_repository.Add(metamorphicRelation);
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
        /// <param name="metamorphicRelation"></param>
        /// <returns></returns>
        public bool DeleteService(MetamorphicRelation metamorphicRelation)
        {
            var result = MR_repository.Remove(metamorphicRelation);
            return result;
        }

        /// <summary>
        /// 查询服务
        /// </summary>
        /// <param name="metamorphicRelation"></param>
        /// <param name="domainNames"></param>
        /// <returns></returns>
        public ObservableCollection<MetamorphicRelations_QueryResultData> QueryService(MetamorphicRelation metamorphicRelation, string domainName)
        {
            var result = MR_repository.Get(metamorphicRelation, domainName);
            return result;
        }

        /// <summary>
        /// 两表联合查询
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<MetamorphicRelations_QueryResultData> showMultTwoTableResult()
        {
            var result = MR_repository.GetAll_MIXTwoTable();
            return result;
        }

        /// <summary>
        /// 三表联合查询
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<MetamorphicRelations_QueryResultData> showMultThreeTableResult()
        {
            var result = MR_repository.GetAll_MIX();
            return result;
        }

        /// <summary>
        /// 更新服务
        /// </summary>
        /// <param name="metamorphicRelation"></param>
        /// <returns>0 表示失败 1 表示重复  2表示成功 </returns>
        public async Task<int> UpdateService(MetamorphicRelation metamorphicRelation)
        {
            // 查重
            var isexist = MR_repository.IsDuplicate(metamorphicRelation, true);
            if (isexist)
            {
                return 1;
            }
            // 转换Sympy格式
            var latextosympy_Await = new Latextosympy_Await();
            metamorphicRelation.InputPatterntosympy = await latextosympy_Await.LatextosympyAsync(metamorphicRelation.InputPattern);
            string inputPatternimagepath = await latextosympy_Await.LatextoImageAsync(metamorphicRelation.InputPattern);
            metamorphicRelation.InputPatternImageData = latextosympy_Await.ConvertImageToByteArray(inputPatternimagepath);
            // 转换Sympy格式
            metamorphicRelation.OutputPatterntosympy = await latextosympy_Await.LatextosympyAsync(metamorphicRelation.OutputPattern);
            string outputPatternimagepath = await latextosympy_Await.LatextoImageAsync(metamorphicRelation.OutputPattern);
            metamorphicRelation.OutputPatternImageData = latextosympy_Await.ConvertImageToByteArray(outputPatternimagepath);
            // 修改
            var result = MR_repository.Modify(metamorphicRelation);
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
        /// 获取全部蜕变关系实体
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<MetamorphicRelation> GetAllMRs()
        {
            var result = MR_repository.GetAll();
            return result;

        }
    }
}
