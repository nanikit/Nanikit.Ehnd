#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nanikit.Ehnd {
  public class EhndException : Exception {
    public EhndException(string message) : base(message) { }
  }

  public class EhndNotFoundException : EhndException {
    public EhndNotFoundException(string message) : base($"Ehnd를 찾지 못했습니다{message}") { }
  }

  /// <summary>
  /// Ehnd C# binding
  /// </summary>
  public class Ehnd {
    private static readonly string _dllName = "J2KEngine.dll";

    private readonly J2K_FreeMem _j2kFree;
    private readonly J2K_TranslateMMNTW _j2kMmntw;

    public Ehnd(string? dllPath = null) : this(LoadDll(dllPath)) { }

    private Ehnd(IntPtr eztransDll) {
      _j2kMmntw = GetFuncAddress<J2K_TranslateMMNTW>(eztransDll, "J2K_TranslateMMNTW");
      _j2kFree = GetFuncAddress<J2K_FreeMem>(eztransDll, "J2K_FreeMem");
    }

    /// <summary>
    /// Translates japanese to korean. Not thread safe.
    /// </summary>
    public string Translate(string japanese) {
      return TranslateInternal(japanese);
    }

    /// <summary>
    /// Returns true if Hdor dictionary is installed. Not thread safe.
    /// </summary>
    public bool IsHdorEnabled() {
      string? chk = Translate("蜜ドル辞典");
      return chk?.Contains("OK") ?? false;
    }

    /// <summary>
    /// Returns guessed eztrans installed directory.
    /// </summary>
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

    private static IntPtr LoadNativeDll(string path) {
      IntPtr dll = NativeLibrary.Load(path);
      if (dll == IntPtr.Zero) {
        int errorCode = Marshal.GetLastWin32Error();
        throw new EhndException($"라이브러리 로드 실패(에러 코드: {errorCode})");
      }

      string key = Path.Combine(Path.GetDirectoryName(path)!, "Dat");
      var initEx = GetFuncAddress<J2K_InitializeEx>(dll, "J2K_InitializeEx");
      if (!initEx("CSUSER123455", key)) {
        throw new EhndException("엔진 초기화에 실패했습니다.");
      }

      return dll;
    }

    private static T GetFuncAddress<T>(IntPtr dll, string name) {
      IntPtr addr = NativeLibrary.GetExport(dll, name);
      if (addr == IntPtr.Zero) {
        throw new EhndException("Ehnd 파일이 아닙니다.");
      }
      return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }

    private static IntPtr LoadDll(string? dllPath) {
      var exceptions = new Dictionary<string, Exception>();
      foreach (string path in GetDllSearchPaths(dllPath)) {
        if (!File.Exists(path)) {
          continue;
        }
        try {
          return LoadNativeDll(path);
        }
        catch (Exception e) {
          exceptions.Add(path, e);
        }
      }

      string detail = string.Join("", exceptions.Select(x => $"\n  {x.Key}: {x.Value.Message}"));
      throw new EhndNotFoundException(detail);
    }

    private static IEnumerable<string> GetDllSearchPaths(string? path) {
      var paths = new List<string>();

      if (path != null) {
        paths.Add(path);
      }

      string? regPath = GetEztransDirFromReg();
      if (regPath != null) {
        paths.Add($"{regPath}\\{_dllName}");
      }

      string defPath = @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP";
      paths.Add($"{defPath}\\{_dllName}");
      paths.AddRange(GetAssemblyParentDirectories().Select(x => Path.Combine(x, _dllName)));

      return paths.Distinct();
    }

    private string TranslateInternal(string jpStr) {
      var escaper = new EztransEscaper();
      string escaped = escaper.Escape(jpStr);
      IntPtr p = _j2kMmntw(0, escaped);
      if (p == IntPtr.Zero) {
        throw new EhndException("이지트랜스에서 알 수 없는 오류가 발생했습니다");
      }
      string? ret = Marshal.PtrToStringAuto(p);
      _j2kFree(p);
      return escaper.Unescape(ret ?? "");
    }

    // FreeLibrary를 호출하면 Access violation이 뜬다.

    #region PInvoke
    delegate bool J2K_InitializeEx(
      [MarshalAs(UnmanagedType.LPStr)] string user,
      [MarshalAs(UnmanagedType.LPStr)] string key);
    delegate IntPtr J2K_TranslateMMNTW(int data0, [MarshalAs(UnmanagedType.LPWStr)] string jpStr);
    delegate void J2K_FreeMem(IntPtr ptr);
    #endregion
  }
}
