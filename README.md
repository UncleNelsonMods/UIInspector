# UI Inspector Mod for Schedule I

A simple mod that helps you inspect UI elements in Schedule I. This tool is useful for developers and modders who need to understand the game's UI structure.

## Features

- Press the H key to take a UI snapshot
- Lists all active canvases in the game
- Shows detailed information about buttons, including text and paths
- Lists all important text elements
- Identifies interesting scripts (messages, phone, ATM, etc.)
- Provides helper methods for finding UI elements by path

## Installation

1. Make sure you have [MelonLoader](https://github.com/LavaGang/MelonLoader) installed for Schedule I
2. Download the latest `UIInspectorMod.dll` from the releases
3. Place the `UIInspectorMod.dll` file in your `<Schedule I>/Mods/` folder
4. (Optional) Create a `UIInspectorMod.json` file in your `<Schedule I>/Mods/` folder to customize the key binding

## Usage

1. Launch the game with the mod installed
2. While in the game, press the H key to take a UI snapshot
3. Check the MelonLoader console for detailed information about the UI elements

### Example Output

```
[UIInspectorMod] Taking UI snapshot...
[UIInspectorMod] Found 3 Canvas objects
[UIInspectorMod] Active Canvas: MainCanvas
[UIInspectorMod]   Buttons (5):
[UIInspectorMod]   - Menu/StartButton
[UIInspectorMod]       Text: "START GAME"
[UIInspectorMod]   - Menu/OptionsButton
[UIInspectorMod]       Text: "OPTIONS"
...
```

## Configuration

You can customize the key used for inspection by editing the `UIInspectorMod.json` file:

```json
{
  "InspectKey": "H"
}
```

Change "H" to the key you prefer (e.g., "F1", "Tab", etc.).

## For Developers

The mod provides a helpful method for finding specific UI elements:

```csharp
// Example of how to find a UI element by path
GameObject startButton = UIInspector.FindUIElement("MainCanvas", "Menu/StartButton");
```

## Building from Source

### Requirements
- .NET 6.0 SDK or later
- Visual Studio 2019/2022 or another compatible IDE

### Steps
1. Clone this repository
2. Open the solution in your IDE
3. Ensure the `GamePath` in `build.bat` points to your Schedule I installation
4. Run the `build.bat` script or build the solution from your IDE

## License

This project is open source. Feel free to modify and distribute as needed. 