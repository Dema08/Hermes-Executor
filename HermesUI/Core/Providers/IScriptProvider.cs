using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hermes_Executor.Models;

namespace Hermes_Executor.Core.Providers;

public interface IScriptProvider
{
    string Name { get; }

    Task<IReadOnlyList<ScriptItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<ScriptItem?> GetScriptAsync(
        string id,
        CancellationToken cancellationToken = default);
}