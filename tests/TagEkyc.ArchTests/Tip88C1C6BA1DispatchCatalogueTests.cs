using System.Text.RegularExpressions;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C6BA1DispatchCatalogueTests
{
    [Fact]
    public void DispatchCatalogue_IsClosedAndBidirectionallyJoined()
    {
        A1CatalogueJoinProof.Verify(FindRepositoryRoot());
    }


    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing TagEkyc.sln was not found.");
    }
}
