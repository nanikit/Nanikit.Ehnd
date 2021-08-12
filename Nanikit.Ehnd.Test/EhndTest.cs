using System.Threading.Tasks;
using Xunit;

namespace Nanikit.Ehnd.Test {
  public class EhndTest {
    readonly Ehnd ehnd;

    public EhndTest() {
      ehnd = new Ehnd();
      Assert.Equal("��", ehnd.Translate("��").GetAwaiter().GetResult());
    }

    private void TestPreservation(string str) {
      Task<string> t = ehnd.Translate(str);
      t.Wait();
      Assert.Equal(str, t.Result);
    }

    [Fact]
    public void SymbolPreservationTest() {
      TestPreservation("-----");
      TestPreservation("#####");
      TestPreservation("����������");
      TestPreservation("����������");
      TestPreservation("--##����@@--");
    }

    [Fact]
    public void WhitespacePreservationTest1() {
      TestPreservation("\r");
    }

    [Fact]
    public void WhitespacePreservationTest2() {
      TestPreservation("\n\nd");
    }

    [Fact]
    public void WhitespacePreservationTest3() {
      TestPreservation("\r\n");
    }

    [Fact]
    public void WhitespacePreservationTest4() {
      TestPreservation("\n\n\n ��\n\n");
    }

    [Fact]
    public void InitializationTest() {
   
    }
  }
}
