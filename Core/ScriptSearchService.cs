using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hermes_Executor.Core.Providers;
using Hermes_Executor.Models;

namespace Hermes_Executor.Core;

public class ScriptSearchService
{
    private readonly IEnumerable<IScriptProvider> _providers;

    public ScriptSearchService(
        IEnumerable<IScriptProvider> providers)
    {
        _providers = providers;
    }

    public async Task<List<ScriptItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<ScriptItem>();
        }

        var tasks =
            _providers.Select(
                provider =>
                    provider.SearchAsync(
                        query,
                        cancellationToken
                    )
            );

        var results =
            await Task.WhenAll(tasks);

        return results
            .SelectMany(x => x)
            .ToList();
    }
}