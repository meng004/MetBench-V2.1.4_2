using System.IO;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class MutationCampaignPageXamlTests
{
    [Fact]
    public void Mutation_summary_does_not_bind_localized_labels_to_run_text()
    {
        var xaml = File.ReadAllText(FindRepoFile("MetBench_Client/Views/Pages/MutationCampaignPage.xaml"));

        Assert.DoesNotContain("<Run Text=\"{Binding ViewModel.Localization[Mutation_Summary", xaml);
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.");
    }
}
