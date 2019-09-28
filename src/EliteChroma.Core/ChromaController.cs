﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colore;
using Colore.Api;
using Colore.Data;
using Colore.Native;
using EliteChroma.Chroma;
using EliteChroma.Elite;
using EliteChroma.Elite.Internal;
using EliteFiles;

namespace EliteChroma.Core
{
    public sealed class ChromaController : IDisposable
    {
        private const int _defaultFps = 25;

        private readonly GameStateWatcher _watcher;
        private readonly LayeredEffect _effect;
        private readonly System.Timers.Timer _animation;
        private readonly SemaphoreSlim _chromaLock = new SemaphoreSlim(1, 1);

        private IChroma _chroma;
        private int _rendering;
        private int _fps;

        private bool _disposed;

        [ExcludeFromCodeCoverage]
        public ChromaController(string gameInstallFolder)
            : this(gameInstallFolder, Folders.GetDefaultGameOptionsFolder(), Folders.GetDefaultJournalFolder())
        {
        }

        public ChromaController(string gameInstallFolder, string gameOptionsFolder, string journalFolder)
        {
            _watcher = new GameStateWatcher(gameInstallFolder, gameOptionsFolder, journalFolder);
            _watcher.Changed += GameState_Changed;

            _animation = new System.Timers.Timer
            {
                AutoReset = true,
                Enabled = false,
            };
            _animation.Elapsed += Animation_Elapsed;
            AnimationFrameRate = _defaultFps;

            _effect = InitChromaEffect(_watcher.GameState);
        }

        public IChromaApi ChromaApi { get; set; } = new NativeApi();

        public AppInfo ChromaAppInfo { get; set; }

        public int AnimationFrameRate
        {
            get => _fps;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(AnimationFrameRate));
                }

                _fps = value;
                if (_fps > 0)
                {
                    _animation.Interval = 1000 / _fps;
                }
            }
        }

        public static bool IsChromaSdkAvailable()
        {
            bool is64Bit = Environment.Is64BitProcess && Environment.Is64BitOperatingSystem;
            var dllName = is64Bit ? "RzChromaSDK64.dll" : "RzChromaSDK.dll";

            var hModule = NativeMethods.LoadLibrary(dllName);

            if (hModule != IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(hModule);
                return true;
            }

            return false;
        }

        public void Start()
        {
            _watcher.Start();
        }

        public void Stop()
        {
            _watcher.Stop();
            ChromaStop().Wait();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _watcher?.Dispose();
            _animation?.Dispose();
            _chromaLock.Dispose();
            _disposed = true;
        }

        private async Task ChromaStart()
        {
            await _chromaLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_chroma != null)
                {
                    return;
                }

                _chroma = await ColoreProvider.CreateAsync(ChromaAppInfo, ChromaApi).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false); // HACK: It seems that Chroma takes a while to get up and running
            }
            finally
            {
                _chromaLock.Release();
            }
        }

        private async Task ChromaStop()
        {
            await _chromaLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_chroma == null)
                {
                    return;
                }

                await _chroma.UninitializeAsync().ConfigureAwait(false);
                _chroma = null;
            }
            finally
            {
                _chromaLock.Release();
            }
        }

        private void GameState_Changed(object sender, EventArgs e)
        {
            Task.Run(RenderEffect);
        }

        private void Animation_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(RenderEffect);
        }

        private async Task RenderEffect()
        {
            if (Interlocked.Exchange(ref _rendering, 1) == 1)
            {
                return;
            }

            try
            {
                if (!_watcher.GameState.IsRunning)
                {
                    await ChromaStop().ConfigureAwait(false);
                    return;
                }

                await ChromaStart().ConfigureAwait(false);

                await _chromaLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    await _effect.Render(_chroma).ConfigureAwait(false);
                }
                finally
                {
                    _chromaLock.Release();
                }

                _animation.Enabled = AnimationFrameRate > 0 && _effect.Layers.Cast<LayerBase>().Any(x => x.Animated);
            }
            finally
            {
                Interlocked.Exchange(ref _rendering, 0);
            }
        }

        private LayeredEffect InitChromaEffect(GameState game)
        {
            var layers =
                from type in typeof(LayerBase).Assembly.GetTypes()
                where type.IsSubclassOf(typeof(LayerBase)) && !type.IsAbstract
                select (LayerBase)Activator.CreateInstance(type);

            var res = new LayeredEffect();

            foreach (var layer in layers)
            {
                layer.Game = game;
                res.Layers.Add(layer);
            }

            return res;
        }
    }
}
