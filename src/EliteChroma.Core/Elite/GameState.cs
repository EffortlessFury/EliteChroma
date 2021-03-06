﻿using System;
using System.Collections.Generic;
using EliteFiles.Bindings;
using EliteFiles.Graphics;
using EliteFiles.Journal;
using EliteFiles.Journal.Events;
using EliteFiles.Status;

namespace EliteChroma.Elite
{
    public sealed class GameState
    {
        internal GameState()
        {
        }

        public bool IsRunning { get; internal set; }

        public IReadOnlyDictionary<string, Binding> Bindings { get; internal set; }

        public DeviceKeySet PressedModifiers { get; internal set; }

        public StatusEntry Status { get; internal set; }

        public GuiColourMatrix GuiColour { get; internal set; }

        public string MusicTrack { get; internal set; }

        public StartJump.FsdJumpType FsdJumpType { get; internal set; }

        public DateTimeOffset FsdJumpChange { get; internal set; }

        public bool InMainMenu => MusicTrack == "MainMenu";

        public bool InGalacticPowers => MusicTrack == "GalacticPowers";

        public bool InCockpit
        {
            get
            {
                if (Status.Flags == Flags.None)
                {
                    return false;
                }

                switch (Status.GuiFocus)
                {
                    case GuiFocus.GalaxyMap:
                    case GuiFocus.SystemMap:
                    case GuiFocus.Codex:
                        return false;
                }

                if (InGalacticPowers)
                {
                    return false;
                }

                return true;
            }
        }

        public bool InWitchSpace =>
            FsdJumpType == StartJump.FsdJumpType.Hyperspace
            && (DateTimeOffset.UtcNow - FsdJumpChange).TotalSeconds >= 5;
    }
}
