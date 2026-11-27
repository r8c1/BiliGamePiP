<div align="center">
  <img src="docs/assets/logo.png" alt="BiliGamePiP Logo" width="112" height="112" />
  <h1>BiliGamePiP</h1>
  <p><strong>玩游戏时，不切屏掌控 B 站置顶小窗</strong></p>
  <p>专为《原神》《明日方舟：终末地》等 Windows 游戏玩家打造的 B 站独立置顶小窗 + 全局快捷键工具</p>

  <p>
    <a href="https://r8c1.github.io/BiliGamePiP/">
      <img src="https://img.shields.io/badge/产品官网-BiliGamePiP-fb7299?style=for-the-badge&logo=googlechrome&logoColor=white" alt="Website" />
    </a>
    <a href="https://github.com/r8c1/BiliGamePiP/releases/latest">
      <img src="https://img.shields.io/github/v/release/r8c1/BiliGamePiP?style=for-the-badge&color=38bdf8&label=最新版本" alt="Latest Release" />
    </a>
    <a href="https://github.com/r8c1/BiliGamePiP/releases">
      <img src="https://img.shields.io/github/downloads/r8c1/BiliGamePiP/total?style=for-the-badge&color=22c55e&label=总下载量" alt="Downloads" />
    </a>
    <a href="LICENSE">
      <img src="https://img.shields.io/badge/License-MIT-8b5cf6?style=for-the-badge" alt="MIT License" />
    </a>
  </p>

  <p>
    <a href="https://github.com/r8c1/BiliGamePiP/releases/latest"><strong>⬇ 前往 Releases 下载 Windows 安装包</strong></a>
    &nbsp;·&nbsp;
    <a href="https://r8c1.github.io/BiliGamePiP/"><strong>🌐 访问官方介绍页</strong></a>
    &nbsp;·&nbsp;
    <a href="https://github.com/r8c1/BiliGamePiP/issues"><strong>💬 反馈建议 / Bug</strong></a>
  </p>
</div>

---

## ✨ 为什么用 BiliGamePiP？

打副本、跑图找宝箱、跟攻略视频时，每次切出游戏暂停或回退 5 秒都很打断体验。**BiliGamePiP** 把这件事做到最简单：

* 📦 **单安装包即装即用**：支持 **简体中文 / English** 安装向导，无需浏览器插件，无需额外安装 .NET 运行时。
* 📺 **原生 Edge WebView2 内核**：完整保留 B 站账号登录态、实时弹幕、历史记录与高画质；一键切入置顶小窗自动隐藏页眉与评论区。
* ⌨️ **游戏内不切屏控制**：采用独立消息窗口注册标准 Win32 全局热键（`RegisterHotKey`），切小窗、玩管理员权限游戏全程稳定生效，**绝不抢游戏焦点**。
* 🎯 **贴心避开游戏热键**：默认使用小键盘 `7 / 8 / 9 / 0 / .`，与《原神》《终末地》主键盘 `1-4` 切人完全独立；所有热键与快进秒数均可自定义。
* 🔒 **一键鼠标穿透防误触**：锁定小窗后鼠标直接穿透点击背后游戏，按 `Alt` 或开背包甩鼠标绝不误点失焦。
* 🛡️ **反作弊友好 & 完全开源**：**零进程注入、零内存读写、零模拟键鼠**，纯标准系统热键控制自身窗口内网页，代码 MIT 开源公开审计。

---

## ⌨️ 默认快捷键

> 使用小键盘数字键时请确保 `NumLock` 已开启；无小键盘（68 / 87 配列）用户可在软件右上角 **「⚙ 快捷键」** 中自由改键。

| 默认快捷键 | 功能 | 说明 |
| :---: | :--- | :--- |
| **`Num7`** | 后退 5 秒 | 支持长按连续快退，视频顶部轻提示当前进度 |
| **`Num8`** | 暂停 / 播放 | 同步控制 B 站视频与弹幕暂停/继续 |
| **`Num9`** | 快进 5 秒 | 支持长按连续快进，跟攻略快人一步 |
| **`Num0`** | 鼠标穿透锁定 / 解锁 | 锁定后隐藏顶栏且鼠标穿透；托盘图标亦可随时解锁 |
| **`Num.`** | 切换普通选片 / 置顶小窗 | 自由拖动与边缘缩放，自动记忆上次小窗位置 |

---

## 🚀 三步快速上手

1. **安装软件**：前往 [Releases 页面](https://github.com/r8c1/BiliGamePiP/releases/latest) 下载最新的 `BiliGamePiP-Setup-*.exe`，双击按中文向导安装。
   > 若 Windows SmartScreen 弹出未签名提示，点击 **「更多信息」→「仍要运行」** 即可（安装包均由 GitHub Actions 云端公开构建）。
2. **选视频并置顶小窗**：打开软件扫码登录 B 站（登录态永久保存），打开要看的视频，点击右上角 **「📌 小窗置顶」**（或按 `Num.`）并拖到屏幕角落。
3. **进游戏畅玩**：将《原神》/《明日方舟：终末地》的画面显示模式设为 **无边框窗口化**，即可在游戏内按小键盘随时控制视频。

---

## ❓ 常见问题

<details>
<summary><strong>1. 为什么启动时会弹出管理员权限（UAC）提示？</strong></summary>
<p>《原神》《明日方舟：终末地》等游戏常以管理员权限运行。受 Windows UIPI 特权隔离限制，普通权限工具在管理员游戏前台时无法稳定接收全局快捷键。本软件以同等权限运行，仅监听全局热键并操作自身内置网页，完全不触碰游戏进程。</p>
</details>

<details>
<summary><strong>2. 进入游戏后小窗被遮住了怎么办？</strong></summary>
<p>请将游戏画面模式从「独占全屏」改为 <strong>无边框窗口化</strong>，所有桌面置顶小窗即可正常悬浮在游戏画面上方。</p>
</details>

<details>
<summary><strong>3. 开启鼠标穿透后忘记解锁快捷键怎么办？</strong></summary>
<p>右键点击 Windows 右下角任务栏里的 <strong>BiliGamePiP 托盘图标</strong>，随时可点击「解锁鼠标穿透」或「退出置顶小窗」。</p>
</details>

---

## 🛠️ 本地构建说明（开发者）

本项目基于 **.NET 8 (WinForms)** + **Microsoft Edge WebView2** + **Inno Setup 6** 构建：

```powershell
# Windows 环境下安装 .NET 8 SDK 与 Inno Setup 6 后运行：
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

安装包将生成于 `dist\BiliGamePiP-Setup-*.exe`。推送 `v*` 标签（如 `git tag v1.1.0 && git push --tags`）会自动通过 GitHub Actions 编译并发布到 **GitHub Releases**。

---

## 📄 开源协议

本项目基于 [MIT License](LICENSE) 开源，欢迎 Star、Fork 或提交 Issue 提出你的想法！
