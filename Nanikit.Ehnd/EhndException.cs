namespace Nanikit.Ehnd {
  using System;

  public class EhndException : Exception {
    public EhndException(string message) : base(message) { }
  }

  public class EhndNotFoundException : EhndException {
    public EhndNotFoundException(string message) : base($"Ehnd를 찾지 못했습니다{message}") { }
  }
}
