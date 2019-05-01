// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Extension methods for <see cref="Task"/> and <see cref="Task{TResult}"/> objects.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="Task"/> into a <see cref="MachineTask"/>.
        /// </summary>
        public static MachineTask ToMachineTask(this Task @this) => new MachineTask(@this);

        /// <summary>
        /// Converts the specified <see cref="Task{TResult}"/> into a <see cref="MachineTask{TResult}"/>.
        /// </summary>
        public static MachineTask<TResult> ToMachineTask<TResult>(this Task<TResult> @this) =>
            new MachineTask<TResult>(@this);
    }
}
