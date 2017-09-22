//-----------------------------------------------------------------------
// <copyright file="SyncWriterLogger.cs">
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

using System;
using System.IO;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Logger that wraps the TextWriter passed in a thread-safe wrapper
    /// </summary>
    internal sealed class SyncWriterLogger : StateMachineLogger
    {

        TextWriter writer;

        
        public SyncWriterLogger(TextWriter t)
        {            
            writer = t?? TextWriter.Null;
        }
        
        public override void Write(string value)
        {
            lock (writer)
            {
                writer.Write(value);
            }
        }

        
        public override void Write(string format, params object[] args)
        {
            lock (writer)
            {
                writer.Write(format, args);
            }
        }

        
        public override void WriteLine(string value)
        {
            lock (writer)
            {
                writer.WriteLine(value);
            }
        }

        
        public override void WriteLine(string format, params object[] args)
        {
            lock (writer)
            {
                writer.WriteLine(format, args);
            }
        }

        public override void Dispose()
        {
            
        }
    }
}
