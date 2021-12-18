#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Nanikit.Ehnd {
  public interface IEhnd {
    Task<string> TranslateAsync(string japanese, CancellationToken? cancellationToken = null);
  }

  /// <summary>
  /// Ehnd C# binding
  /// </summary>
  public class Ehnd : IEhnd {
    public static readonly string DllName = EhndInterop.DllName;

    /// <summary>
    /// Returns guessed eztrans installed directory.
    /// </summary>
    public static string? GetEztransDirFromReg() {
      return EhndInterop.GetEztransDirFromReg();
    }

    /// <summary>
    /// Load ehnd. Throws EhndException if failed.
    /// </summary>
    /// <param name="dllPath">J2KEngine.dll path</param>
    public Ehnd(string? dllPath = null) {
      IntPtr eztransDll = EhndInterop.LoadDll(dllPath);
      _j2kFree = EhndInterop.GetFuncAddress<J2K_FreeMem>(eztransDll, "J2K_FreeMem");
      _j2kMmntw = EhndInterop.GetFuncAddress<J2K_TranslateMMNTW>(eztransDll, "J2K_TranslateMMNTW");
    }

    /// <summary>
    /// Translate by ehnd immediately. Not thread safe.
    /// </summary>
    public string Translate(string japanese) {
      var escaper = new EztransEscaper();
      string escaped = escaper.Escape(japanese);
      IntPtr p = _j2kMmntw(0, escaped);
      if (p == IntPtr.Zero) {
        throw new EhndException("이지트랜스에서 알 수 없는 오류가 발생했습니다");
      }
      string? ret = Marshal.PtrToStringAuto(p);
      _j2kFree(p);
      return escaper.Unescape(ret ?? "");
    }

    /// <summary>
    /// Queue translation to ehnd. Thread safe.
    /// </summary>
    public async Task<string> TranslateAsync(string japanese, CancellationToken? cancellationToken = null) {
      await _semaphore.WaitAsync(cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

      try {
        return Translate(japanese);
      }
      finally {
        _semaphore.Release();
      }
    }

    /// <summary>
    /// Returns true if Hdor dictionary is installed. Not thread safe.
    /// </summary>
    public bool IsHdorEnabled() {
      string? chk = Translate("蜜ドル辞典");
      return chk?.Contains("OK") ?? false;
    }

    private readonly J2K_FreeMem _j2kFree;
    private readonly J2K_TranslateMMNTW _j2kMmntw;

    private readonly SemaphoreSlim _semaphore = new(1);

    // FreeLibrary를 호출하면 Access violation이 뜬다.

    #region PInvoke
    internal delegate void J2K_FreeMem(IntPtr ptr);
    internal delegate IntPtr J2K_TranslateMMNTW(int data0, [MarshalAs(UnmanagedType.LPWStr)] string jpStr);
    #endregion
  }
}
