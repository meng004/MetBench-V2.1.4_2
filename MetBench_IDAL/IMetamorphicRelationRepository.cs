using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL
{   //蜕变关系仓库接口
    public interface IMetamorphicRelationRepository:IRepository<MetamorphicRelation>
    {
         DatatoImage datatoImage { get ; set; }
        // 三表连接查询
        ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX();

        //组合条件查询
        ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation metamorphicRelation, string domainNames);
        
        ////添加 并返回添加后的id
        //int Add_Return_ID(MetamorphicRelation mr);

        //两表连接查询
        public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable();

        public bool IsDuplicate(MetamorphicRelation metamorphicRelation, bool excludeSelf = false);
    }
}
