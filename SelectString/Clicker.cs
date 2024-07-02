﻿using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using System;
using System.Collections.Generic;

namespace SelectString
{
    unsafe class Clicker
    {
        private readonly Dictionary<string, long> ThrottleTimes = [];
        delegate void ReceiveEventDelegate(IntPtr addon, EventType evt, uint a3, IntPtr a4, IntPtr a5);

        internal void ClickItemThrottled(IntPtr addonPtr, ushort index, string identifier)
        {
            if (ThrottleTimes.TryGetValue(identifier, out var time))
            {
                if (Environment.TickCount64 < time)
                {
                    Svc.Log.Information($"Click for {identifier} was throttled.");
                    return;
                }
            }
            ThrottleTimes[identifier] = Environment.TickCount64 + 500;
            Svc.Log.Information($"Click for {identifier} was processed.");
            var addon = new AddonMaster.SelectString(addonPtr);
            addon.Entries[index].Select();
        }
    }
}
