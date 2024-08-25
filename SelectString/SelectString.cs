using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
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
        bool exec = false;
        List<(float X, float Y, string Text)> DrawList = new();
        Clicker clickMgr;

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
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectString>(out var ss) && ss.IsAddonReady)
                    AddToDrawList(ss);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectIconString>(out var sis) && sis.IsAddonReady)
                    AddToDrawList(sis);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.ContextMenu>(out var cm) && cm.IsAddonReady)
                    AddToDrawList(cm);
            }
            catch (Exception e)
            {
                Svc.Chat.Print(e.Message + "\n" + e.StackTrace);
            }
        }

        private void Draw()
        {
            foreach (var e in DrawList)
            {
                ImGuiHelpers.ForceNextWindowMainViewport();
                var textSize = ImGui.CalcTextSize(e.Text);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(e.X - textSize.X - 2, e.Y - textSize.Y / 2f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.Begin("##selectstring" + e.Text, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);
                ImGui.TextUnformatted(e.Text);
                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }

        private void AddToDrawList(AddonMaster.SelectString am)
        {
            //Svc.Chat.Print($"{addonSelectStringBase->X * addonSelectStringBase->Scale}, {addonSelectStringBase->Y * addonSelectStringBase->Scale}");
            if (am.Base->UldManager.NodeListCount < 3) return;
            var listNode = am.Base->UldManager.NodeList[2];
            var textNode = (AtkTextNode*)am.Base->UldManager.NodeList[3];
            //Svc.Chat.Print($"{listNode->X}, {listNode->Y}");
            for (ushort i = 0; i < Math.Min(am.Addon->PopupMenu.PopupMenu.EntryCount, 12); i++)
            {
                if (i < 10)
                {
                    var state = Svc.KeyState.GetRawValue(49 + (i == 9 ? -1 : i));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(49 + (i == 9 ? -1 : i), 0);
                        clickMgr.ClickItemThrottled(am, i, textNode->NodeText.ToString());
                    }
                    state = Svc.KeyState.GetRawValue(97 + (i == 9 ? -1 : i));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(97 + (i == 9 ? -1 : i), 0);
                        clickMgr.ClickItemThrottled(am, i, textNode->NodeText.ToString());
                    }
                }
                else
                {
                    var state = Svc.KeyState.GetRawValue(189 + (i == 10 ? 0 : -2));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(189 + (i == 10 ? 0 : -2), 0);
                        clickMgr.ClickItemThrottled(am, i, textNode->NodeText.ToString());
                    }
                }

                //Svc.Chat.Print(Marshal.PtrToStringUTF8((IntPtr)addonSelectString->PopupMenu.EntryNames[i]));
                var itemNode = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                //Svc.Chat.Print($"{itemNode->X}, {itemNode->Y}");
                DrawList.Add((
                    am.Base->X + (listNode->X + itemNode->X) * am.Base->Scale,
                    am.Base->Y + (listNode->Y + itemNode->Y + itemNode->Height / 2) * am.Base->Scale,
                    $"{(i == 11 ? "=" : (i == 10 ? "-" : (i == 9 ? 0 : i + 1)))}"
                    ));
            }
        }

        private void AddToDrawList(AddonMaster.SelectIconString am)
        {
            if (am.Base->UldManager.NodeListCount < 3) return;
            var listNode = am.Base->UldManager.NodeList[2];
            var textNode = (AtkTextNode*)am.Base->UldManager.NodeList[3];
            for (ushort i = 0; i < Math.Min(am.Addon->PopupMenu.PopupMenu.EntryCount, 12); i++)
            {
                if (i < 10)
                {
                    var state = Svc.KeyState.GetRawValue(49 + (i == 9 ? -1 : i));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(49 + (i == 9 ? -1 : i), 0);
                        clickMgr.ClickItemThrottled(am, i, textNode->NodeText.ToString());
                    }
                    state = Svc.KeyState.GetRawValue(97 + (i == 9 ? -1 : i));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(97 + (i == 9 ? -1 : i), 0);
                        clickMgr.ClickItemThrottled(am, i, textNode->NodeText.ToString());
                    }
                }
                else
                {
                    var state = Svc.KeyState.GetRawValue(189 + (i == 10 ? 0 : -2));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(189 + (i == 10 ? 0 : -2), 0);
                        clickMgr.ClickItemThrottled(am, i, textNode->NodeText.ToString());
                    }
                }

                var itemNode = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                DrawList.Add((
                    am.Base->X + (listNode->X + itemNode->X) * am.Base->Scale,
                    am.Base->Y + (listNode->Y + itemNode->Y + itemNode->Height / 2) * am.Base->Scale,
                    $"{(i == 11 ? "=" : (i == 10 ? "-" : (i == 9 ? 0 : i + 1)))}"
                    ));
            }
        }

        private void AddToDrawList(AddonMaster.ContextMenu am)
        {
            if (am.Base->AtkValuesCount <= 7) return;
            var listNode = am.Base->UldManager.NodeList[2];
            for (ushort i = 0; i < Math.Min(am.Entries.Length, 12); i++)
            {
                var entry = am.Entries[i];
                if (!entry.IsNativeEntry) continue;
                if (i < 10)
                {
                    var state = Svc.KeyState.GetRawValue(49 + (i == 9 ? -1 : i));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(49 + (i == 9 ? -1 : i), 0);
                        clickMgr.ClickItemThrottled(am, (ushort)entry.Index, entry.Text);
                    }
                    state = Svc.KeyState.GetRawValue(97 + (i == 9 ? -1 : i));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(97 + (i == 9 ? -1 : i), 0);
                        clickMgr.ClickItemThrottled(am, (ushort)entry.Index, entry.Text);
                    }
                }
                else
                {
                    var state = Svc.KeyState.GetRawValue(189 + (i == 10 ? 0 : -2));
                    if (state == 3)
                    {
                        Svc.KeyState.SetRawValue(189 + (i == 10 ? 0 : -2), 0);
                        clickMgr.ClickItemThrottled(am, (ushort)entry.Index, entry.Text);
                    }
                }

                var itemNode = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                DrawList.Add((
                am.Base->X + (listNode->X + itemNode->X) * am.Base->Scale,
                    am.Base->Y + (listNode->Y + itemNode->Y + itemNode->Height / 2) * am.Base->Scale,
                    $"{(i == 11 ? "=" : (i == 10 ? "-" : (i == 9 ? 0 : i + 1)))}"
                    ));
            }
        }
    }
}
