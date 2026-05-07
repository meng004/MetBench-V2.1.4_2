using LiteDB;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using static System.Net.Mime.MediaTypeNames;

namespace MetBench_Domain
{   //蜕变关系类
    public class MetamorphicRelation 
    {
        [BsonId]
        public int IdMR { get; set; }
        public string Description { get; set; }
        public string Context { get; set; }
        public string Constraint { get; set; }
        public string OrderOfMR { get; set; }
        public RtType RepresentationType { get; set; }
        public string InputPattern { get; set; }
        public string OutputPattern { get; set; }

        #region
        //Latex转sympy格式
        public string InputPatterntosympy { get; set; }
        public string OutputPatterntosympy { get; set; }
        //InputPatternimage属性
        public byte[] InputPatternImageData { get; set; }
        //OutputPatternimage属性
        public byte[] OutputPatternImageData { get; set; }
        #endregion

        public string DimensionOfInputPattern { get; set; }
        public string DimensionOfOutputPattern { get; set; } 

        public string Granularity { get; set; } //粒度
        public string Hierarchy { get; set; } //层次 这个是MR推荐的关键属性
        public string Operator { get; set; } // 运算符
        public string Expression { get; set; } // 表达式 分为线性和非线性

        //Application的Name 作为外键
        public string ApplicationName { get; set; } = String.Empty; //Litedb不存储空字符串  AppllicationName之间用:隔开
    }
}