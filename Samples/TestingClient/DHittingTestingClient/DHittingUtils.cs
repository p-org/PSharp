using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHittingTestingClient
{
    public static class DHittingUtils
    {
        // Some handy constants
        public const ulong TESTHARNESSMACHINEID = 0;
        public const ulong TESTHARNESSMACHINEHASH = 199999;

        /// <summary>
        /// Specifies the type of WrapperStrategy to use.
        /// </summary>
        public enum DHittingSignature
        {
            /// <summary>
            /// EventHashSignature
            /// </summary>
            EventHash,

            /// <summary>
            /// EventTypeIndexStepSignature
            /// </summary>
            EventTypeIndex,

            /// <summary>
            /// TreeHashStepSignature
            /// </summary>
            TreeHash
        }
    }
}
