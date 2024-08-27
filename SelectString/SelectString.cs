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
        List<(float X, float Y, string Text)> DrawList = [];
        Clicker clickMgr;
        private const int horizontalOffset = 10;

        public SelectString(IDalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this);
            clickMgr = new Clicker();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.Commands.AddHandler("/ss", new CommandInfo(delegate { exec ^= true; }));
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
                    DrawEntries(ss);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectIconString>(out var sis) && sis.IsAddonReady)
                    DrawEntries(sis);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.ContextMenu>(out var cm) && cm.IsAddonReady)
                    DrawEntries(cm);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectYesno>(out var yn) && yn.IsAddonReady)
                    DrawEntries(yn);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectOk>(out var ok) && ok.IsAddonReady)
                    DrawEntries(ok);
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

        private void DrawEntries(AddonMaster.SelectString am)
        {
            //Svc.Chat.Print($"{addonSelectStringBase->X * addonSelectStringBase->Scale}, {addonSelectStringBase->Y * addonSelectStringBase->Scale}");
            if (am.Base->UldManager.NodeListCount < 3) return;
            var listNode = am.Base->UldManager.NodeList[2];
            var textNode = (AtkTextNode*)am.Base->UldManager.NodeList[3];
            //Svc.Chat.Print($"{listNode->X}, {listNode->Y}");
            for (ushort i = 0; i < Math.Min(am.Addon->PopupMenu.PopupMenu.EntryCount, 12); i++)
            {
                CheckKeyState(i, () => clickMgr.ClickItemThrottled(() => am.Entries[i].Select(), textNode->NodeText.ToString()));
                //Svc.Chat.Print(Marshal.PtrToStringUTF8((IntPtr)addonSelectString->PopupMenu.EntryNames[i]));
                var listComponent = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                var itemText = listComponent->GetComponent()->UldManager.NodeList[3]->GetAsAtkTextNode()->AtkResNode;
                //Svc.Chat.Print($"{listComponent->X}, {listComponent->Y}");
                DrawKey(i, &itemText);
            }
        }

        private void DrawEntries(AddonMaster.SelectIconString am)
        {
            if (am.Base->UldManager.NodeListCount < 3) return;
            var listNode = am.Base->UldManager.NodeList[2];
            var textNode = (AtkTextNode*)am.Base->UldManager.NodeList[3];
            for (ushort i = 0; i < Math.Min(am.Addon->PopupMenu.PopupMenu.EntryCount, 12); i++)
            {
                CheckKeyState(i, () => clickMgr.ClickItemThrottled(() => am.Entries[i].Select(), textNode->NodeText.ToString()));
                var listComponent = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                var itemText = listComponent->GetComponent()->UldManager.NodeList[3]->GetAsAtkTextNode()->AtkResNode;
                DrawKey(i, &itemText);
            }
        }

        private void DrawEntries(AddonMaster.ContextMenu am)
        {
            if (am.Base->AtkValuesCount <= 7) return;
            var listNode = am.Base->UldManager.NodeList[2];
            for (ushort i = 0; i < Math.Min(am.Entries.Length, 12); i++)
            {
                var entry = am.Entries[i];
                if (!entry.IsNativeEntry) continue;

                CheckKeyState(i, () => clickMgr.ClickItemThrottled(() => am.Entries[i].Select(), entry.Text));
                var listComponent = ((AtkComponentNode*)listNode)->Component->UldManager.NodeList[i + 1];
                var itemText = listComponent->GetComponent()->UldManager.NodeList[6]->GetAsAtkTextNode()->AtkResNode;
                DrawKey(i, &itemText);
            }
        }

        private void DrawEntries(AddonMaster.SelectYesno am)
        {
            CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Yes(), am.Text + "Yes"));
            DrawKey(0, am.Addon->YesButton->UldManager.NodeList[0]);
            CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.No(), am.Text + "No"));
            DrawKey(1, am.Addon->NoButton->UldManager.NodeList[0]);
            // TODO: there's a third button sometimes (usually a wait)
        }

        private void DrawEntries(AddonMaster.SelectOk am)
        {
            CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Ok(), am.Text + "Ok"));
            DrawKey(0, am.Addon->OkButton->AtkResNode);
            // TODO: there's a second button sometimes (usually a cancel)
        }

        private static void CheckKeyState(ushort i, Action clickAction)
        {
            if (i < 10)
            {
                var state = Svc.KeyState.GetRawValue(49 + (i == 9 ? -1 : i));
                if (state == 3)
                {
                    Svc.KeyState.SetRawValue(49 + (i == 9 ? -1 : i), 0);
                    clickAction();
                }
                state = Svc.KeyState.GetRawValue(97 + (i == 9 ? -1 : i));
                if (state == 3)
                {
                    Svc.KeyState.SetRawValue(97 + (i == 9 ? -1 : i), 0);
                    clickAction();
                }
            }
            else
            {
                var state = Svc.KeyState.GetRawValue(189 + (i == 10 ? 0 : -2));
                if (state == 3)
                {
                    Svc.KeyState.SetRawValue(189 + (i == 10 ? 0 : -2), 0);
                    clickAction();
                }
            }
        }

        private void DrawKey(int idx, AtkResNode* node)
        {
            var pos = GetNodePosition(node);
            // only offset if you passed a text node, otherwise it looks bad
            DrawList.Add((node->GetAsAtkTextNode() == null ? pos.X : pos.X - horizontalOffset, pos.Y + node->Height / 2, $"{(idx == 11 ? "=" : (idx == 10 ? "-" : (idx == 9 ? 0 : idx + 1)))}"));
        }

        private static Vector2 GetNodePosition(AtkResNode* node)
        {
            var pos = new Vector2(node->X, node->Y);
            pos -= new Vector2(node->OriginX * (node->ScaleX - 1), node->OriginY * (node->ScaleY - 1));
            var par = node->ParentNode;
            while (par != null)
            {
                pos *= new Vector2(par->ScaleX, par->ScaleY);
                pos += new Vector2(par->X, par->Y);
                pos -= new Vector2(par->OriginX * (par->ScaleX - 1), par->OriginY * (par->ScaleY - 1));
                par = par->ParentNode;
            }

            return pos;
        }
    }
}
