# Command System Sample Setup

To test the sample:

1. Create a new Scene in Unity.
2. Create a Plane at (0,0,0) with a Collider.
3. Create a Cube at (0,0.5,0) and assign it to the `Actor` field of the `CommandSampleUI`.
4. Create a Canvas with:
    - **UndoRedoUI** component (assign Undo/Redo buttons).
    - **CommandSampleUI** component.
    - Buttons to call `StartRecording`, `StopRecording`, and `PlayReplay`.
5. Ensure a `MainCamera` is present and tagged correctly.
6. Press Play!

## Keyboard Shortcuts
- **[R]**: Start Recording.
- **[S]**: Stop Recording.
- **[P]**: Play Replay (spawns a Ghost).
- **[Left Click]**: Move Actor.
- **[Ctrl+Z] / [Ctrl+Y]**: Undo/Redo (if UndoRedoUI is setup).

## Expected Behaviour
- **Left Click**: Moves the cube to the clicked position (adds to Undo history).
- **Undo/Redo**: Moves the cube back and forth.
- **Recording**: Record a sequence of moves, then stop.
- **Playback**: Watch the cube re-execute those moves with the same timing.
