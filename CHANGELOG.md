## 2.3.0
- Add `details` field containing search path information to EztransNotFoundException.

## 2.2.0
- BatchEhnd doesn't ignore exception when underlying translator throws.

## 2.1.0
- Add TranslateAsync() which is thread safe.
- Add BatchEhnd, which aggregates strings and send it at once (for performance).

# 2.0.0
- Remove Ehnd.Create. Use constructor instead.
- Change Translate, IsHdorEnabled methods to synchronous.