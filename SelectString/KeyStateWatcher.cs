using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace SelectString;
public unsafe class KeyStateWatcher() : IDisposable
{
    public bool Enabled
    {
        set
        {
            if (value)
                Svc.Framework.Update += CheckKeyStates;
            else
                Svc.Framework.Update -= CheckKeyStates;
        }
    }

    public static event Action<int> NumKeyPressed;
    public static void OnNumKeyPressed(int idx) => NumKeyPressed?.Invoke(idx);

    private void CheckKeyStates(IFramework framework)
    {
        if (RaptureAtkModule.Instance()->AtkModule.IsTextInputActive()) return;
        for (var i = 0; i < Math.Min(SelectString.ActiveButtons.Count, 12); i++)
        {
            if (i < 10)
            {
                var state = Svc.KeyState.GetRawValue(49 + (i == 9 ? -1 : i));
                if (state == 3)
                {
                    Svc.KeyState.SetRawValue(49 + (i == 9 ? -1 : i), 0);
                    NumKeyPressed?.Invoke(i);
                }
                state = Svc.KeyState.GetRawValue(97 + (i == 9 ? -1 : i));
                if (state == 3)
                {
                    Svc.KeyState.SetRawValue(97 + (i == 9 ? -1 : i), 0);
                    NumKeyPressed?.Invoke(i);
                }
            }
            else
            {
                var state = Svc.KeyState.GetRawValue(189 + (i == 10 ? 0 : -2));
                if (state == 3)
                {
                    Svc.KeyState.SetRawValue(189 + (i == 10 ? 0 : -2), 0);
                    NumKeyPressed?.Invoke(i);
                }
            }
        }
    }

    public void Dispose() => Svc.Framework.Update -= CheckKeyStates;
}
