using LiteDB;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using static System.Net.Mime.MediaTypeNames;

namespace MetBench_Domain
{   //蜕变关系类 (MR Schema, L2 in v2 4-level hierarchy)
    //v1 method-level MT 与 v2 system-level MT 共用同一实体，由 Kind 字段区分
    public class MetamorphicRelation
    {
        [BsonId]
        public int IdMR { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Constraint { get; set; } = string.Empty;
        public string OrderOfMR { get; set; } = string.Empty;
        public RtType RepresentationType { get; set; }
        public string InputPattern { get; set; } = string.Empty;
        public string OutputPattern { get; set; } = string.Empty;

        #region
        //Latex转sympy格式
        public string InputPatterntosympy { get; set; } = string.Empty;
        public string OutputPatterntosympy { get; set; } = string.Empty;
        //InputPatternimage属性
        public byte[] InputPatternImageData { get; set; } = Array.Empty<byte>();
        //OutputPatternimage属性
        public byte[] OutputPatternImageData { get; set; } = Array.Empty<byte>();
        #endregion

        public string DimensionOfInputPattern { get; set; } = string.Empty;
        public string DimensionOfOutputPattern { get; set; } = string.Empty;

        public string Granularity { get; set; } = string.Empty; //粒度
        public string Hierarchy { get; set; } = string.Empty; //层次 这个是MR推荐的关键属性
        public string Operator { get; set; } = string.Empty; // 运算符
        public string Expression { get; set; } = string.Empty; // 表达式 分为线性和非线性

        // ===== v1 反模式（保留读取兼容，标 Obsolete；v2 由 MRBindings collection 取代） =====
        //Application的Name 作为外键
        //AppllicationName之间用:隔开
        [Obsolete("Use MRBindings collection instead. Kept for v1 read compatibility only.")]
        public string ApplicationName { get; set; } = String.Empty; //Litedb不存储空字符串

        // ===== v2 新增字段（系统级 MT 专用；v1 行默认值为空） =====

        /// <summary>简码（如 "MR-T"），v2 系统级 MR 必填；v1 行默认空。</summary>
        public string Code { get; set; } = String.Empty;

        /// <summary>NOETHER MetaPattern 代码：m_inv / m_mono / m_conv / m_cmp / m_adj / m_rev / m_dyn / m_rel</summary>
        public string MetaPatternCode { get; set; } = String.Empty;

        /// <summary>使用哪个 IMRTransformation 实现（如 "ScaleField"）。</summary>
        public string TransformationName { get; set; } = String.Empty;

        /// <summary>断言类型代码（见 AssertionTypeCodes，如 "less-noise-aware"）。</summary>
        public string AssertionTypeCode { get; set; } = String.Empty;

        /// <summary>断言检查的输出字段名（如 "k_eff"）。</summary>
        public string ValueName { get; set; } = String.Empty;

        /// <summary>是否使用噪声感知断言（概率 SUT 必需）。</summary>
        public bool NoiseAware { get; set; }

        /// <summary>相对容差（0 = 严格）。</summary>
        public double ToleranceRel { get; set; }

        /// <summary>噪声底乘子（默认 3.0 = 3σ）。</summary>
        public double NoiseMultiplier { get; set; } = 3.0;

        /// <summary>对应 .feature 文件路径（视图，非真理源）。</summary>
        public string FeatureFilePath { get; set; } = String.Empty;

        /// <summary>来自哪个 Discovery Run（若由 Discovery 子系统提议；手动写入则空）。</summary>
        public Guid? DiscoveryRunId { get; set; }

        /// <summary>Discovery 方法给的 confidence 分数（0-1）。</summary>
        public double? DiscoveryConfidence { get; set; }

        /// <summary>Discovery 方法名：manual / metapattern / llm-native / ...</summary>
        public string DiscoveryMethod { get; set; } = "manual";

        /// <summary>记录创建时间（v1 行默认 MinValue）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>创建者标识。</summary>
        public string CreatedBy { get; set; } = String.Empty;

        /// <summary>"method-level"（v1）或 "system-level"（v2）。</summary>
        public string Kind { get; set; } = "method-level";

        // ===== mr-architecture P0 新增字段（详见 docs/design/mr-architecture.md §4.2 §7） =====

        /// <summary>
        /// 所属方程的稳定业务键(如 <c>bateman</c> / <c>heat-equation-1d</c>),
        /// 与 <c>EquationMetadata.EquationKey</c> 对应。空字符串表示"未关联方程"
        /// (向后兼容既有 v2 数据 + v1 method-level 行;v1 行默认空)。
        /// 设计:`mr-architecture.md` §4.2。
        /// </summary>
        public string EquationKey { get; set; } = String.Empty;

        /// <summary>
        /// MR 操作的数据形态:<c>scalar</c>(默认) / <c>vector</c> / <c>field</c> /
        /// <c>sample-set</c> / <c>case-set</c>。当前所有既有 MR 都是 scalar;预留以支持
        /// 集合形态 L3/L4 演化(`mr-architecture.md` §7)。
        /// </summary>
        public string ValueShape { get; set; } = "scalar";
    }
}