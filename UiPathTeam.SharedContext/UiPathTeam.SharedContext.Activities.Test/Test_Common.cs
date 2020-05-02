using System;
using System.IO;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiPathTeam.SharedContext.Context;
using System.Threading;

namespace UiPathTeam.SharedContext.Activities.Test
{
    [TestClass]
    public class Test_Common
    {
        [TestMethod]
        public void GetProcessInfoTest()
        {
/*            var getProcessInfoActivity = new GetProcessInfoActivity
            {
            };

            var output = WorkflowInvoker.Invoke(getProcessInfoActivity);

            Assert.IsTrue(output["Value"].ToString() == "sss");
*       }
    }
}
