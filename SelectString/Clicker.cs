using ClickLib;
using ClickLib.Clicks;
using ClickLib.Enums;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SelectString
{
    unsafe class Clicker
    {
        Dictionary<string, long> ThrottleTimes = new();
        delegate void ReceiveEventDelegate(IntPtr addon, EventType evt, uint a3, IntPtr a4, IntPtr a5);

        internal void ClickItemThrottled(IntPtr addonPtr, ushort index, string identifier)
        {
            if(ThrottleTimes.TryGetValue(identifier, out var time))
            {
                if(Environment.TickCount64 < time)
                {
                    PluginLog.Information($"Click for {identifier} was throttled.");
                    return;
                }
            }
            ThrottleTimes[identifier] = Environment.TickCount64 + 500;
            PluginLog.Information($"Click for {identifier} was processed.");
            ClickSelectString.Using(addonPtr).SelectItem(index);
        }
    }
}
