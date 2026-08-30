using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Hermes_Executor.Models;

using Newtonsoft.Json.Linq;

namespace Hermes_Executor.Core.Providers;

public class RScriptProvider : IScriptProvider
{
    private readonly HttpClient _httpClient;
    private readonly string[] _fallbackEndpoints = new[]
    {
        "https://api.rscript.org/v1/scripts",
        "https://raw.githubusercontent.com/rscript-org/scripts/main/index.json",
        "https://rscript-api.vercel.app/api/scripts"
    };

    public string Name => "RScript";

    public RScriptProvider(string apiKey)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.rscripts.net/")
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                apiKey
            );

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"
            )
        );
    }


    public async Task<IReadOnlyList<ScriptItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ScriptItem>();
        }

        string endpointPath =
            "v1/search" +
            $"?q={Uri.EscapeDataString(query)}" +
            "&index=scripts" +
            "&limit=20" +
            "&page=1" +
            "&includeScript=true";

        HttpResponseMessage? response = null;
        string json = string.Empty;

        // Try base address + endpoint path
        try
        {
            response = await _httpClient.GetAsync(endpointPath, cancellationToken);
            json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return ParseScriptsJson(json);
            }
        }
        catch { }

        // Try fallback endpoints
        foreach (var endpoint in _fallbackEndpoints)
        {
            try
            {
                var fullUrl = endpoint.EndsWith("/") ? endpoint + endpointPath : endpoint + "/" + endpointPath;
                using var fallbackClient = new HttpClient();
                fallbackClient.DefaultRequestHeaders.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
                fallbackClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                response = await fallbackClient.GetAsync(fullUrl, cancellationToken);
                json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return ParseScriptsJson(json);
                }
            }
            catch { }
        }

        // If all failed, try a direct fetch on fallback endpoints assuming they are direct index lists
        foreach (var endpoint in _fallbackEndpoints)
        {
            try
            {
                using var fallbackClient = new HttpClient();
                response = await fallbackClient.GetAsync(endpoint, cancellationToken);
                json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var scripts = ParseScriptsJson(json);
                    if (scripts.Count > 0)
                    {
                        return scripts;
                    }
                }
            }
            catch { }
        }

        return Array.Empty<ScriptItem>();
    }

    private IReadOnlyList<ScriptItem> ParseScriptsJson(string json)
    {
        try
        {
            JObject root = JObject.Parse(json);
            JArray? scripts = FindScriptArray(root);

            if (scripts == null)
            {
                return Array.Empty<ScriptItem>();
            }

            var result = new List<ScriptItem>();

            foreach (JToken item in scripts)
            {
                ScriptItem script = new ScriptItem
                {
                    Id = GetString(
                        item,
                        "id",
                        "_id",
                        "scriptId"
                    ),

                    Title = GetString(
                        item,
                        "title",
                        "name",
                        "scriptName"
                    ),

                    Description = GetString(
                        item,
                        "description",
                        "desc"
                    ),

                    Author = GetString(
                        item,
                        "author",
                        "creator",
                        "username"
                    ),

                    Game = GetGameName(item),

                    Script = GetString(
                        item,
                        "script",
                        "source",
                        "code",
                        "lua"
                    ),

                    ThumbnailUrl = GetStringOrNull(
                        item,
                        "thumbnail",
                        "thumbnailUrl",
                        "image"
                    ),

                    SourceUrl = GetStringOrNull(
                        item,
                        "url",
                        "sourceUrl",
                        "rawScript"
                    ),

                    Provider = "RScript",

                    Views = GetInt(
                        item,
                        "views",
                        "viewCount"
                    )
                };

                if (string.IsNullOrWhiteSpace(script.Title))
                {
                    script.Title = "Untitled Script";
                }

                result.Add(script);
            }

            return result;
        }
        catch
        {
            return Array.Empty<ScriptItem>();
        }
    }


    public async Task<ScriptItem?> GetScriptAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        // Untuk sekarang pencarian sudah cukup.
        // Detail endpoint kita sambungkan setelah search berhasil.
        await Task.CompletedTask;

        return null;
    }


    private static JArray? FindScriptArray(
        JObject root)
    {
        // Beberapa kemungkinan envelope JSON.
        // Dibuat fleksibel supaya tidak langsung pecah
        // jika API membungkus hasil di "data" atau "results".

        JToken? token =
            root.SelectToken("data.scripts")
            ?? root.SelectToken("results.scripts")
            ?? root.SelectToken("data.results")
            ?? root.SelectToken("scripts")
            ?? root.SelectToken("results");

        if (token is JArray array)
        {
            return array;
        }

        // Fallback: cari array pertama bernama scripts.
        JProperty? scriptsProperty =
            root.Descendants()
                .OfType<JProperty>()
                .FirstOrDefault(
                    p => p.Name.Equals(
                        "scripts",
                        StringComparison.OrdinalIgnoreCase
                    )
                    && p.Value is JArray
                );

        return scriptsProperty?.Value as JArray;
    }


    private static string GetString(
        JToken token,
        params string[] names)
    {
        foreach (string name in names)
        {
            JToken? value =
                FindProperty(token, name);

            if (value != null &&
                value.Type != JTokenType.Null)
            {
                return value.ToString();
            }
        }

        return "";
    }


    private static string? GetStringOrNull(
        JToken token,
        params string[] names)
    {
        string value =
            GetString(token, names);

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }


    private static int GetInt(
        JToken token,
        params string[] names)
    {
        foreach (string name in names)
        {
            JToken? value =
                FindProperty(token, name);

            if (value != null &&
                int.TryParse(
                    value.ToString(),
                    out int number))
            {
                return number;
            }
        }

        return 0;
    }


    private static string GetGameName(
        JToken token)
    {
        JToken? game =
            FindProperty(token, "game");

        if (game == null)
        {
            return "";
        }

        if (game.Type == JTokenType.String)
        {
            return game.ToString();
        }

        if (game.Type == JTokenType.Object)
        {
            return
                game["name"]?.ToString()
                ?? game["title"]?.ToString()
                ?? "";
        }

        return "";
    }


    private static JToken? FindProperty(
        JToken token,
        string name)
    {
        if (token is not JObject obj)
        {
            return null;
        }

        JProperty? property =
            obj.Properties()
                .FirstOrDefault(
                    p => p.Name.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

        return property?.Value;
    }
}