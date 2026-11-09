using Xunit;

// ShiftBlazor keeps two process-wide static registries — IShortcutComponent (the keyboard
// shortcut stack) and ShiftBlazorEvents (the modal-closed bus). Every rendered ShiftFormBasic,
// ShiftList and dialog in the process shares them, so tests from different classes running at
// the same time see each other's components: a ShiftList from one class receives the modal-closed
// event of another, and "the top shortcut component" is whichever test registered last.
// Run the classes one after another so each test only ever sees its own components.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
