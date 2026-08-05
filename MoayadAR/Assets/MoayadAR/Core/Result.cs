using System;

namespace MoayadAR.Core
{
    /// <summary>Structured result: machine-readable error code + localization key, never a swallowed exception.</summary>
    public readonly struct Result<T>
    {
        public readonly bool Ok;
        public readonly T Value;
        public readonly string ErrorCode;
        public readonly string MessageKey;
        public readonly string Detail;
        private Result(bool ok, T v, string code, string key, string detail)
        { Ok = ok; Value = v; ErrorCode = code; MessageKey = key; Detail = detail; }
        public static Result<T> Success(T v) => new Result<T>(true, v, null, null, null);
        public static Result<T> Fail(string code, string messageKey, string detail = null)
            => new Result<T>(false, default, code, messageKey, detail);
    }
}
