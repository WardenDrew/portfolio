using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RadiusException : Exception
    {
        public RadiusException() : base() { }
        public RadiusException(string message) : base(message) { }
        public RadiusException(string message, Exception inner) : base(message, inner) { }
    }
}
