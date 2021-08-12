#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nanikit.Ehnd {
  public class EhndException : Exception {
    public EhndException(string message) : base(message) { }
  }

  public class EhndNotFoundException : EhndException {
    public EhndNotFoundException(string message) : base($"Ehnd를 찾지 못했습니다{message}") { }
  }

  public class Ehnd {

    public static async Task<Ehnd> Create(string? eztPath = null, int msDelay = 200) {
      var exceptions = new Dictionary<string, Exception>();
      foreach (string path in GetEztransDirs(eztPath)) {
        if (!File.Exists(Path.Combine(path, "J2KEngine.dll"))) {
          continue;
        }
        try {
          IntPtr eztransDll = await LoadNativeDll(path, msDelay).ConfigureAwait(false);
          return new Ehnd(eztransDll);
        }
        catch (Exception e) {
          exceptions.Add(path, e);
        }
      }

      string detail = string.Join("", exceptions.Select(x => $"\n  {x.Key}: {x.Value.Message}"));
      throw new EhndNotFoundException(detail);
    }

    private static IEnumerable<string> GetEztransDirs(string? path) {
      var paths = new List<string>();

      if (path != null) {
        paths.Add(path);
      }

      string? regPath = GetEztransDirFromReg();
      if (regPath != null) {
        paths.Add(regPath);
      }

      string defPath = @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP";
      paths.Add(defPath);
      paths.AddRange(GetAssemblyParentDirectories());

      return paths.Distinct();
    }

    public static string? GetEztransDirFromReg() {
      RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
      return key.OpenSubKey(@"Software\ChangShin\ezTrans")?.GetValue(@"FilePath") as string;
    }

    private static IEnumerable<string> GetAssemblyParentDirectories() {
      var assembly = System.Reflection.Assembly.GetEntryAssembly();
      string child = assembly?.Location ?? Directory.GetCurrentDirectory();
      while (true) {
        string? parent = Path.GetDirectoryName(child);
        if (parent == null) {
          break;
        }
        yield return parent;
        child = parent;
      }
    }

    private static async Task<IntPtr> LoadNativeDll(string eztPath, int msDelay) {
      string path = GetDllPath(eztPath);
      IntPtr dll = NativeLibrary.Load(path);
      if (dll == IntPtr.Zero) {
        int errorCode = Marshal.GetLastWin32Error();
        throw new EhndException($"라이브러리 로드 실패(에러 코드: {errorCode})");
      }

      await Task.Delay(msDelay).ConfigureAwait(false);
      string key = Path.Combine(eztPath, "Dat");
      var initEx = GetFuncAddress<J2K_InitializeEx>(dll, "J2K_InitializeEx");
      if (!initEx("CSUSER123455", key)) {
        throw new EhndException("엔진 초기화에 실패했습니다.");
      }

      return dll;
    }

    private static string GetDllPath(string eztPath) {
      return Path.Combine(eztPath, "J2KEngine.dll");
    }

    private static T GetFuncAddress<T>(IntPtr dll, string name) {
      IntPtr addr = NativeLibrary.GetExport(dll, name);
      if (addr == IntPtr.Zero) {
        throw new EhndException("Ehnd 파일이 아닙니다.");
      }
      return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }


    private readonly J2K_FreeMem J2kFree;
    private readonly J2K_TranslateMMNTW J2kMmntw;

    private Ehnd(IntPtr eztransDll) {
      J2kMmntw = GetFuncAddress<J2K_TranslateMMNTW>(eztransDll, "J2K_TranslateMMNTW");
      J2kFree = GetFuncAddress<J2K_FreeMem>(eztransDll, "J2K_FreeMem");
    }

    public Task<string> Translate(string jpStr) {
      return Task.Run(() => TranslateInternal(jpStr));
    }

    public async Task<bool> IsHdorEnabled() {
      string? chk = await Translate("蜜ドル辞典").ConfigureAwait(false);
      return chk?.Contains("OK") ?? false;
    }

    // 원래 FreeLibrary를 호출하려 했는데 그러면 Access violation이 뜬다.

    private string TranslateInternal(string jpStr) {
      var escaper = new EztransEscaper();
      string escaped = escaper.Escape(jpStr);
      IntPtr p = J2kMmntw(0, escaped);
      if (p == IntPtr.Zero) {
        throw new EhndException("이지트랜스에서 알 수 없는 오류가 발생했습니다");
      }
      string? ret = Marshal.PtrToStringAuto(p);
      J2kFree(p);
      return escaper.Unescape(ret ?? "");
    }

    #region PInvoke
    delegate bool J2K_InitializeEx(
      [MarshalAs(UnmanagedType.LPStr)] string user,
      [MarshalAs(UnmanagedType.LPStr)] string key);
    delegate IntPtr J2K_TranslateMMNTW(int data0, [MarshalAs(UnmanagedType.LPWStr)] string jpStr);
    delegate void J2K_FreeMem(IntPtr ptr);
    #endregion
  }
}
