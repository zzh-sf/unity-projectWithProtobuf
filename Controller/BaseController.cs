using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketGameProtool;

namespace ConsoleApp3.Controller
{
    abstract class BaseController
    {
        protected RequestCode requestCode = RequestCode.RequestNone;
        public RequestCode _requestCode{get { return requestCode; } }
    }
}
