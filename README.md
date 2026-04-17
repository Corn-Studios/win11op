# ⚡ Win11 Optimizer

A clean, open-source Windows 10/11 optimizer built in C# / WinForms. Designed to be dropped onto a fresh Windows install and run once to apply performance, privacy, gaming, network, and security tweaks — with full per-category undo support.

---

## Features

| Category | What it does |
|---|---|
| ⚡ Performance | High performance power plan, disables SysMain & Windows Search, NTFS optimizations, removes startup delay, best performance visual mode, sets timer resolution to 0.5ms via `timeBeginPeriod` |
| 🔒 Privacy & Telemetry | Disables all telemetry services and scheduled tasks, removes advertising ID, disables Bing/Cortana in Start, blocks activity feed and location tracking, disables Windows Recall (Copilot+ AI screenshot), removes Chat/Teams taskbar icon, blocks 35 Microsoft telemetry domains in the hosts file |
| 🖥 Responsiveness | Instant menus, faster shutdown timers, high-resolution system clock, disables Windows Tips and suggested content |
| 🎮 Gaming | Enables HAGS & Game Mode, disables mouse acceleration, boosts foreground CPU priority, disables Game DVR, disables fullscreen optimizations, sets GPU power policy to Prefer Maximum Performance, disables NVIDIA telemetry services |
| 🌐 Network | Disables Nagle's Algorithm, enables RSS, TCP auto-tuning, removes network throttling index, enables DNS over HTTPS via Cloudflare 1.1.1.1 |
| 🗑 Bloatware Removal | Removes pre-installed Microsoft and third-party bloat (Bing apps, Xbox overlays, TikTok, LinkedIn, etc.) from both user and provisioned packages |
| 🔐 Security Hardening | Disables AutoRun/AutoPlay on all drive types, disables Remote Desktop (RDP), disables SMBv1, disables NetBIOS over TCP/IP, enforces Windows Defender real-time protection |
| ⚠ Advanced Tweaks | CPU scheduler tuning (Win32PrioritySeparation), disable dynamic tick, disable CPU throttling for background processes, ensure SSD TRIM is enabled, aggressive animation disabling |

### Per-Category Undo
Every registry change is backed up before being applied. After running a category, an **↩ Undo Selected** button appears in the bottom bar — click it to fully restore that category to its pre-tweak state. Backups persist across app restarts via `tweaks_backup.json`.

---

## Tweak Details

### ⚡ Performance
- High Performance power plan via `powercfg`
- Disable Power Throttling (`PowerThrottlingOff`)
- Disable SysMain (Superfetch) and Windows Search Indexer
- Remove Explorer startup delay
- Visual effects set to Best Performance
- Disable NTFS last-access timestamps and 8.3 filenames
- Disable hibernation (frees several GB of disk space)
- Disable memory compression
- **Set timer resolution to 0.5ms** — calls `timeBeginPeriod(1)` via P/Invoke and sets `GlobalTimerResolutionRequests=1` in the kernel registry key for persistent sub-millisecond scheduler ticks

### 🔒 Privacy & Telemetry
- Set `AllowTelemetry=0` at machine and policy scope
- Disable DiagTrack, dmwappushservice, WerSvc, RetailDemo services
- Disable all CEIP, AppraiserV2, DiskDiagnostic, and Proxy scheduled tasks
- Disable Advertising ID, Bing in Start, Cortana consent, Activity Feed, location tracking, app camera access, SmartScreen, feedback requests, app launch tracking
- **Disable Windows Recall** — sets `DisableAIDataAnalysis=1` in both machine and user `WindowsAI` policy keys (no-op on non-Copilot+ hardware)
- **Disable Chat/Teams taskbar icon** — sets `TaskbarMn=0` in Explorer Advanced
- **Block telemetry hosts** — appends 35 Microsoft telemetry/data-collection domains to `C:\Windows\System32\drivers\etc\hosts`, fully reversible via undo

### 🖥 Responsiveness
- Menu show delay → 0ms
- WaitToKillAppTimeout / HungAppTimeout → 2000ms / 1000ms
- WaitToKillServiceTimeout → 2000ms
- AutoEndTasks on shutdown
- Platform tick (high-res timer) via `bcdedit`
- Disable Windows Tips and suggested content in Start

