using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Nanikit.Ehnd {

  /// <summary>
  /// It merge / split translation works for single threaded deterministic translator.
  /// </summary>
  public class BatchEhnd : IEhnd, IDisposable {
    private readonly CancellationTokenSource _cancellation = new();

    private readonly IEhnd _ehnd;

    private readonly Channel<Work> _works = Channel.CreateUnbounded<Work>(new UnboundedChannelOptions() {
      SingleReader = true,
      SingleWriter = true,
      AllowSynchronousContinuations = true,
    });

    public BatchEhnd(IEhnd ehnd) {
      _ehnd = ehnd;
      _ = ProcessQueue();
    }

    public void Dispose() {
      _cancellation.Cancel();
      _cancellation.Dispose();
      GC.SuppressFinalize(this);
    }

    public async Task<string> TranslateAsync(string japanese, CancellationToken? cancellationToken = null) {
      var work = new Work(japanese);
      await _works.Writer.WriteAsync(work).ConfigureAwait(false);
      return await work.Client.Task;
    }

    private static int GetNthNewlineFrom(string merged, int newlineCount, int startIdx) {
      int start = startIdx;
      int idx = merged.Length;
      for (int i = 0; i <= newlineCount; i++) {
        idx = merged.IndexOf('\n', start);
        if (idx == -1) {
          return merged.Length;
        }
        start = idx + 1;
      }
      return idx;
    }

    private static IEnumerable<string> SplitBySegmentNewline(string merged, IEnumerable<string> texts) {
      int startIdx = 0;
      foreach (string text in texts) {
        int newlineCount = text.Count((c) => c == '\n');
        int endIdx = GetNthNewlineFrom(merged, newlineCount, startIdx);
        yield return merged.Substring(startIdx, endIdx - startIdx);
        startIdx = endIdx + 1;
      }
    }

    private async Task<List<Work>> GetWorksOfThisRound(CancellationToken token) {
      var works = new List<Work>();
      long length = 0;
      var reader = _works.Reader;

      await reader.WaitToReadAsync(token).ConfigureAwait(false);
      while (length < 4000 && reader.TryRead(out var work)) {
        length += work.Text.Length;
        works.Add(work);
      }

      return works;
    }

    private async Task ProcessQueue() {
      var token = _cancellation.Token;
      while (!token.IsCancellationRequested) {
        var works = await GetWorksOfThisRound(token).ConfigureAwait(false);

        var texts = works.Select(x => x.Text);
        string mergedStart = string.Join("\n", texts);

        try {
          string mergedEnd = await _ehnd.TranslateAsync(mergedStart).ConfigureAwait(false) ?? "";

          string[] translateds = SplitBySegmentNewline(mergedEnd, texts).ToArray();

          for (int i = 0; i < works.Count; i++) {
            works[i].Client.TrySetResult(translateds[i]);
          }
        }
        catch (Exception exception) {
          for (int i = 0; i < works.Count; i++) {
            works[i].Client.TrySetException(exception);
          }
        }
      }
    }

    private class Work {
      public readonly TaskCompletionSource<string> Client;
      public readonly string Text;

      public Work(string text) {
        Text = text;
        Client = new TaskCompletionSource<string>();
      }
    }
  }
}
