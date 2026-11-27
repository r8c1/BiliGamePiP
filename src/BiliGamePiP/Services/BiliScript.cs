using System.Text.Json;

namespace BiliGamePiP.Services;

public static class BiliScript
{
    private const string CommonHelperJs = """
        function __bgpGetBiliPlayer() {
            try {
                if (window.player && typeof window.player.play === 'function' && typeof window.player.pause === 'function') {
                    return window.player;
                }
            } catch (_) {}
            return null;
        }

        function __bgpFindMedia() {
            const candidates = [];
            const collect = (root) => {
                if (!root || !root.querySelectorAll) return;
                root.querySelectorAll('video, bwp-video').forEach(el => candidates.push(el));
                root.querySelectorAll('*').forEach(el => {
                    if (el.shadowRoot) collect(el.shadowRoot);
                });
            };
            collect(document);
            document.querySelectorAll('iframe').forEach(frame => {
                try {
                    if (frame.contentDocument) collect(frame.contentDocument);
                } catch (_) {}
            });
            if (candidates.length === 0) return null;

            candidates.sort((a, b) => {
                const aInBpx = a.closest && a.closest('#bilibili-player, .bpx-player-video-wrap') ? 1 : 0;
                const bInBpx = b.closest && b.closest('#bilibili-player, .bpx-player-video-wrap') ? 1 : 0;
                if (aInBpx !== bInBpx) return bInBpx - aInBpx;

                const aReady = (a.readyState || 0) > 0 || (a.duration || 0) > 0 ? 1 : 0;
                const bReady = (b.readyState || 0) > 0 || (b.duration || 0) > 0 ? 1 : 0;
                if (aReady !== bReady) return bReady - aReady;

                const ra = a.getBoundingClientRect ? a.getBoundingClientRect() : { width: 0, height: 0 };
                const rb = b.getBoundingClientRect ? b.getBoundingClientRect() : { width: 0, height: 0 };
                return (rb.width * rb.height) - (ra.width * ra.height);
            });
            return candidates[0];
        }

        function __bgpFormatTime(sec) {
            if (!isFinite(sec) || sec < 0) return '00:00';
            sec = Math.floor(sec);
            const h = Math.floor(sec / 3600);
            const m = Math.floor((sec % 3600) / 60);
            const s = sec % 60;
            const pad = n => String(n).padStart(2, '0');
            return h > 0 ? `${h}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
        }

        function __bgpShowOsd(text) {
            let el = document.getElementById('biligamepip-osd');
            if (!el) {
                el = document.createElement('div');
                el.id = 'biligamepip-osd';
                Object.assign(el.style, {
                    position: 'fixed',
                    top: '14px',
                    left: '50%',
                    transform: 'translateX(-50%)',
                    zIndex: '2147483647',
                    pointerEvents: 'none',
                    padding: '6px 14px',
                    borderRadius: '999px',
                    background: 'rgba(18, 18, 24, 0.86)',
                    color: '#ffffff',
                    fontSize: '13px',
                    fontWeight: '600',
                    fontFamily: '"Microsoft YaHei UI", "Segoe UI", sans-serif',
                    letterSpacing: '0.3px',
                    boxShadow: '0 4px 16px rgba(0, 0, 0, 0.45)',
                    border: '1px solid rgba(251, 114, 153, 0.6)',
                    whiteSpace: 'nowrap',
                    transition: 'opacity 0.22s ease'
                });
                document.documentElement.appendChild(el);
            }
            el.textContent = text;
            el.style.opacity = '1';
            clearTimeout(window.__bgpOsdTimer);
            window.__bgpOsdTimer = setTimeout(() => {
                if (el) el.style.opacity = '0';
            }, 1150);
        }
        """;

    public static string GetTogglePlayPauseScript() => $$"""
        (() => {
            {{CommonHelperJs}}
            // 优先走 B 站原生 player 实例，确保播放状态与弹幕完全同步
            const bp = __bgpGetBiliPlayer();
            if (bp && typeof bp.isPaused === 'function') {
                if (bp.isPaused()) {
                    bp.play();
                    __bgpShowOsd('▶ 继续播放');
                } else {
                    bp.pause();
                    __bgpShowOsd('⏸ 已暂停');
                }
                return;
            }

            const v = __bgpFindMedia();
            if (v) {
                if (v.paused) {
                    const p = v.play();
                    if (p && typeof p.catch === 'function') p.catch(() => {});
                    __bgpShowOsd('▶ 继续播放');
                } else {
                    v.pause();
                    __bgpShowOsd('⏸ 已暂停');
                }
                return;
            }

            const playBtn = document.querySelector('.bpx-player-ctrl-play');
            if (playBtn) {
                playBtn.click();
                __bgpShowOsd('⏯ 切换播放状态');
                return;
            }

            __bgpShowOsd('未检测到正在播放的视频');
        })();
        """;

