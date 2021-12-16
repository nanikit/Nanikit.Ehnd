using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nanikit.Ehnd.Test {
  public class EhndTest {
    readonly Ehnd ehnd;

    public EhndTest() {
      ehnd = new Ehnd();
    }

    [Fact]
    public void TestHdor() {
      Assert.Equal("꿀", ehnd.Translate("蜜"));
      Assert.True(ehnd.IsHdorEnabled());
    }

    [Fact]
    public void TestSymbolPreservation() {
      TestPreservation("-----");
      TestPreservation("#####");
      TestPreservation("―――――");
      TestPreservation("─────");
      TestPreservation("--##――@@--");
    }

    [Fact]
    public void TestWhitespacePreservation1() {
      TestPreservation("\r");
    }

    [Fact]
    public void TestWhitespacePreservation2() {
      TestPreservation("\n\nd");
    }

    [Fact]
    public void TestWhitespacePreservation3() {
      TestPreservation("\r\n");
    }

    [Fact]
    public void TestWhitespacePreservation4() {
      TestPreservation("\n\n\n 　\n\n");
    }

    [Fact]
    public void TestParallelism() {
      string japanese = "ご支援に対する感謝のしるしとして提供するもので、商品として販売しているものではありません。";
      string korean = "지원에 대한 감사의 표시로서 제공해서, 상품으로서 판매하고 있는 것이 아닙니다.";
      Task<string>[] tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => ehnd.TranslateAsync(japanese));
      }).ToArray();

      Task.WaitAll(tasks);

      Assert.All(tasks, task => {
        Assert.Equal(korean, task.Result);
      });
    }

    private void TestPreservation(string str) {
      Assert.Equal(str, ehnd.Translate(str));
    }
  }
}
