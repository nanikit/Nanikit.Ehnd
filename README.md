# Nanikit.Ehnd

Personal C# binding for [Ehnd](https://github.com/sokcuri/ehnd).

It also does some pre/postprocessing for EzTransXp artifact.

```
var batchEhnd = new BatchEhnd(new Ehnd());
return batchEhnd.TranslateAsync(source);
```