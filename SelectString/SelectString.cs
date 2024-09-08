using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
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
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectString>(out var ss) && ss.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(ss);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectIconString>(out var sis) && sis.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(sis);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.ContextMenu>(out var cm) && cm.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(cm);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectYesno>(out var yn) && yn.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(yn);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.SelectOk>(out var ok) && ok.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(ok);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.JournalDetail>(out var jd) && jd.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(jd);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.DifficultySelectYesNo>(out var dyn) && dyn.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(dyn);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.Request>(out var rq) && rq.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(rq);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && lfg.IsAddonReady && lfg.IsAddonHighestFocus)
                    DrawEntries(lfg);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var lfgc) && lfgc.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(lfgc);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.AirShipExplorationResult>(out var aser) && aser.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(aser);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.CompanyCraftSupply>(out var ccs) && ccs.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(ccs);
                if (GenericHelpers.TryGetAddonMaster<AddonMaster.CollectablesShop>(out var cs) && cs.IsAddonReady && ss.IsAddonHighestFocus)
                    DrawEntries(cs);

                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("FrontlineRecord", out var fl) && GenericHelpers.IsAddonReady(fl))
                    DrawEntries(fl, fl->UldManager.NodeList[3]->GetAsAtkComponentButton(), -1);
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("MKSRecord", out var cc) && GenericHelpers.IsAddonReady(cc))
                    DrawEntries(cc, cc->UldManager.NodeList[4]->GetAsAtkComponentButton(), -1);
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("DawnStory", out var ds) && GenericHelpers.IsAddonReady(ds))
                    DrawEntries(ds, ds->UldManager.NodeList[84]->GetAsAtkComponentButton(), 14);
            }
            catch (Exception e)
            {
                Svc.Log.Error(e.Message + "\n" + e.StackTrace);
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
                var itemText = listComponent->GetComponent()->UldManager.NodeList[4]->GetAsAtkTextNode()->AtkResNode;
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
            if (ButtonActive(am.Addon->YesButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Yes(), am.Text + "Yes"));
                DrawKey(0, am.Addon->YesButton->UldManager.NodeList[0]);
            }
            if (ButtonActive(am.Addon->NoButton))
            {
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.No(), am.Text + "No"));
                DrawKey(1, am.Addon->NoButton->UldManager.NodeList[0]);
            }
            // TODO: there's a third button sometimes (usually a wait)
        }

        private void DrawEntries(AddonMaster.SelectOk am)
        {
            if (ButtonActive(am.Addon->OkButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Ok(), am.Text + "Ok"));
                DrawKey(0, am.Addon->OkButton->AtkResNode);
            }
            // TODO: there's a second button sometimes (usually a cancel)
        }

        private void DrawEntries(AddonMaster.JournalDetail am)
        {
            if (ButtonActive(am.Addon->InitiateButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Initiate(), "Initiate"));
                DrawKey(0, am.Addon->InitiateButton->AtkResNode);

                if (ButtonActive(am.Addon->AcceptMapButton))
                {
                    CheckKeyState(2, () => clickMgr.ClickItemThrottled(() => am.AcceptMap(), "Map"));
                    DrawKey(2, am.Addon->AcceptMapButton->AtkResNode);
                }
            }
            else
            {
                if (ButtonActive(am.Addon->AcceptMapButton))
                {
                    CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.AcceptMap(), "Accept"));
                    DrawKey(0, am.Addon->AcceptMapButton->AtkResNode);
                }
            }
            if (ButtonActive(am.Addon->AbandonDeclineButton))
            {
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.AbandonDecline(), "Abandon"));
                DrawKey(1, am.Addon->AbandonDeclineButton->AtkResNode);
            }
        }

        private void DrawEntries(AddonMaster.DifficultySelectYesNo am)
        {
            if (ButtonActive(am.ProceedButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Proceed(), "Proceed"));
                DrawKey(0, am.ProceedButton->AtkResNode);
            }
            if (ButtonActive(am.LeaveButton))
            {
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.Leave(), "Leave"));
                DrawKey(1, am.LeaveButton->AtkResNode);
            }
            // TODO: the ClickHelper for radio buttons doesn't actually function

            //if (ButtonActive(am.NormalButton))
            //{
            //    CheckKeyState(2, () => clickMgr.ClickItemThrottled(() => am.SetDifficultyNormal(), "Normal"));
            //    DrawKey(2, am.NormalButton->AtkResNode);
            //}
            //if (ButtonActive(am.EasyButton))
            //{
            //    CheckKeyState(3, () => clickMgr.ClickItemThrottled(() => am.SetDifficultyEasy(), "Easy"));
            //    DrawKey(3, am.EasyButton->AtkResNode);
            //}
            //if (ButtonActive(am.VeryEasyButton))
            //{
            //    CheckKeyState(4, () => clickMgr.ClickItemThrottled(() => am.SetDifficultyVeryEasy(), "VeryEasy"));
            //    DrawKey(4, am.VeryEasyButton->AtkResNode);
            //}
        }

        private void DrawEntries(AddonMaster.Request am)
        {
            if (ButtonActive(am.HandOverButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.HandOver(), "HandOver"));
                DrawKey(0, am.HandOverButton->AtkResNode);
            }
            if (ButtonActive(am.CancelButton))
            {
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.Cancel(), "Cancel"));
                DrawKey(1, am.CancelButton->AtkResNode);
            }
        }

        private void DrawEntries(AddonMaster.LookingForGroup am)
        {
            if (ButtonActive(am.RecruitMembersButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.RecruitMembersOrDetails(), "RecruitMembers"));
                DrawKey(0, am.RecruitMembersButton->AtkResNode);
            }
        }

        private void DrawEntries(AddonMaster.LookingForGroupCondition am)
        {
            if (ButtonActive(am.RecruitButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Recruit(), "Recruit"));
                DrawKey(0, am.RecruitButton->AtkResNode);
            }
            if (ButtonActive(am.CancelButton))
            {
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.Cancel(), "Cancel"));
                DrawKey(1, am.CancelButton->AtkResNode);
            }
            if (ButtonActive(am.ResetButton))
            {
                CheckKeyState(2, () => clickMgr.ClickItemThrottled(() => am.Reset(), "Reset"));
                DrawKey(2, am.ResetButton->AtkResNode);
            }
        }

        private void DrawEntries(AddonMaster.AirShipExplorationResult am)
        {
            if (ButtonActive(am.RedeployButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Redeploy(), "Redeploy"));
                DrawKey(0, am.RedeployButton->AtkResNode);
            }
            if (ButtonActive(am.FinalizeReportButton))
            {
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(() => am.FinalizeReport(), "FinalizeReport"));
                DrawKey(1, am.FinalizeReportButton->AtkResNode);
            }
        }

        private void DrawEntries(AddonMaster.CompanyCraftSupply am)
        {
            if (ButtonActive(am.CloseButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Close(), "Close"));
                DrawKey(0, am.CloseButton->AtkResNode);
            }
        }

        private void DrawEntries(AddonMaster.CollectablesShop am)
        {
            if (ButtonActive(am.TradeButton))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => am.Trade(), "Trade"));
                DrawKey(0, am.TradeButton->AtkResNode);
            }
        }

        private void DrawEntries(AtkUnitBase* atk, AtkComponentButton* button, params object[] callback)
        {
            if (ButtonActive(button))
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(() => Callback.Fire(atk, true, callback), button->ButtonTextNode->NodeText.ToString()));
                DrawKey(0, button->AtkResNode);
            }
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
            var text = IndexToText(idx);
            // only offset if you passed a text node, otherwise it looks bad
            // check whether an element with the same text already exists, and if it does, replace it with the new
            DrawList.RemoveAll(x => x.Text == text);
            DrawList.Add((node->GetAsAtkTextNode() == null ? pos.X : pos.X - horizontalOffset, pos.Y + node->Height / 2, text));
        }

        private static string IndexToText(int idx) => $"{(idx == 11 ? "=" : (idx == 10 ? "-" : (idx == 9 ? 0 : idx + 1)))}";

        private static bool ButtonActive(AtkComponentButton* btn)
            => btn != null && btn->IsEnabled && btn->OwnerNode->NodeFlags.HasFlag(NodeFlags.Visible);

        private static bool ButtonActive(AtkComponentRadioButton* btn)
            => btn != null && btn->IsEnabled && btn->OwnerNode->NodeFlags.HasFlag(NodeFlags.Visible);

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
