using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Nanikit.Ehnd.Test {

  [TestClass]
  public class EhndInteropTest {

    [TestMethod]
    public void TestSearchPaths() {
      var paths = EhndInterop.GetDllSearchPaths("a").ToList();
      foreach (string? path in paths) {
        Trace.WriteLine(path);
      }

      Assert.AreEqual("a", paths[0]);
      CollectionAssert.Contains(paths, @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP\J2KEngine.dll");
    }
  }
}
