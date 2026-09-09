# Phase 1 acceptance checklist

Where a criterion is machine verifiable it is covered by a unit test (name shown); the remainder is
verified by running the app on Windows.

## Edge dock

- [x] Dock can be positioned on the left — `DockEdge`, `DockMetricsTests`, `SettingsStoreTests`
- [x] Dock can be positioned on the right — same
- [x] Dock is vertical — `DockMetricsTests.Compute_*` (column layout)
- [x] Auto reveal when the cursor reaches the edge — `DockVisibilityControllerTests.Cursor_at_the_edge_reveals_the_dock`
- [x] Auto hide after leaving — `DockVisibilityControllerTests.Leaving_starts_the_hide_delay_and_only_then_hides`
- [x] Smooth reveal/hide — `DockAnimatorTests`, `DockPosition` slide math
- [x] Does not steal focus — `WS_EX_NOACTIVATE` + `ShowActivated=false` (runtime)

## Pinned items

- [x] Pin application / file / folder — `PinnedItemTests.Factory_maps_paths_to_the_right_kind`
- [x] Remove pinned item — `ItemStoreTests.Remove_*`
- [x] Reorder — `ItemStoreTests.Move_*` (+ drag & drop with insertion indicator)
- [x] Persist across restarts — `JsonRepositoryTests.Save_then_load_round_trips_*`
- [x] Missing files/folders handled safely — `PinnedItemTests.Missing_*`, `JsonRepositoryTests.A_corrupt_file_*`

## Running apps

- [x] Running applications detected — `RunningAppIndexTests`
- [x] Running state visually indicated — `DockViewModelTests.Running_and_active_state_*` + XAML indicator
- [x] Click running app brings it forward — `WindowManager.Activate` (runtime) + `WindowActivator`
- [x] Click non-running launches it — `PinnedItemTests.Application_is_available_and_launches_its_target`
- [x] Minimized windows restored — `WindowActivator` (SW_RESTORE) + `RunningAppIndexTests.Minimized_*`
- [x] Active app identifiable — `DockViewModelTests` + `ForegroundWatcher`

## Settings

- [x] Left/right change — `SettingsStoreTests`
- [x] Size / appearance — `DockSettings`, `DockLook`
- [x] Startup behaviour — `StartupRegistration`

## Reliability

- [x] Long-running stability, low idle CPU — cursor poll only when hidden; window enumeration only when
      visible; event-driven foreground (runtime)
- [x] Restart preserves config + pins — persistence tests
- [x] No interference with normal Windows use — invisible when hidden, no focus steal (runtime)
