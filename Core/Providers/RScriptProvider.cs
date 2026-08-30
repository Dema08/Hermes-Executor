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

        string url =
            "v1/search" +
            $"?q={Uri.EscapeDataString(query)}" +
            "&index=scripts" +
            "&limit=20" +
            "&page=1" +
            "&includeScript=true";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                url,
                cancellationToken
            );

        string json =
            await response.Content.ReadAsStringAsync(
                cancellationToken
            );

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"RScript API error {(int)response.StatusCode}: {json}"
            );
        }

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