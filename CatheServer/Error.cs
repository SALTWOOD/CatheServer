using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatheServer
{
    public class ClientRequestInvalidException : Exception
    {
        public ClientRequestInvalidException() : base()
        {

        }

        public ClientRequestInvalidException(string? message) : base(message)
        {

        }
    }

    public class InternalServerException : Exception
    {
        public InternalServerException() : base()
        {

        }

        public InternalServerException(string? message) : base(message)
        {

        }
    }
}
