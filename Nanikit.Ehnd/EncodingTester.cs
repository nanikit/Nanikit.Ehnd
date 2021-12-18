using System.Text;

namespace Nanikit.Ehnd {
  internal class EncodingTester {
    static EncodingTester() {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static Encoding GetSOHFallbackEncoding(int codepage) {
      EncoderFallback efall = new EncoderReplacementFallback("\x01");
      DecoderFallback dfall = new DecoderReplacementFallback("\x01");
      return Encoding.GetEncoding(codepage, efall, dfall);
    }

    public EncodingTester(int codepage) {
      _encoder = GetSOHFallbackEncoding(codepage).GetEncoder();
    }

    public bool IsEncodable(char ch) {
      _chars[0] = ch;
      _encoder.Convert(_chars, 0, 1, _bytes, 0, 8, false, out _, out _, out _);
      return _chars[0] != '\x01';
    }

    private readonly Encoder _encoder;
    private readonly char[] _chars = new char[1];
    private readonly byte[] _bytes = new byte[8];
  }
}
