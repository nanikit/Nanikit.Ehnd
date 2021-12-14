﻿#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nanikit.Ehnd {
  /// <summary>
  /// Ehnd C# binding internals
  /// </summary>
  internal static class EhndInterop {
    public static readonly string DllName = "J2KEngine.dll";

    /// <summary>
    /// Returns guessed eztrans installed directory.
    /// </summary>
    public static string? GetEztransDirFromReg() {
      RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
      return key.OpenSubKey(@"Software\ChangShin\ezTrans")?.GetValue("FilePath") as string;
    }

    public static IntPtr LoadDll(string? dllPath) {
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

    public static T GetFuncAddress<T>(IntPtr dll, string name) {
      IntPtr addr = NativeLibrary.GetExport(dll, name);
      if (addr == IntPtr.Zero) {
        throw new EhndException("Ehnd 파일이 아닙니다.");
      }
      return Marshal.GetDelegateForFunctionPointer<T>(addr);
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

    private static IEnumerable<string> GetDllSearchPaths(string? path) {
      var paths = new List<string>();

      if (path != null) {
        paths.Add(path);
      }

      string? regPath = GetEztransDirFromReg();
      if (regPath != null) {
        paths.Add($"{regPath}\\{DllName}");
      }

      string defPath = @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP";
      paths.Add($"{defPath}\\{DllName}");
      paths.AddRange(GetAssemblyParentDirectories().Select(x => Path.Combine(x, DllName)));

      return paths.Distinct();
    }

    // FreeLibrary를 호출하면 Access violation이 뜬다.

    #region PInvoke
    delegate bool J2K_InitializeEx(
      [MarshalAs(UnmanagedType.LPStr)] string user,
      [MarshalAs(UnmanagedType.LPStr)] string key);
    #endregion
  }
}