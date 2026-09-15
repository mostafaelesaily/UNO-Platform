using System;
using System.Collections.Generic;
using System.Text;

namespace UNO.Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
