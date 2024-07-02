using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SelectString
{
    public unsafe class SelectString : IDalamudPlugin
    {
        public string Name => "SelectString";
        private bool exec = false;
        private readonly List<(float X, float Y, string Text)> DrawList = [];
        private readonly Clicker clickMgr;

        public SelectString(IDalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this);
            clickMgr = new Clicker();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.Commands.AddHandler("/ss", new CommandInfo(delegate { exec = true; }));
        }

        public void Dispose()
        {
            Svc.Framework.Update -= Tick;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            Svc.Commands.RemoveHandler("/ss");
            ECommonsMain.Dispose();
        }

        private void Tick(object framework)
        {
            /*if (!exec) return;
            exec = false;*/
            DrawList.Clear();
            try
            {
                var addonSelectStringPtr = Svc.GameGui.GetAddonByName("SelectString", 1);
                if (addonSelectStringPtr != IntPtr.Zero)
                {
                    if (Svc.GameGui.GetAddonByName("HousingMenu", 1) != IntPtr.Zero) return;
                    var addonSelectString = (AddonSelectString*)addonSelectStringPtr;
                    var addonSelectStringBase = (AtkUnitBase*)addonSelectStringPtr;
                    //Svc.Chat.Print($"{addonSelectStringBase->X * addonSelectStringBase->Scale}, {addonSelectStringBase->Y * addonSelectStringBase->Scale}");
                    if (addonSelectStringBase->UldManager.NodeListCount < 3) return;
                    var listNode = addonSelectStringBase->UldManager.NodeList[2];
                    //Svc.Chat.Print($"{listNode->X}, {listNode->Y}");
                    for (ushort i = 0; i < Math.Min(addonSelectString->PopupMenu.PopupMenu.EntryCount, 12); i++)
                    {
                        if (i < 10)
                        {
                            var state = Svc.KeyState.GetRawValue(49 + (i == 9 ? -1 : i));
                            if (state == 3)
                            {
                                Svc.KeyState.SetRawValue(49 + (i == 9 ? -1 : i), 0);
                                clickMgr.ClickItemThrottled((IntPtr)addonSelectString, i, ((AtkTextNode*)(addonSelectStringBase->UldManager.NodeList[3]))->NodeText.ToString());
                            }
                            state = Svc.KeyState.GetRawValue(97 + (i == 9 ? -1 : i));
                            if (state == 3)
                            {
                                Svc.KeyState.SetRawValue(97 + (i == 9 ? -1 : i), 0);
                                clickMgr.ClickItemThrottled((IntPtr)addonSelectString, i, ((AtkTextNode*)(addonSelectStringBase->UldManager.NodeList[3]))->NodeText.ToString());
                            }
                        }
                        else
                        {
                            var state = Svc.KeyState.GetRawValue(189 + (i == 10 ? 0 : -2));
                            if (state == 3)
                            {
                                Svc.KeyState.SetRawValue(189 + (i == 10 ? 0 : -2), 0);
                                clickMgr.ClickItemThrottled((IntPtr)addonSelectString, i, ((AtkTextNode*)(addonSelectStringBase->UldManager.NodeList[3]))->NodeText.ToString());
                            }
                        }

                        //Svc.Chat.Print(Marshal.PtrToStringUTF8((IntPtr)addonSelectString->PopupMenu.EntryNames[i]));
                        var itemNode = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                        //Svc.Chat.Print($"{itemNode->X}, {itemNode->Y}");
                        DrawList.Add((
                            addonSelectStringBase->X + (listNode->X + itemNode->X) * addonSelectStringBase->Scale,
                            addonSelectStringBase->Y + (listNode->Y + itemNode->Y + itemNode->Height / 2) * addonSelectStringBase->Scale,
                            $"{(i == 11 ? "=" : (i == 10 ? "-" : (i == 9 ? 0 : i + 1)))}"
                            ));
                    }
                }
            }
            catch (Exception e)
            {
                Svc.Chat.Print(e.Message + "\n" + e.StackTrace);
            }
        }

        private void Draw()
        {
            foreach (var (X, Y, Text) in DrawList)
            {
                ImGuiHelpers.ForceNextWindowMainViewport();
                var textSize = ImGui.CalcTextSize(Text);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(X - textSize.X - 2, Y - textSize.Y / 2f));
                using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(2f, 0f));
                using var size = ImRaii.PushStyle(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.Begin("##selectstring" + Text, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);
                ImGui.TextUnformatted(Text);
                ImGui.End();
            }
        }
    }
}
