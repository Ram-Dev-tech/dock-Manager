using System.Collections.ObjectModel;
using DockManager.Core.Dock;
using DockManager.Core.Items;
using DockManager.Core.Shell;
using DockManager.Core.Windows;

namespace DockManager.Core.Ui;

/// <summary>
/// Aggregates the pinned items, their availability and the running application index into the two
/// lists the dock renders plus the resulting layout. The WPF layer subscribes to
/// <see cref="ContentChanged"/> and rebinds; per item state changes flow through
/// <see cref="DockItemViewModel"/> property notifications.
/// </summary>
public sealed class DockViewModel
{
    private readonly ItemStore _store;
    private readonly IFileSystemProbe _fileSystem;
    private readonly Dictionary<string, DockItemViewModel> _byId = new(StringComparer.Ordinal);
    private readonly ObservableCollection<DockItemViewModel> _applications = [];
    private readonly ObservableCollection<DockItemViewModel> _files = [];

    private DockLayoutMetrics _metrics;
    private RunningAppIndex _running = RunningAppIndex.Empty;
    private IntPtr _activeWindow;

    public DockViewModel(ItemStore store, IFileSystemProbe fileSystem, DockLayoutMetrics metrics)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _metrics = metrics;
        _store.Changed += OnStoreChanged;
        Rebuild();
    }

    /// <summary>Raised when the set or the order of items changed, or when the metrics changed.</summary>
    public event EventHandler? ContentChanged;

    public IReadOnlyList<DockItemViewModel> Applications => _applications;

    public IReadOnlyList<DockItemViewModel> Files => _files;

    public DockPlan Plan { get; private set; } = DockMetrics.Compute(0, 0, DockLayoutMetrics.ForIconSize(32));

    public DockLayoutMetrics Metrics => _metrics;

    public ItemStore Store => _store;

    public void ApplyMetrics(DockLayoutMetrics metrics)
    {
        if (_metrics == metrics)
        {
            return;
        }

        _metrics = metrics;
        RecomputePlan();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Updates running/active indicators for every item.</summary>
    public void RefreshRunning(RunningAppIndex index, IntPtr activeWindowHandle)
    {
        _running = index ?? RunningAppIndex.Empty;
        _activeWindow = activeWindowHandle;

        var activeApp = _running.FindByWindowHandle(_activeWindow);
        ApplyState(_applications, activeApp, refreshAvailability: true);
        ApplyState(_files, activeApp, refreshAvailability: false);
    }

    /// <summary>Re-checks whether every pinned target still exists.</summary>
    public void RefreshAvailability()
    {
        var activeApp = _running.FindByWindowHandle(_activeWindow);
        ApplyState(_applications, activeApp, refreshAvailability: true);
        ApplyState(_files, activeApp, refreshAvailability: true);
    }

    public DockItemViewModel? GetFlattened(int flattenedIndex)
    {
        if (flattenedIndex < 0)
        {
            return null;
        }

        if (flattenedIndex < _applications.Count)
        {
            return _applications[flattenedIndex];
        }

        var fileIndex = flattenedIndex - _applications.Count;
        return fileIndex < _files.Count ? _files[fileIndex] : null;
    }

    public DockItemViewModel? Find(string? id)
        => id is not null && _byId.TryGetValue(id, out var viewModel) ? viewModel : null;

    public int Flatten(DockSection section, int indexInSection) => section switch
    {
        DockSection.Applications => indexInSection,
        DockSection.Files => _applications.Count + indexInSection,
        _ => -1,
    };

    /// <summary>Returns the flattened slot of an item, or -1 when it is not in the dock.</summary>
    public int FlattenedIndexOf(string? id)
    {
        var viewModel = Find(id);
        if (viewModel is null)
        {
            return -1;
        }

        if (viewModel.Section == DockSection.Applications)
        {
            return _applications.IndexOf(viewModel);
        }

        var index = _files.IndexOf(viewModel);
        return index < 0 ? -1 : _applications.Count + index;
    }

    private void OnStoreChanged(object? sender, EventArgs e) => Rebuild();

    private void Rebuild()
    {
        var nextApplications = new List<DockItemViewModel>();
        var nextFiles = new List<DockItemViewModel>();
        var live = new HashSet<string>(StringComparer.Ordinal);
        var activeApp = _running.FindByWindowHandle(_activeWindow);

        foreach (var item in _store.Items)
        {
            if (!_byId.TryGetValue(item.Id, out var viewModel))
            {
                viewModel = new DockItemViewModel(item);
                _byId[item.Id] = viewModel;
            }

            live.Add(item.Id);

            var availability = item.GetAvailability(_fileSystem);
            var running = ResolveRunning(item, activeApp);
            viewModel.Update(availability, running.State, running.WindowCount);

            if (item.Section == DockSection.Applications)
            {
                nextApplications.Add(viewModel);
            }
            else
            {
                nextFiles.Add(viewModel);
            }
        }

        foreach (var id in _byId.Keys.Where(key => !live.Contains(key)).ToList())
        {
            _byId.Remove(id);
        }

        Sync(_applications, nextApplications);
        Sync(_files, nextFiles);

        RecomputePlan();
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adjusts <paramref name="target"/> to match <paramref name="desired"/> with the minimum number
    /// of collection notifications, so WPF reuses item containers and icons instead of rebuilding.
    /// </summary>
    private static void Sync(ObservableCollection<DockItemViewModel> target, List<DockItemViewModel> desired)
    {
        for (var i = target.Count - 1; i >= 0; i--)
        {
            if (!desired.Contains(target[i]))
            {
                target.RemoveAt(i);
            }
        }

        for (var i = 0; i < desired.Count; i++)
        {
            var current = target.IndexOf(desired[i]);
            if (current < 0)
            {
                target.Insert(i, desired[i]);
            }
            else if (current != i)
            {
                target.Move(current, i);
            }
        }
    }

    private void ApplyState(IReadOnlyList<DockItemViewModel> items, RunningApp? activeApp, bool refreshAvailability)
    {
        foreach (var viewModel in items)
        {
            var availability = refreshAvailability
                ? viewModel.Item.GetAvailability(_fileSystem)
                : viewModel.Availability;

            var running = ResolveRunning(viewModel.Item, activeApp);
            viewModel.Update(availability, running.State, running.WindowCount);
        }
    }

    private (PinnedItemRunState State, int WindowCount) ResolveRunning(PinnedItem item, RunningApp? activeApp)
    {
        if (item is not AppItem)
        {
            return (PinnedItemRunState.NotRunning, 0);
        }

        var key = item.RunningMatchKey;
        if (key.Length == 0)
        {
            return (PinnedItemRunState.NotRunning, 0);
        }

        var app = _running.FindByExecutable(key);
        if (app is null)
        {
            return (PinnedItemRunState.NotRunning, 0);
        }

        var isActive = activeApp is not null && ReferenceEquals(activeApp, app);
        return (isActive ? PinnedItemRunState.Active : PinnedItemRunState.Running, app.WindowCount);
    }

    private void RecomputePlan()
        => Plan = DockMetrics.Compute(_applications.Count, _files.Count, _metrics);
}
