using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Nanikit.Ehnd.Test {

  [TestClass]
  public class BatchEhndTest {
    private readonly string _japanese = "ご支援に対する感謝のしるしとして提供するもので、商品として販売しているものではありません。";
    private readonly string _korean = "지원에 대한 감사의 표시로서 제공해서, 상품으로서 판매하고 있는 것이 아닙니다.";

    [TestMethod]
    public async Task TestException() {
      var batch = new BatchEhnd(new ThrowingEhnd());

      await Assert.ThrowsExceptionAsync<BatchTestException>(() => batch.TranslateAsync(_japanese));
    }

    [TestMethod]
    public async Task TestMerge() {
      var mock = new EhndMock();
      var batch = new BatchEhnd(mock);
      var tasks = Enumerable.Range(0, 100).Select((_) => {
        return batch.TranslateAsync(_japanese);
      }).ToArray();

      string[] result = await Task.WhenAll(tasks).ConfigureAwait(false);

      CollectionAssert.AreEqual(Enumerable.Repeat(_japanese, 100).ToArray(), result);
      Assert.IsFalse(mock.Receivals.All(x => x.Length == _japanese.Length), "No batch was done");
    }

    [TestCategory("NoCi")]
    [TestMethod]
    public async Task TestPerformance() {
      var ehnd = new Ehnd();
      var batch = new BatchEhnd(ehnd);

      var watch = new Stopwatch();
      watch.Start();

      var tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => batch.TranslateAsync(_japanese));
      }).ToArray();

      string[] result = await Task.WhenAll(tasks).ConfigureAwait(false);
      watch.Stop();

      CollectionAssert.AreEqual(Enumerable.Repeat(_korean, 100).ToArray(), result);
      Trace.WriteLine($"Batch elapsed: {watch.Elapsed}");
      var batchTime = watch.Elapsed;

      watch.Restart();
      tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => ehnd.TranslateAsync(_japanese));
      }).ToArray();

      result = await Task.WhenAll(tasks).ConfigureAwait(false);
      watch.Stop();

      CollectionAssert.AreEqual(Enumerable.Repeat(_korean, 100).ToArray(), result);
      Trace.WriteLine($"Raw elapsed: {watch.Elapsed}");
      var rawTime = watch.Elapsed;

      Assert.IsTrue(batchTime < rawTime, "No performance gain");
    }
  }

  internal class BatchTestException : Exception { }

  internal class EhndMock : IEhnd {
    public List<string> Receivals = [];

    public async Task<string> TranslateAsync(string japanese, CancellationToken? cancellationToken = null) {
      Receivals.Add(japanese);
      Trace.WriteLine($"- {japanese}");
      await Task.Yield();
      return japanese;
    }
  }

  internal class ThrowingEhnd : IEhnd {

    public Task<string> TranslateAsync(string japanese, CancellationToken? cancellationToken = null) {
      throw new BatchTestException();
    }
  }
}
