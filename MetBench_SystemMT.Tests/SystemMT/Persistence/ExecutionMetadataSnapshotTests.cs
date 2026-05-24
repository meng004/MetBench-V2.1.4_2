using System;
using System.Collections.Generic;
using MetBench_BLL.SystemMT.Persistence;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Persistence;

public sealed class ExecutionMetadataSnapshotTests
{
    [Fact]
    public void ExecutionMetadataSnapshot_captures_5d_tags_and_V3_ref()
    {
        var v3Id = Guid.NewGuid();
        var snap = new ExecutionMetadataSnapshot
        {
            MrId = "openmoc-pincell-nu-sigma-f",
            V3MrIdRef = v3Id,
            SutName = "openmoc",
            Equation = "Boltzmann",
            ProgramType = "Num",
            MetaPattern = "Mono",
            SourceLevel = "Manual",
            FailureCorrelation = "None",
            CatalogManifestPath = "SUT/openmoc/catalog.json",
            MetbenchVersion = "v2.2-dev",
        };

        Assert.Equal("openmoc-pincell-nu-sigma-f", snap.MrId);
        Assert.Equal(v3Id, snap.V3MrIdRef);
        Assert.Equal("Boltzmann", snap.Equation);
        Assert.Equal("Mono", snap.MetaPattern);
        Assert.Equal("SUT/openmoc/catalog.json", snap.CatalogManifestPath);
    }

    [Fact]
    public void ExecutionSampleTrace_carries_per_variable_source_transformed_output_triple()
    {
        var trace = new ExecutionSampleTrace
        {
            VariableName = "amplitude",
            Path = "/initial/amplitude",
            SourceValueJson = "1.0",
            TransformedValueJson = "2.0",
            OutputValueJson = "2.0",
        };

        Assert.Equal("/initial/amplitude", trace.Path);
        Assert.Equal("1.0", trace.SourceValueJson);
        Assert.Equal("2.0", trace.TransformedValueJson);
        Assert.Equal("2.0", trace.OutputValueJson);
    }

    [Fact]
    public void ExecutionEvidence_aggregates_metadata_traces_and_transformation_parameters()
    {
        var ev = new ExecutionEvidence
        {
            IdEvidence = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            Metadata = new ExecutionMetadataSnapshot
            {
                MrId = "heat-equation-amplitude",
                SutName = "heat-equation",
                Equation = "Fourier",
            },
            SampleTraces =
            {
                new ExecutionSampleTrace
                {
                    VariableName = "amplitude",
                    Path = "/initial/amplitude",
                    SourceValueJson = "1.0",
                    TransformedValueJson = "2.0",
                    OutputValueJson = "2.0",
                },
            },
            TransformationParameters = new Dictionary<string, string> { ["factor"] = "2" },
        };

        Assert.NotEqual(Guid.Empty, ev.IdEvidence);
        Assert.NotEqual(Guid.Empty, ev.ExecutionId);
        Assert.Single(ev.SampleTraces);
        Assert.Equal("2", ev.TransformationParameters["factor"]);
        Assert.Equal("heat-equation-amplitude", ev.Metadata.MrId);
    }

    [Fact]
    public void ExecutionEvidence_RecordedAtUtc_defaults_to_UtcNow()
    {
        var before = DateTime.UtcNow;
        var ev = new ExecutionEvidence();
        var after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, ev.RecordedAtUtc.Kind);
        Assert.InRange(ev.RecordedAtUtc, before, after);
    }
}
