# Desktop Aim Trainer - FPS Aim Trainer

A Windows desktop floating aim training tool designed for FPS players, helping you maintain your skills even during work hours!

## 🎯 Background

As an FPS gaming enthusiast, have you ever encountered these dilemmas:
- Want to practice during work breaks, but opening games is too obvious?
- Want to maintain aiming precision during lunch breaks, but don't want colleagues to notice?
- Need a low-key training tool that improves skills without affecting work?

**Desktop Aim Trainer** is here! It cleverly disguises targets as desktop icons, allowing you to train in a real desktop environment while maintaining your skills without being detected. A true "stealth training" tool!

## ✨ Features

### Core Features
- ✅ **Two Training Modes**: Count Mode and Time Mode
- ✅ **Real Desktop Environment**: Targets disguised as desktop icons (recycle bin, folders, documents, etc.), training happens on the real desktop
- ✅ **Click-through Windows**: Doesn't intercept mouse operations, doesn't affect normal work
- ✅ **System Icon Support**: Automatically retrieves Windows system default icons, no manual resource files needed
- ✅ **Smart Target Generation**: Core zone + peripheral zone weighted distribution, simulating real aiming scenarios
- ✅ **Multi-language Support**: Supports Simplified Chinese and English, interface can be switched anytime

### Convenience Features
- ✅ **Global Hotkey Support**: ESC/F6/F7 quick operations without switching windows
- ✅ **System Tray Resident**: Minimizes to tray, runs discreetly
- ✅ **One-Click Quick Start**: F7 uses last configuration to start training quickly
- ✅ **Training Statistics**: Real-time recording of hit rate, time, and other data

### Training Mode Details

#### 📊 Count Mode
- Set target hit count
- Must hit current target before next one appears
- **Statistics**: Hits, Total Time, Average Time per Target

#### ⏱️ Time Mode
- Set total training duration and individual target stay time
- Timeout without hit counts as Miss
- **Statistics**: Hits, Misses, Hit Rate, Hits/Minute

## 🛠️ Tech Stack

- **.NET 8** + **WPF** - Modern desktop application framework
- **Win32 API (P/Invoke)** - Global hotkeys, Click-through windows, system icon retrieval
- **Windows Forms** - System tray icon support
- **Resource Localization** - Multi-language switching support

## ⌨️ Hotkeys

| Hotkey | Function | Description |
|--------|----------|-------------|
| **ESC** | Stop Training | Immediately stop current training, clear all targets |
| **F7** | Quick Start | Start training with last configuration directly (no main window) |
| **F6** | View Results | Open main window to view last training statistics |

> 💡 **Tip**: After training starts, the window automatically minimizes to system tray. Use hotkeys for quick operations without opening the main window.

## 🎮 Usage Scenarios

### Work Stealth Scenarios
1. **Lunch Break**: Quick 5-minute training to maintain skills
2. **Work Gaps**: Use fragmented time for aiming practice
3. **Meeting Wait**: Brief training while waiting for meetings to start

### Training Recommendations
- **Beginner**: Use Count Mode, set 10-20 targets, focus on basic aiming improvement
- **Intermediate**: Use Time Mode, set 60 seconds, target stay time 1.5-2 seconds, improve reaction speed
- **Advanced**: Reduce target stay time to under 1 second, challenge extreme reactions

## 🚀 Quick Start

### Requirements

#### Operating System Support
- ✅ **Windows 11** - Fully supported (Recommended)
- ✅ **Windows 10** - Fully supported
- ⚠️ **Windows 7 SP1+** - Theoretically supported but not recommended (requires .NET 8 Runtime, and Windows 7 has ended official support)

#### Runtime Requirements
- **.NET 8 Runtime** (if only running the program)
- **.NET 8 SDK** (if compiling from source)

> 💡 **Note**: The APIs used by the program (such as `SHGetStockIconInfo`, `SHGetFileInfo`, etc.) are available on Windows 7+, so theoretically it can run on Windows 7 SP1+. However, since Windows 7 has ended official support and .NET 8 officially recommends Windows 10+, it's recommended to use Windows 10 or Windows 11.

### Running Methods

#### Method 1: Direct Run (Recommended)
1. Download `DesktopAimTrainer.exe`
2. Double-click to run

#### Method 2: Compile from Source
```bash
# Clone or download the project
git clone <repository-url>
cd DesktopAimTrainer

# Build the project
dotnet build

# Run the project
dotnet run
```

### First Time Use
1. After starting the program, select language (Simplified Chinese/English) in the top-right corner of the main window
2. Configure training parameters (icon type, training mode, target count/duration)
3. Click "Start Training", window will automatically minimize to system tray
4. Move mouse over target icons on desktop to hit
5. Press **F6** after training ends to view statistics

## 🌐 Multi-language Support

