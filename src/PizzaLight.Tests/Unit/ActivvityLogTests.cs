using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Moq;
using NUnit.Framework;
using PizzaLight.Infrastructure;
using Serilog;

namespace PizzaLight.Tests.Unit
{
    [TestFixture]

    class ActivityLogTests
    {
        [Test]
        public void ActivityLogWillThrottleIfHyperActive()
        {
            var log = new ActivityLog(new Mock<ILogger>().Object);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 12; i++)
            {
                log.Log(i.ToString());
            }
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds>1000);
            Assert.That(log.Activities.Count>10);
        }

        [Test]
        public void ActivityLogIsQuickIfOnly10Events()
        {
            var log = new ActivityLog(new Mock<ILogger>().Object);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                log.Log(i.ToString());
            }
            sw.Stop();
            Assert.LessOrEqual(sw.ElapsedMilliseconds,100);
            Assert.That(log.Activities.Count==10);
        }
    }
}
