using Dalamud.Game.Command;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Singletons;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SelectString.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ECommons.GenericHelpers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

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
            SingletonServiceManager.Initialize(typeof(ServiceManager));
        }

        public void Dispose()
        {
            Svc.Framework.Update -= Tick;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            Svc.Commands.RemoveHandler("/ss");
            ECommonsMain.Dispose();
        }

        private class Button(AtkComponentButton* btn, Action buttonAction)
        {
            public AtkComponentButton* Base => btn;
            public bool Active => ButtonActive(btn);
            public Action ClickAction => buttonAction;
            public string Id => buttonAction.Method.Name;
        }

        private void Tick(object framework)
        {
            /*if (!exec) return;
            exec = false;*/
            DrawList.Clear();
            try
            {
                // requires special handling
                if (TryGetAddonMaster<AddonMaster.SelectString>(out var ss) && ss.IsAddonReady && (ss.HasFocus || ss.IsAddonOnlyFocus))
                    DrawEntries(ss);
                if (TryGetAddonMaster<SelectIconString>(out var sis) && sis.IsAddonReady && sis.HasFocus)
                    DrawEntries(sis);
                if (TryGetAddonMaster<ContextMenu>(out var cm) && cm.IsAddonReady && cm.HasFocus)
                    DrawEntries(cm);
                if (TryGetAddonMaster<JournalDetail>(out var jd) && jd.IsAddonReady) // this addon is never highest focus unless manually clicked upon
                    DrawEntries(jd);
                if (TryGetAddonMaster<RetainerList>(out var rl) && rl.IsAddonReady && rl.HasFocus)
                    DrawEntries(rl);
                if (TryGetAddonMaster<GcArmyMenberProfile>(out var gcp) && gcp.IsAddonReady && gcp.HasFocus)
                    DrawEntries(gcp);

                // generic button addons
                if (TryGetAddonMaster<AirShipExploration>(out var ase) && ase.IsAddonReady && ase.HasFocus)
                    DrawEntries([new(ase.DeployButton, ase.Deploy)]);
                if (TryGetAddonMaster<AirShipExplorationResult>(out var aser) && aser.IsAddonReady && aser.HasFocus)
                    DrawEntries([new(aser.RedeployButton, aser.Redeploy), new(aser.FinalizeReportButton, aser.FinalizeReport)]);
                if (TryGetAddonMaster<CollectablesShop>(out var cs) && cs.IsAddonReady && cs.HasFocus)
                    DrawEntries([new(cs.TradeButton, cs.Trade)]);
                if (TryGetAddonMaster<CompanyCraftRecipeNoteBook>(out var ccrn) && ccrn.IsAddonReady && ccrn.HasFocus)
                    DrawEntries([new(ccrn.BeginButton, ccrn.Begin)]);
                if (TryGetAddonMaster<CompanyCraftSupply>(out var ccs) && ccs.IsAddonReady && ccs.HasFocus)
                    DrawEntries([new(ccs.CloseButton, ccs.Close)]);
                if (TryGetAddonMaster<DifficultySelectYesNo>(out var dyn) && dyn.IsAddonReady && dyn.HasFocus)
                    DrawEntries([new(dyn.ProceedButton, dyn.Proceed), new(dyn.LeaveButton, dyn.Leave)]); // TODO: the radio buttons
                if (TryGetAddonMaster<GcArmyChangeClass>(out var gcac) && gcac.IsAddonReady && gcac.HasFocus)
                    DrawEntries([
                        new(gcac.GladiatorButton, gcac.Gladiator), new(gcac.MarauderButton, gcac.Marauder),
                        new(gcac.PugilistButton, gcac.Pugilist), new(gcac.LancerButton, gcac.Lancer), new(gcac.RogueButton, gcac.Rogue), new(gcac.ArcherButton, gcac.Archer),
                        new(gcac.ConjurerButton, gcac.Conjurer), new(gcac.ThaumaturgeButton, gcac.Thaumaturge), new(gcac.ArcanistButton, gcac.Arcanist)]);
                if (TryGetAddonMaster<GcArmyExpedition>(out var gae) && gae.IsAddonReady && gae.HasFocus)
                    DrawEntries([new(gae.Addon->DeployButton, gae.Deploy)]);
                if (TryGetAddonMaster<GcArmyExpeditionResult>(out var gaer) && gaer.IsAddonReady && gaer.HasFocus)
                    DrawEntries([new(gaer.Addon->CompleteButton, gaer.Complete)]);
                if (TryGetAddonMaster<GcArmyTraining>(out var gat) && gat.IsAddonReady && gat.HasFocus)
                    DrawEntries([new(gat.CloseButton, gat.Close)]);
                if (TryGetAddonMaster<GearSetList>(out var gsl) && gsl.IsAddonReady && gsl.HasFocus)
                    DrawEntries([new(gsl.EquipSetButton, gsl.EquipSet)]);
                if (TryGetAddonMaster<ItemInspectionResult>(out var iir) && iir.IsAddonReady && iir.HasFocus)
                    DrawEntries([new(iir.NextButton, iir.Next), new(iir.CloseButton, iir.Close)]);
                if (TryGetAddonMaster<JournalResult>(out var jr) && jr.IsAddonReady && jr.HasFocus)
                    DrawEntries([new(jr.Addon->CompleteButton, jr.Complete), new(jr.Addon->DeclineButton, jr.Decline)]);
                if (TryGetAddonMaster<LetterList>(out var ll) && ll.IsAddonReady && ll.HasFocus)
                    DrawEntries([new(ll.NewButton, ll.New), new(ll.SentLetterHistoryButton, ll.SentLetterHistory), new(ll.DeliveryRequestButton, ll.DeliveryRequest)]);
                if (TryGetAddonMaster<LetterViewer>(out var lv) && lv.IsAddonReady && lv.HasFocus)
                    DrawEntries([new(lv.TakeAllButton, lv.TakeAll), new(lv.ReplyButton, lv.Reply), new(lv.DeleteButton, lv.Delete)]);
                if (TryGetAddonMaster<LookingForGroup>(out var lfg) && lfg.IsAddonReady && lfg.HasFocus)
                    DrawEntries([new(lfg.RecruitMembersButton, () => lfg.RecruitMembersOrDetails())]);
                if (TryGetAddonMaster<LookingForGroupCondition>(out var lfgc) && lfgc.IsAddonReady && lfgc.HasFocus)
                    DrawEntries([new(lfgc.RecruitButton, () => lfgc.Recruit()), new(lfgc.CancelButton, () => lfgc.Cancel()), new(lfgc.ResetButton, () => lfgc.Reset())]);
                if (TryGetAddonMaster<LookingForGroupDetail>(out var lfgd) && lfgd.IsAddonReady && lfgd.HasFocus)
                    DrawEntries([new(lfgd.JoinEditButton, () => lfgd.JoinEdit()), new(lfgd.TellEndButton, () => lfgd.TellEnd()), new(lfgd.BackButton, () => lfgd.Back())]);
                if (TryGetAddonMaster<LookingForGroupPrivate>(out var lfgp) && lfgp.IsAddonReady && lfgp.HasFocus)
                    DrawEntries([new(lfgp.JoinButton, lfgp.Join), new(lfgp.CancelButton, lfgp.Cancel)]);
                if (TryGetAddonMaster<LotteryWeeklyInput>(out var lwi) && lwi.IsAddonReady && lwi.HasFocus)
                    DrawEntries([new(lwi.PurchaseButton, lwi.Purchase), new(lwi.RandomButton, lwi.Random)]);
                if (TryGetAddonMaster<LotteryWeeklyRewardList>(out var lwrl) && lwrl.IsAddonReady && lwrl.HasFocus)
                    DrawEntries([new(lwrl.CloseButton, lwrl.Close)]);
                if (TryGetAddonMaster<MateriaAttachDialog>(out var mad) && mad.IsAddonReady && mad.HasFocus)
                    DrawEntries([new(mad.MeldButton, mad.Meld), new(mad.ReturnButton, mad.Return)]);
                if (TryGetAddonMaster<MaterializeDialog>(out var md) && md.IsAddonReady && md.HasFocus)
                    DrawEntries([new(md.Addon->YesButton, md.Materialize), new(md.Addon->NoButton, md.No)]);
                if (TryGetAddonMaster<MateriaRetrieveDialog>(out var mrd) && mrd.IsAddonReady && mrd.HasFocus)
                    DrawEntries([new(mrd.BeginButton, mrd.Begin), new(mrd.ReturnButton, mrd.Return)]);
                if (TryGetAddonMaster<MiragePrismExecute>(out var mpe) && mpe.IsAddonReady && mpe.HasFocus)
                    DrawEntries([new(mpe.CastButton, mpe.Cast), new(mpe.ReturnButton, mpe.Return)]);
                if (TryGetAddonMaster<MiragePrismRemove>(out var mpr) && mpr.IsAddonReady && mpr.HasFocus)
                    DrawEntries([new(mpr.DispelButton, mpr.Dispel), new(mpr.ReturnButton, mpr.Return)]);
                if (TryGetAddonMaster<PurifyAutoDialog>(out var pad) && pad.IsAddonReady && pad.HasFocus)
                    DrawEntries([new(pad.CancelExitButton, pad.CancelExit)]);
                if (TryGetAddonMaster<PurifyResult>(out var pr) && pr.IsAddonReady && pr.HasFocus)
                    DrawEntries([new(pr.AutomaticButton, pr.Automatic), new(pr.CloseButton, pr.Close)]);
                if (TryGetAddonMaster<RecipeNote>(out var rn) && rn.IsAddonReady && rn.HasFocus)
                    DrawEntries([new(rn.Addon->SynthesizeButton, rn.Synthesize), new(rn.Addon->QuickSynthesisButton, rn.QuickSynthesis), new(rn.Addon->TrialSynthesisButton, rn.TrialSynthesis)]);
                if (TryGetAddonMaster<RecommendEquip>(out var re) && re.IsAddonReady && re.HasFocus)
                    DrawEntries([new(re.EquipButton, re.Equip), new(re.CancelButton, re.Cancel)]);
                if (TryGetAddonMaster<Repair>(out var rp) && rp.IsAddonReady && rp.HasFocus)
                    DrawEntries([new(rp.Addon->RepairAllButton, rp.RepairAll)]);
                if (TryGetAddonMaster<Request>(out var rq) && rq.IsAddonReady && rq.HasFocus)
                    DrawEntries([new(rq.HandOverButton, rq.HandOver), new(rq.CancelButton, rq.Cancel)]);
                if (TryGetAddonMaster<RetainerItemTransferList>(out var ritl) && ritl.IsAddonReady && ritl.HasFocus)
                    DrawEntries([new(ritl.Addon->ConfirmButton, ritl.Confirm), new(ritl.Addon->CancelButton, ritl.Cancel)]);
                if (TryGetAddonMaster<RetainerItemTransferProgress>(out var ritp) && ritp.IsAddonReady && ritp.HasFocus)
                    DrawEntries([new(ritp.Addon->CloseWindowButton, ritp.Close)]);
                if (TryGetAddonMaster<RetainerSell>(out var rs) && rs.IsAddonReady && rs.HasFocus)
                    DrawEntries([new(rs.ConfirmButton, rs.Confirm), new(rs.CancelButton, rs.Cancel), new(rs.ComparePricesButton, rs.ComparePrices)]);
                if (TryGetAddonMaster<RetainerTaskAsk>(out var rta) && rta.IsAddonReady && rta.HasFocus)
                    DrawEntries([new(rta.AssignButton, rta.Assign), new(rta.ReturnButton, rta.Return)]);
                if (TryGetAddonMaster<RetainerTaskResult>(out var rtr) && rtr.IsAddonReady && rtr.HasFocus)
                    DrawEntries([new(rtr.ReassignButton, rtr.Reassign), new(rtr.ConfirmButton, rtr.Confirm)]);
                if (TryGetAddonMaster<ReturnerDialog>(out var rd) && rd.IsAddonReady && rd.HasFocus)
                    DrawEntries([new(rd.AcceptButton, rd.Accept), new(rd.DeclineButton, rd.Decline), new(rd.DecideLaterButton, rd.DecideLater)]);
                if (TryGetAddonMaster<SalvageAutoDialog>(out var sad) && sad.IsAddonReady && sad.HasFocus)
                    DrawEntries([new(sad.EndDesynthesisButton, sad.EndDesynthesis)]);
                if (TryGetAddonMaster<SalvageResult>(out var sr) && sr.IsAddonReady && sr.HasFocus)
                    DrawEntries([new(sr.CloseButton, sr.Close)]);
                if (TryGetAddonMaster<SelectYesno>(out var yn) && yn.IsAddonReady && yn.HasFocus)
                    DrawEntries([new(yn.Addon->YesButton, yn.Yes), new(yn.Addon->NoButton, yn.No)]); // TODO: these have a third button?
                if (TryGetAddonMaster<SelectOk>(out var ok) && ok.IsAddonReady && ok.HasFocus)
                    DrawEntries([new(ok.Addon->OkButton, ok.Ok)]); // TODO: these have a second button?
                if (TryGetAddonMaster<ShopCardDialog>(out var scd) && scd.IsAddonReady && scd.HasFocus)
                    DrawEntries([new(scd.SellButton, scd.Sell), new(scd.CancelButton, scd.Cancel)]);
                if (TryGetAddonMaster<TripleTriadRequest>(out var ttr) && ttr.IsAddonReady && ttr.HasFocus)
                    DrawEntries([new(ttr.ChallengeButton, ttr.Challenge), new(ttr.QuitButton, ttr.Quit)]);
                if (TryGetAddonMaster<TripleTriadResult>(out var ttrr) && ttrr.IsAddonReady && ttrr.HasFocus)
                    DrawEntries([new(ttrr.RematchButton, ttrr.Rematch), new(ttrr.QuitButton, ttrr.Quit)]);
                if (TryGetAddonMaster<WeeklyBingoResult>(out var wbr) && wbr.IsAddonReady && wbr.HasFocus)
                    DrawEntries([new(wbr.AcceptButton, wbr.Accept), new(wbr.CancelButton, wbr.Cancel)]);

                // generic callback addons
                if (TryGetAddonByName<AtkUnitBase>("FrontlineRecord", out var fl) && IsAddonReady(fl))
                    DrawEntries(fl, fl->UldManager.NodeList[3]->GetAsAtkComponentButton(), -1);
                if (TryGetAddonByName<AtkUnitBase>("MKSRecord", out var cc) && IsAddonReady(cc))
                    DrawEntries(cc, cc->UldManager.NodeList[4]->GetAsAtkComponentButton(), -1);
                if (TryGetAddonByName<AtkUnitBase>("DawnStory", out var ds) && IsAddonReady(ds))
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

        #region Generic Draws
        private void DrawEntries(List<Button> btns)
        {
            if (RaptureAtkModule.Instance()->AtkModule.IsTextInputActive()) return;
            foreach (var btn in btns)
            {
                if (btn.Active)
                {
                    CheckKeyState(btns.IndexOf(btn), () => clickMgr.ClickItemThrottled(btn.ClickAction, btn.Id));
                    DrawKey(btns.IndexOf(btn), btn.Base->AtkResNode);
                }
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
        #endregion


        #region Custom Draws
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

        private void DrawEntries(SelectIconString am)
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

        private void DrawEntries(ContextMenu am)
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

        private void DrawEntries(JournalDetail am)
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

        private void DrawEntries(RetainerList am)
        {
            var list = am.Addon->GetComponentListById(27);
            foreach (var node in Enumerable.Range(0, list->GetItemCount()))
            {
                var item = list->GetItemRenderer(node);
                if (item == null)
                    continue;

                CheckKeyState(node, () => clickMgr.ClickItemThrottled(() => am.Retainers[node].Select(), am.Retainers[node].Name));
                DrawKey(node, item->AtkResNode);
            }
        }

        private void DrawEntries(GcArmyMenberProfile am)
        {
            if (am.Addon->AtkValues[18].Bool) // afaik this bool indicates whether the buttons are shown or the radio buttons. Could also be #23 but the value is inverted.
            {
                CheckKeyState(0, () => clickMgr.ClickItemThrottled(am.Question, am.QuestionButton->ButtonTextNode->NodeText.ToString()));
                DrawKey(0, am.QuestionButton->AtkResNode);
                CheckKeyState(1, () => clickMgr.ClickItemThrottled(am.Postpone, am.PostponeButton->ButtonTextNode->NodeText.ToString()));
                DrawKey(1, am.PostponeButton->AtkResNode);
                CheckKeyState(2, () => clickMgr.ClickItemThrottled(am.Dismiss, am.DismissButton->ButtonTextNode->NodeText.ToString()));
                DrawKey(2, am.DismissButton->AtkResNode);
            }
            //else
            //{
            //    // TODO: get radio buttons working
            //    // Note: DisplayOrders is sometimes not visible at all, considering shifting all the numbers accordingly
            //    CheckKeyState(0, () => clickMgr.ClickItemThrottled(am.DisplayOrders, am.DisplayOrdersButton->ButtonTextNode->NodeText.ToString()));
            //    DrawKey(0, am.DisplayOrdersButton->AtkResNode);
            //    CheckKeyState(1, () => clickMgr.ClickItemThrottled(am.ChangeClass, am.ChangeClassButton->ButtonTextNode->NodeText.ToString()));
            //    DrawKey(1, am.ChangeClassButton->AtkResNode);
            //    CheckKeyState(2, () => clickMgr.ClickItemThrottled(am.ConfirmChemistry, am.ConfirmChemistryButton->ButtonTextNode->NodeText.ToString()));
            //    DrawKey(2, am.ConfirmChemistryButton->AtkResNode);
            //    CheckKeyState(3, () => clickMgr.ClickItemThrottled(am.Outfit, am.OutfitButton->ButtonTextNode->NodeText.ToString()));
            //    DrawKey(3, am.OutfitButton->AtkResNode);
            //}
        }
        #endregion

        private static void CheckKeyState(int i, Action clickAction) => CheckKeyState((ushort)i, clickAction);
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
