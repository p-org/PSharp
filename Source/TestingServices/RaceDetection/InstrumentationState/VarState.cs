//-----------------------------------------------------------------------
// <copyright file="VarState.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.RaceDetection.Util;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.RaceDetection.InstrumentationState
{
    internal class VarState
    {
        internal long ReadEpoch;

        internal long WriteEpoch;

        internal VectorClock VC;

        internal string lastWriteLocation;

        internal Dictionary<long, string> LastReadLocation;

        internal long InMonitorWrite;

        internal Dictionary<long, long> InMonitorRead;

        public VarState(bool isWrite, long epoch, bool shouldCreateInstrumentationState, long inMonitor)
        {
            if (isWrite)
            {
                ReadEpoch = Epoch.Zero;
                WriteEpoch = epoch;
                InMonitorWrite = inMonitor;
            }
            else
            {
                WriteEpoch = Epoch.Zero;
                ReadEpoch = epoch;
            }

            if (shouldCreateInstrumentationState)
            {
                LastReadLocation = new Dictionary<long, string>();
            }
            InMonitorRead = new Dictionary<long, long>();
        }
    }
}