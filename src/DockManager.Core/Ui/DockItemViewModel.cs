using System.ComponentModel;
using System.Runtime.CompilerServices;
using DockManager.Core.Dock;
using DockManager.Core.Items;

namespace DockManager.Core.Ui;

/// <summary>
/// Bindable view of one pinned item. It lives in the core so the running/active/unavailable logic
/// is unit testable; the WPF layer only renders it and supplies the icon.
/// </summary>
public sealed class DockItemViewModel : INotifyPropertyChanged
{
    private ItemAvailability _availability = ItemAvailability.Available;
    private PinnedItemRunState _runState;
    private int _windowCount;
    private string? _tooltip;
    private bool _isSelected;

    public DockItemViewModel(PinnedItem item)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PinnedItem Item { get; }

    public string Id => Item.Id;

    public PinnedItemKind Kind => Item.Kind;

    public DockSection Section => Item.Section;

    public string Name => Item.EffectiveName;

    public string TargetPath => Item.TargetPath;

    /// <summary>Key the icon cache uses. For shortcuts this is the resolved executable.</summary>
    public string IconKey
        => Item is AppItem { ExecutablePath: { Length: > 0 } executable } ? executable : Item.TargetPath;

    public ItemAvailability Availability
    {
        get => _availability;
        private set => SetField(ref _availability, value);
    }

    public bool IsAvailable => _availability == ItemAvailability.Available;

    public bool IsMissing => _availability == ItemAvailability.Missing;

    public PinnedItemRunState RunState
    {
        get => _runState;
        private set => SetField(ref _runState, value);
    }

    public bool IsRunning => _runState is PinnedItemRunState.Running or PinnedItemRunState.Active;

    public bool IsActive => _runState == PinnedItemRunState.Active;

    public int WindowCount
    {
        get => _windowCount;
        private set => SetField(ref _windowCount, value);
    }

    /// <summary>Keyboard selection highlight, used by the global next/previous shortcuts.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>Short text used for the indicator and the tooltip.</summary>
    public string? Tooltip
    {
        get => _tooltip;
        private set => SetField(ref _tooltip, value);
    }

    /// <summary>Applies freshly computed state and raises only the properties that changed.</summary>
    public void Update(ItemAvailability availability, PinnedItemRunState runState, int windowCount)
    {
        var previousAvailability = _availability;
        var previousName = Name;

        Availability = availability;
        RunState = availability == ItemAvailability.Available ? runState : PinnedItemRunState.NotRunning;
        WindowCount = windowCount;
        Tooltip = BuildTooltip();

        if (previousName != Name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }

        if (previousAvailability != availability)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAvailable)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMissing)));
        }
    }

    public string BuildTooltip()
    {
        if (!Item.Kind.IsOpenable())
        {
            return string.Empty;
        }

        if (!IsAvailable)
        {
            return $"{Name} — unavailable. Right-click to remove.";
        }

        return Kind switch
        {
            PinnedItemKind.Application when IsActive => WindowCount > 1
                ? $"{Name} — active ({WindowCount} windows)"
                : $"{Name} — active",
            PinnedItemKind.Application when IsRunning => WindowCount > 1
                ? $"{Name} — running ({WindowCount} windows)"
                : $"{Name} — running",
            PinnedItemKind.Application => $"{Name}\n{TargetPath}",
            PinnedItemKind.Folder => $"{Name}\nOpens in File Explorer",
            _ => $"{Name}\n{TargetPath}",
        };
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (propertyName == nameof(RunState))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }
}