### 🎮 Gaming
- Enable Hardware-Accelerated GPU Scheduling (HAGS)
- Enable Game Mode and Auto Game Mode
- Disable mouse acceleration (pointer precision)
- CPU foreground priority boost (Win32PrioritySeparation=38)
- Disable Game DVR capture and FSO globally
- Disable fullscreen optimizations
- **GPU Power: Prefer Maximum Performance** — writes to the D3D GPU class driver registry key, prevents GPU idle downclocking
- **Disable NVIDIA Telemetry** — stops `NvTelemetryContainer`, `NvDisplayContainerLS`, and three NVIDIA scheduled tasks (no-op if NVIDIA drivers aren't installed)

### 🌐 Network
- Disable Nagle's Algorithm (`TcpAckFrequency`, `TCPNoDelay`) on all adapters
- Enable Receive-Side Scaling (RSS)
- TCP auto-tuning set to Normal
- Remove network throttling index and multimedia responsiveness cap
- **DNS over HTTPS (Cloudflare 1.1.1.1)** — enables `EnableAutoDoh=2` in the DNS Client and registers `1.1.1.1` / `1.0.0.1` with the Cloudflare DoH template

### 🗑 Bloatware Removal
Removes the following from both user packages and provisioned (all-user) packages:
Bing News/Weather/Search, Zune Video/Music, Skype, Solitaire, Feedback Hub, Maps, Phone Link, Clipchamp, Mixed Reality, Power Automate, LinkedIn, Disney+, Spotify, TikTok, Instagram, Facebook, Office Hub, OneNote, People, To Do, Widgets, Xbox apps and overlays, 3D Viewer, Print 3D, Wallet, and advertising apps.

> ⚠ Bloatware removal **cannot be undone** — removed apps must be reinstalled manually from the Microsoft Store if needed.

### 🔐 Security Hardening
- **Disable AutoRun/AutoPlay** — sets `NoDriveTypeAutoRun=0xFF` at both user and machine scope, blocks `autorun.inf` execution
- **Disable Remote Desktop (RDP)** — sets `fDenyTSConnections=1` and disables the RDP firewall rule group
- **Disable SMBv1** — runs `Set-SmbServerConfiguration -EnableSMB1Protocol $false` and removes the `SMB1Protocol` Windows feature (the WannaCry/EternalBlue vector)
- **Disable NetBIOS over TCP/IP** — sets `TcpipNetbios=2` on all network adapters via WMI, stopping NetBIOS name broadcasts and NBNS poisoning attacks
- **Ensure Defender Real-Time Protection** — sets policy keys to prevent Defender being disabled and runs `Set-MpPreference -DisableRealtimeMonitoring $false`

### ⚠ Advanced Tweaks
Selected individually before applying via a confirmation dialog:
- **Processor Scheduling → Programs** (Win32PrioritySeparation=38)
- **Disable Dynamic Tick** — `bcdedit /set disabledynamictick yes`, forces constant high-res IRQ8 timer
- **Disable CPU Throttling** — sets THROTTLE_POLICY ValueMax=0, prevents Windows pulling background CPU clocks
- **Ensure SSD TRIM** — `fsutil behavior set disabledeletenotify 0`
- **Aggressive Animation Disabling** — kills UserPreferencesMask bits, TaskbarAnimations, MinAnimate, ListviewShadow

---

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Administrator privileges (required for registry, service, and hosts file changes)

---

## Releases

1. Go to [Releases](https://github.com/ConnorCorn07/win11op/releases) and download the latest `.exe`
2. Right-click → **Run as Administrator**

## Build from Source

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone the repo:
   ```
   git clone https://github.com/ConnorCorn07/win11op.git
   ```
3. Build:
   ```
   cd win11op/buildwinop
   dotnet build -c Release
   ```
4. Run as Administrator:
   ```
   bin\Release\net8.0-windows\Win11Optimizer.exe
   ```

---

## Notes

- A **reboot is required** after applying tweaks for HAGS, timer resolution, and SMBv1 changes to take full effect
- Bloatware removal cannot be undone — removed apps must be reinstalled from the Microsoft Store
- All registry changes are backed up to `tweaks_backup.json` next to the exe before being applied
- Windows Recall tweaks are a no-op on non-Copilot+ PCs — safe to apply on any hardware
- NVIDIA telemetry tweaks are a no-op if NVIDIA drivers are not installed
- The hosts file block list is cleanly removed by the Privacy undo function

---

## License

MIT — see [LICENSE](LICENSE)

> ⚠ **AI Disclosure:** This project contains code written with the assistance of generative AI (Claude by Anthropic).