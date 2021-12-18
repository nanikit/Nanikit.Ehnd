using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Nanikit.Ehnd.Test {
  public class BatchEhndTest {
    private readonly Ehnd _ehnd;

    private readonly string _japanese = "ご支援に対する感謝のしるしとして提供するもので、商品として販売しているものではありません。";
    private readonly string _korean = "지원에 대한 감사의 표시로서 제공해서, 상품으로서 판매하고 있는 것이 아닙니다.";
    private readonly ITestOutputHelper _output;

    public BatchEhndTest(ITestOutputHelper output) {
      _output = output;
      _ehnd = new Ehnd();
    }

    [Fact]
    public void TestMerge() {
      var mock = new EhndMock(_output);
      var batch = new BatchEhnd(mock);
      Task<string>[] tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => batch.TranslateAsync(_japanese));
      }).ToArray();

      Task.WaitAll(tasks);

      Assert.All(tasks, task => {
        Assert.Equal(_japanese, task.Result);
      });

      Assert.True(mock.Receivals.Any(x => x.Length != _japanese.Length), "No batch was done");
    }

    [Fact]
    public void TestPerformance() {
      var batch = new BatchEhnd(_ehnd);

      var watch = new Stopwatch();
      watch.Start();

      Task<string>[] tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => batch.TranslateAsync(_japanese));
      }).ToArray();

      Task.WaitAll(tasks);
      watch.Stop();

      Assert.All(tasks, task => {
        Assert.Equal(_korean, task.Result);
      });
      _output.WriteLine($"Batch elapsed: {watch.Elapsed}");
      TimeSpan batchTime = watch.Elapsed;

      watch.Restart();
      tasks = Enumerable.Range(0, 100).Select((_) => {
        return Task.Run(() => _ehnd.TranslateAsync(_japanese));
      }).ToArray();

      Task.WaitAll(tasks);
      watch.Stop();

      Assert.All(tasks, task => {
        Assert.Equal(_korean, task.Result);
      });
      _output.WriteLine($"Raw elapsed: {watch.Elapsed}");
      TimeSpan rawTime = watch.Elapsed;

      Assert.True(batchTime < rawTime, "No performance gain");
    }
  }

  class EhndMock : IEhnd {
    private readonly ITestOutputHelper _output;
    public EhndMock(ITestOutputHelper output) {
      _output = output;
    }

    public List<string> Receivals = new();

    public Task<string> TranslateAsync(string japanese, CancellationToken? cancellationToken = null) {
      Receivals.Add(japanese);
      _output.WriteLine($"- {japanese}");
      return Task.FromResult(japanese);
    }
  }
}
