<div align="center">

# 🚀 TweakHub <sup><kbd>PUBLIC BETA</kbd></sup>

### *The Command Center for Windows Tweakers*

[![Status](https://img.shields.io/badge/Status-Public%20Beta-orange.svg)]()
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-orange.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011-orange.svg)](https://www.microsoft.com/windows/)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)](https://github.com/yourusername/tweakhub)

**TweakHub** is a modern command center for Windows 11 that curates and launches the best optimization tools and advanced tweaks—helping you unlock your system’s full potential. It centralizes, secures, and streamlines everything in one clean interface.

[🔥 **Download Latest Release**](https://github.com/PrimeBuild-pc/TweakHub/releases/tag/v1.0.0) • [📖 **Documentation**](#-documentation) • [🤝 **Contributing**](#-contributing-to-tweakhub)

> [!WARNING]
> **Public Beta:** il progetto è in fase di test attivo. Possibili bug e cambiamenti rapidi. Effettua il backup prima di applicare modifiche.

</div>

---

<img width="1608" height="1025" alt="image" src="https://github.com/user-attachments/assets/ecb8bdbe-7cf2-44b7-99d7-5981efe05101" />

## 📦 **Installation & Quick Start**

### 🚀 **Option 1: Ready-to-Run executable** *(Recommended)*

```bash
# 1. Download the latest release
wget https://github.com/PrimeBuild-pc/TweakHub/releases/download/v1.0.0/TweakHub-v1.0.0-win-x64-portable.zip

# 2. Extract to your preferred location
unzip TweakHub.zip -d "C:\\Tools\\TweakHub"

# 3. Run as Administrator
# Right-click TweakHub.exe → "Run as administrator"
```

### 🛠️ **Option 2: Build from source**

```bash
# Prerequisites: .NET 8.0 SDK
git clone https://github.com/yourusername/tweakhub.git
cd tweakhub

# Build and publish
dotnet build --configuration Release
dotnet publish --configuration Release --self-contained true --runtime win-x64 --output ./publish

# Run the application
cd publish
./TweakHub.exe
```

### ⚡ **First Launch**

1. **🔐 Administrator Rights**: Essential for registry modifications
2. **📊 System Analysis**: Automatic hardware detection and registry scanning
3. **🎨 Dark Theme**: Sleek interface with orange accents loads automatically
4. **🛡️ Safety First**: Automatic backup creation before any changes

---

## 🔧 **Troubleshooting**

<details>
<summary><b>❌ Application Won't Start</b></summary>

### 🔍 **Common solutions**

* **🔐 Administrator Rights**: Right-click → "Run as administrator"
* **🖥️ Windows Version**: Ensure Windows 10 1809+ or Windows 11
* **🛡️ Antivirus**: Temporarily disable real-time protection
* **🔄 Restart**: Reboot system and try again

</details>

<details>
<summary><b>⚙️ Registry Tweaks Not Applying</b></summary>

### 🔍 **Diagnostic Steps**

* **🔐 Permissions**: Verify Administrator privileges
* **🛡️ Security Software**: Check antivirus registry protection
* **🔄 Restart Required**: Some tweaks need system restart
* **📋 Compatibility**: Ensure Windows version compatibility

</details>

---

## 📜 **License & Legal**

### 📄 **MIT License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### ⚠️ **Important Disclaimer**

> **🚨 CRITICAL NOTICE**: TweakHub modifies Windows registry settings and system configurations. While all tweaks are carefully selected, tested, and rated for safety, system modifications always carry inherent risks.

#### 🛡️ **Safety Recommendations**

* ✅ **Always run as Administrator** for proper functionality
* ✅ **Create system restore points** before making changes
* ✅ **Use built-in backup functionality** for registry changes
* ✅ **Understand each tweak** before applying it
* ✅ **Test on non-critical systems** first

#### 📋 **Liability**

The developers provide this software "as-is" without warranty. Users assume full responsibility for any system changes. We recommend thorough testing and understanding of each modification.

---

## 💖 Supporta il progetto

Ti piace questo tool? Offrimi un caffè ☕:

[![PayPal](https://img.shields.io/badge/Supporta%20su-PayPal-blue?logo=paypal)](https://paypal.me/PrimeBuildOfficial?country.x=IT&locale.x=it_IT)

**Made with ❤️ and ☕ by the TweakHub Team**

*Optimize your Windows 11 experience with confidence.*
