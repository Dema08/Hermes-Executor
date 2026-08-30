using System;

namespace Hermes_Executor.Models;

public class ScriptItem
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Provider { get; set; } = "";

    public string Author { get; set; } = "";

    public string Game { get; set; } = "";

    public string Script { get; set; } = "";

    public string? ThumbnailUrl { get; set; }

    public string? SourceUrl { get; set; }

    public int Views { get; set; }

    public DateTime? UpdatedAt { get; set; }
}