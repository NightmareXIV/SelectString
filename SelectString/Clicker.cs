using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using System;
using System.Collections.Generic;

namespace SelectString
{
    unsafe class Clicker
    {
        Dictionary<string, long> ThrottleTimes = [];
        delegate void ReceiveEventDelegate(IntPtr addon, EventType evt, uint a3, IntPtr a4, IntPtr a5);

        internal void ClickItemThrottled(AddonMaster.SelectString addon, ushort index, string identifier)
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
            addon.Entries[index].Select();
        }

        internal void ClickItemThrottled(AddonMaster.SelectIconString addon, ushort index, string identifier)
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
            addon.Entries[index].Select();
        }

        internal void ClickItemThrottled(AddonMaster.ContextMenu addon, ushort index, string identifier)
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
            addon.Entries[index].Select();
        }
    }
}
