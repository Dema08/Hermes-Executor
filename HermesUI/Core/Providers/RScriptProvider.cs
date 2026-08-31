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
            BaseAddress = new Uri("https://api.rscripts.net/"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        );

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey
                );
        }

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
        string endpointPath = string.IsNullOrWhiteSpace(query)
            ? "v1/scripts?limit=20&page=1"
            : "v1/search" +
              $"?q={Uri.EscapeDataString(query)}" +
              "&index=scripts" +
              "&limit=20" +
              "&page=1" +
              "&includeScript=true";

        HttpResponseMessage? response = null;
        string json = string.Empty;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            response = await _httpClient.GetAsync(endpointPath, cts.Token);
            json = await response.Content.ReadAsStringAsync(cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var scripts = ParseScriptsJson(json);
                return scripts;
            }
        }
        catch { }

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

                    Author = GetAuthorName(item),

                    Game = GetGameName(item),

                    Script = GetString(
                        item,
                        "script",
                        "source",
                        "code",
                        "lua"
                    ),

                    ThumbnailUrl = GetThumbnailUrl(item),

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

                if (string.IsNullOrWhiteSpace(script.Script))
                {
                    string rawScriptUrl = GetString(item, "rawScript", "url", "sourceUrl");
                    if (!string.IsNullOrWhiteSpace(rawScriptUrl) && rawScriptUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        script.Script = $"loadstring(game:HttpGet(\"{rawScriptUrl}\"))()";
                    }
                }

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
            ?? root.SelectToken("results")
            ?? root.SelectToken("data");

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


    private static string? GetThumbnailUrl(JToken item)
    {
        string value = GetString(item, "imageUrl", "thumbnail", "thumbnailUrl", "image");
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        JToken? game = FindProperty(item, "game");
        if (game != null)
        {
            value = GetString(game, "thumbnailUrl", "logoUrl", "image");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
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

    private static string GetAuthorName(JToken token)
    {
        if (token is not JObject obj) return "Unknown";

        JToken? creator = FindProperty(token, "creator") ?? FindProperty(token, "author");
        if (creator != null)
        {
            if (creator.Type == JTokenType.String)
            {
                string str = creator.ToString();
                if (!string.IsNullOrWhiteSpace(str) && !str.StartsWith("{"))
                    return str;
            }
            if (creator.Type == JTokenType.Object)
            {
                string? uname = creator["username"]?.ToString()
                             ?? creator["name"]?.ToString()
                             ?? creator["title"]?.ToString();
                if (!string.IsNullOrWhiteSpace(uname))
                    return uname;
            }
        }

        string fallbackUser = GetString(token, "username", "authorName");
        return !string.IsNullOrWhiteSpace(fallbackUser) ? fallbackUser : "Unknown";
    }
}