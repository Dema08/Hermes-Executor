using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Hermes_Executor.Models;

public class ScriptItem : INotifyPropertyChanged
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

    private BitmapImage? _thumbnailImage;
    public BitmapImage? ThumbnailImage
    {
        get => _thumbnailImage;
        set
        {
            _thumbnailImage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}