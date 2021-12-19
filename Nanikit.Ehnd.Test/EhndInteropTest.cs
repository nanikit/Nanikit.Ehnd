namespace Nanikit.Ehnd.Test {
  using System.Linq;
  using Xunit;
  using Xunit.Abstractions;

  public class EhndInteropTest {
    private readonly ITestOutputHelper _output;

    public EhndInteropTest(ITestOutputHelper output) {
      _output = output;
    }

    [Fact]
    public void TestSearchPaths() {
      var paths = EhndInterop.GetDllSearchPaths("a").ToList();
      foreach (var path in paths) {
        _output.WriteLine(path);
      }

      Assert.Equal("a", paths[0]);
      Assert.True(paths.Count > 1);
    }
  }
}
