using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace budget
{
    class DebugTimer : IDisposable
    {
        public DebugTimer(string aMessage)
        {
            mMessage = aMessage;
            mStopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine($"{System.DateTime.Now.ToLongTimeString()}: took {mStopwatch.ElapsedMilliseconds} - {mMessage}");
        }

        private string mMessage;
        private System.Diagnostics.Stopwatch mStopwatch;
    }
}
