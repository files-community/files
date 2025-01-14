// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Utils
{
    /// <summary>
    /// Allows an object to be initialized asynchronously.
    /// </summary>
    public interface IAsyncInitialize
    {
        /// <summary>
        /// Initializes resources and prepares them for use.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task InitAsync(CancellationToken cancellationToken = default);
    }
}
