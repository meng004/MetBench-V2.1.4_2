using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_BLL
{
    public class ParsedResult
    {
        //识别的程序名称
        public string ProgramName { get; set; } = string.Empty;
        //识别的程序
        public Application Application { get; set; }
        //识别到的MR数据
        public ObservableCollection<MetamorphicRelation> MetamorphicRelations { get; set; }
    }
}
