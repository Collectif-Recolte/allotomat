using System;

namespace Sig.App.Backend.Services.Crypto
{
    public interface ISignatureService
    {
        string SignUrl(string url, TimeSpan validFor);
        bool VerifySignedUrl(string url);
    }
}