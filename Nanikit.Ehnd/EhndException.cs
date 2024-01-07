namespace Nanikit.Ehnd {

  using System;

  public class EhndException : Exception {

    public EhndException(string message) : base(message) {
    }
  }

  public class EhndNotFoundException : EhndException {

    public EhndNotFoundException(string? details) : base($"Ehnd를 찾지 못했습니다") {
      Details = details;
    }

    public string? Details { get; set; }
  }
}
