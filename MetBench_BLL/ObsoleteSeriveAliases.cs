using MetBench_BLL.Visualization;
using MetBench_IDAL;

namespace MetBench_BLL
{
    // ============================================================================
    // F3a: 老类名拼写错（"Serive" 应为 "Service"）。
    // 重命名已完成（PR #40 cloud-F3a）；但 MetBench_Client (WPF) 仍引用老名字 ——
    // 此文件提供 [Obsolete] 兼容别名让 WPF 端在 PR-VM-7 跟进改名之前继续可编译。
    //
    // 下游迁移：把 `XxxSerive` 改为 `XxxService`，然后 PR-VM-7 把本文件删除。
    // ============================================================================

    [Obsolete("Renamed to ApplicationService. Will be removed after PR-VM-7.", error: false)]
    public sealed class ApplicationSerive : ApplicationService
    {
        public ApplicationSerive(IApplicationRepository applicationRepository)
            : base(applicationRepository) { }
    }

    [Obsolete("Renamed to DomainService. Will be removed after PR-VM-7.", error: false)]
    public sealed class DomainSerive : DomainService
    {
        public DomainSerive(IDomainRepository domain_Repository)
            : base(domain_Repository) { }
    }

    [Obsolete("Renamed to MetamorphicRelationService. Will be removed after PR-VM-7.", error: false)]
    public sealed class MetamorphicRelationSerive : MetamorphicRelationService
    {
        public MetamorphicRelationSerive(
            IMetamorphicRelationRepository metamorphicRelationRepository,
            IApplicationRepository applicationRepository)
            : base(metamorphicRelationRepository, applicationRepository) { }
    }

    [Obsolete("Renamed to MTVisualizationService. Will be removed after PR-VM-7.", error: false)]
    public sealed class MTVisualizationSerive : MTVisualizationService
    {
    }

    [Obsolete("Renamed to MTReportGeneratorService. Will be removed after PR-VM-7.", error: false)]
    public sealed class MTReportGeneratorSerive : MTReportGeneratorService
    {
    }

    [Obsolete("Renamed to MRRecommendationService. Will be removed after PR-VM-7.", error: false)]
    public sealed class MRRecommendationSerive : MRRecommendationService
    {
        public MRRecommendationSerive(
            IPreprocessor preprocessor,
            ISyntaxSimilarityDetector syntaxDetector,
            ISemanticSimilarityDetector semanticDetector,
            SimilarityResultFilter filter,
            SupportRateCalculator supportRateCalculator)
            : base(preprocessor, syntaxDetector, semanticDetector, filter, supportRateCalculator) { }
    }
}
