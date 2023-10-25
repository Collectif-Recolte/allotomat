using System;

namespace Sig.App.Backend.Plugins.MediatR
{
    public abstract class RequestValidationException : Exception
    {
        protected RequestValidationException() { }
        protected RequestValidationException(string message) : base(message) { }
    }
}