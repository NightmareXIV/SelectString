using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.SimpleGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelectString;
public class ConfigWindow : ECommons.SimpleGui.ConfigWindow
{
    private ConfigWindow()
    {
        EzConfigGui.Init(this);
        Svc.PluginInterface.UiBuilder.OpenConfigUi += EzConfigGui.Open;
    }

    public override void OnClose()
    {
        EzConfig.Save();
    }

    public override void Draw()
    {
        ImGuiEx.TextWrapped($"Enable SelectString for these windows:");
        foreach(var x in StaticData.AddonMasterOpts)
        {
            ImGuiEx.CollectionCheckbox(StaticData.GetAddonMasterName(x), x.Name, C.DisabledAddons, true);
        }
        if(ImGui.CollapsingHeader("Debug"))
        {
            if(GenericHelpers.TryGetAddonMaster<AddonMaster.ContextMenu>(out var m) && m.IsAddonReady)
            {
                foreach(var x in m.Entries)
                {
                    ImGuiEx.Text($"{x.Text}: {x.Enabled}");
                }
            }
        }
    }
}
