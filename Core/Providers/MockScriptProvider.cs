using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hermes_Executor.Models;

namespace Hermes_Executor.Core.Providers;

public class MockScriptProvider : IScriptProvider
{
    public string Name => "Demo Provider";

    private readonly List<ScriptItem> _scripts = new()
    {
        new ScriptItem
        {
            Id = "1",
            Title = "Blox Fruits Demo",
            Description = "Contoh script untuk pengujian Script Hub.",
            Provider = "Demo Provider",
            Author = "Hermes",
            Game = "Blox Fruits",
            Script = "-- Demo only\nprint(\"Blox Fruits Demo\")",
            Views = 1250
        },

        new ScriptItem
        {
            Id = "2",
            Title = "Brookhaven Demo",
            Description = "Contoh hasil pencarian Brookhaven.",
            Provider = "Demo Provider",
            Author = "Hermes",
            Game = "Brookhaven",
            Script = "-- Demo only\nprint(\"Brookhaven Demo\")",
            Views = 850
        },

        new ScriptItem
        {
            Id = "3",
            Title = "Universal Utility",
            Description = "Script contoh universal.",
            Provider = "Demo Provider",
            Author = "Hermes",
            Game = "Universal",
            Script = "-- Demo only\nprint(\"Universal Demo\")",
            Views = 420
        }
    };

    public Task<IReadOnlyList<ScriptItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ScriptItem> result = _scripts
            .Where(script =>
                script.Title.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase)
                ||
                script.Game.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(result);
    }

    public Task<ScriptItem?> GetScriptAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ScriptItem? script =
            _scripts.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(script);
    }
}