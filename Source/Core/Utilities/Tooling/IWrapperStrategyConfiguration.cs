// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// Configuration options for wrapper strategies
    /// </summary>
    public class WrapperStrategyConfiguration
    {
        /// <summary>
        /// Specifies the type of WrapperStrategy to use.
        /// </summary>
        public enum WrapperStrategy
        {
            /// <summary>
            /// MessageFlowBasedDHittingMetricStrategy
            /// </summary>
            InboxDHitting,

            /// <summary>
            /// InboxBasedDHittingMetricStrategy
            /// </summary>
            MessageFlowDHitting
        }

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

        /// <summary>
        /// The strategy type to be used
        /// </summary>
        internal WrapperStrategy StrategyType;

        /// <summary>
        /// The StepSignature type to be used
        /// </summary>
        internal DHittingSignature SignatureType;

        /// <summary>
        /// The Maximum d in d-tuple
        /// </summary>
        internal int MaxDTuplesDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="WrapperStrategyConfiguration"/> class.
        /// </summary>
        public WrapperStrategyConfiguration()
        {
        }

        /// <summary>
        /// Parses the commandline option
        /// </summary>
        /// <param name="commandLineOption">The commandline option received</param>
        /// <returns>True if succeeds</returns>
        public bool TryParse(string commandLineOption)
        {
            string[] cliFrags = commandLineOption.Split(new char[] { ':' });

            if (cliFrags.Length < 1)
            {
                throw new ArgumentException("Wrapper strategy usage:  /wrapperstrategy:<StrategyName>[:<StrategySpecificOptions>] ");
            }

            if (!Enum.TryParse(cliFrags[0], true, out this.StrategyType))
            {
                throw new ArgumentException("Invalid wrapper strategy: " + cliFrags[0]);
            }

            switch (this.StrategyType)
            {
                case WrapperStrategy.InboxDHitting:
                case WrapperStrategy.MessageFlowDHitting:
                    this.TryParseDHitting(cliFrags);
                    break;
                default:
                    throw new ArgumentException("Unsupported argument exception: " + cliFrags[0]);
            }

            return true;
        }

        private void TryParseDHitting(string[] cliFrags)
        {
            if (cliFrags.Length < 3)
            {
                throw new ArgumentException("DHitting metric strategies require atleast 2 arguments: /wrapperstrategy:<StrategyName>[:<StrategySpecificOptions>] ");
            }

            if (!Enum.TryParse(cliFrags[1], true, out this.SignatureType))
            {
                throw new ArgumentException("Invalid wrapper strategy: " + cliFrags[1]);
            }

            if (!int.TryParse(cliFrags[2], out this.MaxDTuplesDepth))
            {
                throw new ArgumentException("Invalid value for MaxDTuplesDepth: " + cliFrags[2]);
            }
        }

        /// <summary>
        /// Creates a WrapperStrategyConfiguration instance for a d-hitting metric strategy.
        /// </summary>
        /// <param name="strategyType">The specified strategy type</param>
        /// <param name="signatureType">The StepSignature type</param>
        /// <param name="maxDToHit">The maximum d of d-tuples to record</param>
        /// <returns>A WrapperStrategyConfiguration instance</returns>
        public static WrapperStrategyConfiguration CreateDHittingStrategy(WrapperStrategy strategyType, DHittingSignature signatureType, int maxDToHit)
        {
            WrapperStrategyConfiguration wsc = new WrapperStrategyConfiguration();

            if (strategyType != WrapperStrategy.InboxDHitting && strategyType != WrapperStrategy.MessageFlowDHitting)
            {
                throw new ArgumentException($"strategyType must be one of [{WrapperStrategy.InboxDHitting}, {WrapperStrategy.MessageFlowDHitting}]" );
            }

            wsc.StrategyType = strategyType;
            wsc.SignatureType = signatureType;
            wsc.MaxDTuplesDepth = maxDToHit;

            return wsc;
        }
    }
}
