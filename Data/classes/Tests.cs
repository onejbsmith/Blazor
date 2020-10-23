#define TRACE 
using BasicallyMe.RobinhoodNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RhAutoHSOTP.classes
{

    public class UnitTests
    {
        public static void TestLogin()
        {
            var rh = new RobinhoodClient();

            var tokenBefore = rh.AuthToken;
            RhLogins.authenticate(rh).Wait();

            Debug.Assert(tokenBefore != rh.AuthToken,"Rh Login");
        }
    }
}