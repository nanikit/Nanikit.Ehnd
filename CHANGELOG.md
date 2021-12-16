# 2.1.0
- Added TranslateAsync() which is thread safe.
- Added BatchEhnd, which aggregates strings and send it at once (for performance).

# 2.0.0
- Remove Ehnd.Create. Use constructor instead.
- Change Translate, IsHdorEnabled methods to synchronous.