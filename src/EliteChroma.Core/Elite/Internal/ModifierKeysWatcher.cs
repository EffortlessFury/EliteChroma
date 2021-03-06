﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using EliteFiles.Bindings;
using EliteFiles.Bindings.Devices;
using static EliteChroma.Elite.Internal.NativeMethods;

namespace EliteChroma.Elite.Internal
{
    internal sealed class ModifierKeysWatcher : IDisposable
    {
        private readonly Dictionary<VirtualKey, DeviceKey> _watch;
        private readonly Timer _timer;

        private DeviceKeySet _currPressed;

        public ModifierKeysWatcher()
        {
            _watch = new Dictionary<VirtualKey, DeviceKey>();

            _timer = new Timer
            {
                Interval = 100,
                AutoReset = true,
                Enabled = false,
            };
            _timer.Elapsed += Timer_Elapsed;
        }

        public event EventHandler<DeviceKeySet> Changed;

        public void Watch(IEnumerable<DeviceKey> modifiers)
        {
            _watch.Clear();

            foreach (var m in modifiers.Where(x => x.Device == Device.Keyboard))
            {
                var key = KeyMappings.EliteKeys[m.Key];
                _watch[key] = m;
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var newPressed = new DeviceKeySet(GetAllPressedModifiers());

            if (!newPressed.Equals(_currPressed))
            {
                _currPressed = newPressed;

                Changed?.Invoke(this, _currPressed);
            }
        }

        private IEnumerable<DeviceKey> GetAllPressedModifiers()
        {
            foreach (var kv in _watch)
            {
                if ((GetAsyncKeyState(kv.Key) & 0x8000) != 0)
                {
                    yield return kv.Value;
                }
            }
        }
    }
}