    public static string GetSeekScript(int deltaSeconds) => $$"""
        (() => {
            {{CommonHelperJs}}
            const delta = {{deltaSeconds}};

            const bp = __bgpGetBiliPlayer();
            if (bp && typeof bp.getCurrentTime === 'function' && typeof bp.seek === 'function') {
                const cur = Number(bp.getCurrentTime()) || 0;
                const dur = typeof bp.getDuration === 'function' ? Number(bp.getDuration()) : NaN;
                let target = cur + delta;
                if (target < 0) target = 0;
                if (isFinite(dur) && dur > 0 && target > dur) target = Math.max(0, dur - 0.1);
                bp.seek(target);
                const prefix = delta >= 0 ? `⏩ +${delta}秒` : `⏪ ${delta}秒`;
                __bgpShowOsd(`${prefix}  ${__bgpFormatTime(target)}`);
                return;
            }

            const v = __bgpFindMedia();
            if (!v) {
                __bgpShowOsd('未检测到正在播放的视频');
                return;
            }
            const cur = Number(v.currentTime) || 0;
            const dur = Number(v.duration);
            let target = cur + delta;
            if (target < 0) target = 0;
            if (isFinite(dur) && dur > 0 && target > dur) target = Math.max(0, dur - 0.1);
            v.currentTime = target;
            const prefix = delta >= 0 ? `⏩ +${delta}秒` : `⏪ ${delta}秒`;
            __bgpShowOsd(`${prefix}  ${__bgpFormatTime(target)}`);
        })();
        """;

    public static string GetShowOsdScript(string message)
    {
        string safeText = JsonSerializer.Serialize(message);
        return $$"""
            (() => {
                {{CommonHelperJs}}
                __bgpShowOsd({{safeText}});
            })();
            """;
    }

    public static string GetMiniModeScript(bool enable)
    {
        if (!enable)
        {
            return """
                (() => {
                    if (window.__bgpMiniObserver) {
                        window.__bgpMiniObserver.disconnect();
                        window.__bgpMiniObserver = null;
                    }
                    document.documentElement.classList.remove('biligamepip-mini-active');
                    const st = document.getElementById('biligamepip-mini-style');
                    if (st) st.remove();
                    window.dispatchEvent(new Event('resize'));
                })();
                """;
        }

        return """
            (() => {
                const styleId = 'biligamepip-mini-style';
                let style = document.getElementById(styleId);
                if (!style) {
                    style = document.createElement('style');
                    style.id = styleId;
                    style.textContent = `
                        html.biligamepip-mini-active,
                        html.biligamepip-mini-active body {
                            overflow: hidden !important;
                            background: #000 !important;
                            margin: 0 !important;
                            padding: 0 !important;
                        }
                        html.biligamepip-mini-active #bilibili-player,
                        html.biligamepip-mini-active .bpx-player-container,
                        html.biligamepip-mini-active #live-player-ctnr,
                        html.biligamepip-mini-active .live-player-mounter {
                            position: fixed !important;
                            top: 0 !important;
                            left: 0 !important;
                            right: 0 !important;
                            bottom: 0 !important;
                            width: 100vw !important;
                            height: 100vh !important;
                            max-width: none !important;
                            max-height: none !important;
                            min-width: 0 !important;
                            min-height: 0 !important;
                            margin: 0 !important;
                            padding: 0 !important;
                            z-index: 2147483640 !important;
                            background: #000 !important;
                        }
                        html.biligamepip-mini-active .bpx-player-primary-area,
                        html.biligamepip-mini-active .bpx-player-video-area,
                        html.biligamepip-mini-active .bpx-player-video-perch,
                        html.biligamepip-mini-active .bpx-player-video-wrap {
                            width: 100% !important;
                            height: 100% !important;
                        }
                        html.biligamepip-mini-active .bpx-player-sending-area,
                        html.biligamepip-mini-active .bpx-player-auxiliary,
                        html.biligamepip-mini-active .bili-header,
                        html.biligamepip-mini-active #biliMainHeader {
                            display: none !important;
                        }
                    `;
                    document.head.appendChild(style);
                }

                const syncClass = () => {
                    const hasPlayer = !!document.querySelector('#bilibili-player, .bpx-player-container, #live-player-ctnr, .live-player-mounter');
                    if (hasPlayer) {
                        if (!document.documentElement.classList.contains('biligamepip-mini-active')) {
                            document.documentElement.classList.add('biligamepip-mini-active');
                            window.dispatchEvent(new Event('resize'));
                        }
                    } else {
                        document.documentElement.classList.remove('biligamepip-mini-active');
                    }
                };

                syncClass();
                if (!window.__bgpMiniObserver) {
                    window.__bgpMiniObserver = new MutationObserver(syncClass);
                    window.__bgpMiniObserver.observe(document.documentElement, { childList: true, subtree: true });
                }
                setTimeout(() => window.dispatchEvent(new Event('resize')), 120);
            })();
            """;
    }
}