The program supports Simplified Chinese and English. You can switch languages anytime using the language selector in the top-right corner of the main window. All interface text updates immediately after switching.

- **简体中文**: Default language, suitable for Chinese users
- **English**: English interface, suitable for international users

> 💡 **Tip**: Language settings persist during the current session. After restarting the program, it will restore to system default language.

## 🎨 Icon System

The program supports three icon loading methods (in priority order):

1. **Resource Files**: Priority loading PNG icon files from `Resources` folder
2. **System Icons**: Automatically retrieve system default icons via Windows API (Recommended)
3. **Placeholders**: If both above fail, use colored squares as placeholders

### Supported Icon Types
- 🗑️ **Recycle Bin** - System recycle bin icon
- 📁 **New Folder** - System folder icon
- 📊 **Excel Document** - Excel file icon
- 📝 **Word Document** - Word file icon
- 📄 **Text Document** - Text file icon

> 💡 **Tip**: The program automatically retrieves system icons, no need to manually add resource files. If you want custom icons, place PNG files in the `Resources` folder.

## ⚙️ Technical Details

### Target Generation Rules
- **Vertical Position**: Only generated in screen vertical center 30-40% area
- **Horizontal Distribution**:
  - Core Zone (25%-75%): 70% weight, main training area
  - Peripheral Zone (0-25% and 75-100%): 30% weight, supplementary training
- **Distance Constraints**:
  - Maintain minimum 100 pixels distance from previous target
  - Maintain minimum 80 pixels distance from current mouse position

### Hit Detection
- Mouse pointer entering target rectangle area counts as hit
- No mouse click needed, just move mouse over target
- Click-through design, doesn't intercept underlying application operations

## 💻 System Compatibility

### Supported Windows Versions

| Windows Version | Support Status | Notes |
|----------------|----------------|-------|
| **Windows 11** | ✅ Fully Supported | Recommended, all features work |
| **Windows 10** | ✅ Fully Supported | All features work, recommended |
| **Windows 7 SP1+** | ⚠️ Theoretical Support | Requires .NET 8 Runtime, but Windows 7 has ended official support, not recommended |

### API Compatibility

The Windows APIs used by the program are all common APIs with compatibility as follows:
- `SHGetStockIconInfo` - Windows Vista+ (Windows 7+ supported)
- `SHGetFileInfo` - Windows 95+ (all versions supported)
- `SetWindowsHookEx` - Windows 95+ (all versions supported)
- Other Win32 APIs - Common APIs, all supported on Windows 7+

### .NET 8 Compatibility

- .NET 8 officially supports Windows 7 SP1+, but **recommends Windows 10+**
- Windows 7 has ended official support, recommend upgrading to Windows 10 or Windows 11

## ⚠️ Notes

- **Global Hotkeys**: May require administrator privileges in some cases to use global hotkeys
- **Multi-monitor**: Current version only supports single monitor (future versions will support multi-monitor)
- **Desktop Icons**: Program doesn't recognize real desktop icons, may overlap with real icons (this is a design feature)
- **Resource Cleanup**: Program automatically cleans up all floating windows and system resources on exit

## 🔒 Privacy & Security

- ✅ Doesn't operate real desktop files
- ✅ Doesn't modify Explorer state
- ✅ Doesn't intercept mouse clicks
- ✅ Keyboard Hook only used for necessary hotkeys
- ✅ All data only stored in memory, no history recorded

## Project Structure

```
DesktopAimTrainer/
├── App.xaml / App.xaml.cs          # Application entry
├── MainWindow.xaml / MainWindow.xaml.cs  # Main window
├── Win32Api.cs                      # Win32 API wrapper
├── GlobalHotkeyManager.cs           # Global hotkey management
├── TargetWindow.cs                  # Target window
├── TargetGenerator.cs               # Target position generator
├── TrainingSession.cs               # Training session management
├── TrainingMode.cs                  # Training modes and configuration
├── TrayIconManager.cs               # Tray icon management
├── LocalizationManager.cs           # Localization manager
├── LocalizationConverter.cs          # Localization converter
└── Resources/                       # Resource folder
    ├── Strings.resx                 # Chinese resources
    └── Strings.en-US.resx           # English resources
```

## 📋 Development Roadmap

### ✅ Completed (v1.0 MVP)
- [x] Single target training system
- [x] Two training modes (Count/Time)
- [x] Automatic system icon retrieval
- [x] Global hotkey support
- [x] System tray resident
- [x] Training statistics
- [x] Multi-language support (Simplified Chinese/English)

### 🚧 Planned
- [ ] Sound localization system (sound-based training)
- [ ] Training history records
- [ ] Custom target sizes
- [ ] More icon type options

## 🤝 Contributing

Welcome to submit Issues and Pull Requests!

## 📄 License

This project is for learning and personal use only.

---

**Made with ❤️ for FPS players who want to stay sharp during work hours**

