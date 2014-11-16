using System;
using System.Diagnostics;
using NUnit.Framework;

namespace DalSoft.Dynamic
{
    public class UnitTestStopWatch
    {
        private Stopwatch _stopwatch;

        [SetUp]
        public void SetUp()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _stopwatch.Stop();
            Console.WriteLine("Test took {0} milliseconds", _stopwatch.ElapsedMilliseconds);
        }
    }
}
