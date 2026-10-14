# Project instructions

## UI authoring

- Design all UI in Unity scenes or editable Prefabs. Runtime scripts bind events, update data, and show or hide existing elements using serialized Inspector references.
- Do not create Canvas, Panels, Texts, Buttons, EventSystems, or their layout at runtime, including fallback paths. A missing required reference must produce a clear error naming that reference.
- Repeated UI elements, such as room rows and player statistics, may instantiate an authored Prefab and populate its data.
- Use the MainMenuScene LoadingCanvas instance and the GameScene MatchCanvas instance. Prevent duplicates, preserve loading UI across scene transitions on its own root, and clean up event/button subscriptions.
- Results must hide the local player's UI only, including late UI setup. Do not deactivate the player root or its network components to hide UI.
