using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nanikit.Ehnd.Test {

  [TestCategory("NoCi")]
  [TestClass]
  public class EhndTest {
    private readonly Ehnd ehnd;

    public EhndTest() {
      ehnd = new Ehnd();
    }

    [TestMethod]
    public void TestHdor() {
      Assert.AreEqual("꿀", ehnd.Translate("蜜"));
      Assert.IsTrue(ehnd.IsHdorEnabled());
    }

    [TestMethod]
    public async Task TestParallelism() {
      string japanese = "ご支援に対する感謝のしるしとして提供するもので、商品として販売しているものではありません。";
      string korean = "지원에 대한 감사의 표시로서 제공해서, 상품으로서 판매하고 있는 것이 아닙니다.";
      var tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => ehnd.TranslateAsync(japanese));
      }).ToArray();

      string[] result = await Task.WhenAll(tasks).ConfigureAwait(false);

      CollectionAssert.AreEqual(Enumerable.Repeat(korean, 100).ToArray(), result);
    }

    [TestMethod]
    public void TestSymbolPreservation() {
      TestPreservation("-----");
      TestPreservation("#####");
      TestPreservation("―――――");
      TestPreservation("─────");
      TestPreservation("--##――@@--");
    }

    [TestMethod]
    public void TestWhitespacePreservation1() {
      TestPreservation("\r");
    }

    [TestMethod]
    public void TestWhitespacePreservation2() {
      TestPreservation("\n\nd");
    }

    [TestMethod]
    public void TestWhitespacePreservation3() {
      TestPreservation("\r\n");
    }

    [TestMethod]
    public void TestWhitespacePreservation4() {
      TestPreservation("\n\n\n 　\n\n");
    }

    private void TestPreservation(string str) {
      Assert.AreEqual(str, ehnd.Translate(str));
    }
  }
}
