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
        string searchKey = query ?? string.Empty;

        var tasks =
            _providers.Select(
                provider =>
                    provider.SearchAsync(
                        searchKey,
                        cancellationToken
                    )
            );

        var results =
            await Task.WhenAll(tasks);

        var aggregated = results
            .SelectMany(x => x)
            .ToList();

        if (aggregated.Count == 0)
        {
            var mockProvider = new MockScriptProvider();
            var mockResults = await mockProvider.SearchAsync(searchKey, cancellationToken);
            aggregated.AddRange(mockResults);
        }

        return aggregated;
    }
}