# Repository reset context

This branch intentionally clears the repository contents to provide a clean
starting point while preserving implementation context for future work.

## Base

- Branch created from: `cursor/import-dialog-app-8ec8`
- Current branch: `cursor/clean-repo-keep-context-b2f1`

## What previously existed

The repository previously contained an import-dialog prototype (WPF-oriented)
with these areas:

- `View/ImportDialog.xaml`, `View/ImportDialog.xaml.cs`
- `ViewModel/ImportDialogViewModel.cs`
- `Models/LISCurveItem.cs`
- `Models/ParameterItem.cs` / `ParameterTable`
- legacy duplicate `ViewModels/ImportDialogViewModel.cs` (removed in later work)

## Key context from earlier iterations

1. **ParameterTable direction**
   - moved from fixed 2-column shape to dynamic per-table shape.
   - target data contract discussed and used:
     - `Name` (table name)
     - `Columns` (headers for this specific table)
     - `Rows` (row values, each row is a list of strings)

2. **UI binding approach for dynamic columns**
   - static `DataGrid` columns were not suitable for variable schemas.
   - dynamic columns were created at runtime based on `SelectedParameterTable.Columns`.
   - cells were bound by index to row arrays/lists (e.g. `[0]`, `[1]`, ...).

3. **Command/event wiring issue**
   - direct XAML `SelectionChanged="..."` caused handler mismatch issues in some builds.
   - robust workaround was runtime subscription in code-behind after `InitializeComponent`.

4. **Curve selection flow**
   - available curves -> selected curves via move commands.
   - selected curve edits were intended to be available on Done/confirm output.

## Suggested restart plan

1. Recreate project structure (or new architecture) as needed.
2. Reintroduce `ParameterTable` as immutable container receiving prebuilt
   collections from upstream parser/mapper.
3. Build parser layer separately (e.g. from `Block34Data` / `ListViewItem` data).
4. Reconnect dialog UI with runtime dynamic column generation.
5. Add focused tests:
   - parser to `ParameterTable`
   - move-curve flow and Done output
   - dynamic-column rendering behavior (where practical)

## Notes

- Working stash from previous branch state exists:
  `wip-before-repo-cleanup-branch`
- Use it only if you need to recover in-progress edits.
