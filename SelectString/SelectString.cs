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
using FFXIVClientStructs.Interop;
using ImGuiNET;
using SelectString.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ECommons.Automation.UIInput.ClickHelperExtensions;
using static ECommons.GenericHelpers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace SelectString
{
    public unsafe class SelectString : IDalamudPlugin
    {
        public string Name => "SelectString";
        bool exec = false;
        static List<(float X, float Y, string Text)> DrawList = [];
        Clicker clickMgr;
        FocusWatcher focusWatcher;
        KeyStateWatcher keyWatcher;
        List<Button> ActiveButtons = [];
        const int horizontalOffset = 10;

        public SelectString(IDalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this);
            clickMgr = new Clicker();
            keyWatcher = new KeyStateWatcher();
            Svc.Framework.Update += Tick;
            KeyStateWatcher.NumKeyPressed += OnNumKeyPress;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.Commands.AddHandler("/ss", new CommandInfo(delegate { exec ^= true; }));
            SingletonServiceManager.Initialize(typeof(ServiceManager));
        }

        public void Dispose()
        {
            Svc.Framework.Update -= Tick;
            KeyStateWatcher.NumKeyPressed -= OnNumKeyPress;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            Svc.Commands.RemoveHandler("/ss");
            keyWatcher.Dispose();
            ECommonsMain.Dispose();
        }

        private class Button(AtkComponentButton* btn, Action buttonAction = null)
        {
            public AtkComponentButton* Base => btn;
            public bool Active => ButtonActive(btn);
            public Action Callback => buttonAction;
            public string Id => btn->ButtonTextNode->NodeText.ToString();

            public void DrawKey(int idx)
            {
                var pos = GetNodePosition(btn->AtkResNode);
                var text = IndexToText(idx);
                // check whether an element with the same text already exists, and if it does, replace it with the new
                DrawList.RemoveAll(x => x.Text == text);
                // only offset if you passed a text node, otherwise it looks bad
                DrawList.Add((btn->AtkResNode->GetAsAtkTextNode() == null ? pos.X : pos.X - horizontalOffset, pos.Y + btn->AtkResNode->Height / 2, text));
            }

            public bool Click()
            {
                if (btn->IsEnabled && btn->AtkResNode->IsVisible())
                {
                    btn->ClickAddonButton(GetFocusedAddonFromNode(btn->AtkResNode));
                    return true;
                }
                return false;
            }
        }

        private void OnNumKeyPress(int idx)
        {
            Svc.Log.Info($"{idx} pressed");
            if (ActiveButtons.Count == 0 || ActiveButtons.Count < idx) return;
            ActiveButtons[idx].Click();
        }

        private void Tick(object framework)
        {
            /*if (!exec) return;
            exec = false;*/
            DrawList.Clear();
            ActiveButtons.Clear();
            keyWatcher.Enabled = false;
            try
            {
                var atk = GetFocusedAddon();
                if (atk == null || !IsAddonReady(atk)) return;
                // requires special handling
                //if (TryGetAddonMaster<AddonMaster.SelectString>(atk->NameString, out var ss))
                //    DrawEntries(ss);
                //if (TryGetAddonMaster<SelectIconString>(atk->NameString, out var sis))
                //    DrawEntries(sis);
                //if (TryGetAddonMaster<ContextMenu>(atk->NameString, out var cm))
                //    DrawEntries(cm);
                //if (TryGetAddonMaster<JournalDetail>(atk->NameString, out var jd)) // this addon is never highest focus unless manually clicked on
                //    DrawEntries(jd);
                //if (TryGetAddonMaster<RetainerList>(atk->NameString, out var rl))
                //    DrawEntries(rl);
                //if (TryGetAddonMaster<GcArmyMenberProfile>(atk->NameString, out var gcp))
                //    DrawEntries(gcp);

                // generic button addons
                if (TryGetAddonMasterIfFocused<AirShipExploration>(atk, out var ase))
                    DrawEntries(ase.DeployButton);
                if (TryGetAddonMasterIfFocused<AirShipExplorationResult>(atk, out var aser))
                    DrawEntries([aser.RedeployButton, aser.FinalizeReportButton]);
                if (TryGetAddonMasterIfFocused<CollectablesShop>(atk, out var cs))
                    DrawEntries(cs.TradeButton);
                if (TryGetAddonMasterIfFocused<CompanyCraftRecipeNoteBook>(atk, out var ccrn))
                    DrawEntries(ccrn.BeginButton);
                if (TryGetAddonMasterIfFocused<CompanyCraftSupply>(atk, out var ccs))
                    DrawEntries(ccs.CloseButton);
                if (TryGetAddonMasterIfFocused<DifficultySelectYesNo>(atk, out var dyn))
                    DrawEntries([dyn.ProceedButton, dyn.LeaveButton]); // TODO: the radio buttons
                if (TryGetAddonMasterIfFocused<GcArmyChangeClass>(atk, out var gcac))
                    DrawEntries([gcac.GladiatorButton, gcac.MarauderButton, gcac.PugilistButton, gcac.LancerButton, gcac.RogueButton, gcac.ArcherButton, gcac.ConjurerButton, gcac.ThaumaturgeButton, gcac.ArcanistButton]);
                if (TryGetAddonMasterIfFocused<GcArmyExpedition>(atk, out var gae))
                    DrawEntries(gae.Addon->DeployButton);
                if (TryGetAddonMasterIfFocused<GcArmyExpeditionResult>(atk, out var gaer))
                    DrawEntries(gaer.Addon->CompleteButton);
                if (TryGetAddonMasterIfFocused<GcArmyTraining>(atk, out var gat))
                    DrawEntries(gat.CloseButton);
                if (TryGetAddonMasterIfFocused<GearSetList>(atk, out var gsl))
                    DrawEntries(gsl.EquipSetButton);
                if (TryGetAddonMasterIfFocused<ItemInspectionResult>(atk, out var iir))
                    DrawEntries([iir.NextButton, iir.CloseButton]);
                if (TryGetAddonMasterIfFocused<JournalResult>(atk, out var jr))
                    DrawEntries([jr.Addon->CompleteButton, jr.Addon->DeclineButton]);
                if (TryGetAddonMasterIfFocused<LetterList>(atk, out var ll))
                    DrawEntries([ll.NewButton, ll.SentLetterHistoryButton, ll.DeliveryRequestButton]);
                if (TryGetAddonMasterIfFocused<LetterViewer>(atk, out var lv))
                    DrawEntries([lv.TakeAllButton, lv.ReplyButton, lv.DeleteButton]);
                if (TryGetAddonMasterIfFocused<LookingForGroup>(atk, out var lfg))
                    DrawEntries(lfg.RecruitMembersButton);
                if (TryGetAddonMasterIfFocused<LookingForGroupCondition>(atk, out var lfgc))
                    DrawEntries([lfgc.RecruitButton, lfgc.CancelButton, lfgc.ResetButton]);
                if (TryGetAddonMasterIfFocused<LookingForGroupDetail>(atk, out var lfgd))
                    DrawEntries([lfgd.JoinEditButton, lfgd.TellEndButton, lfgd.BackButton]);
                if (TryGetAddonMasterIfFocused<LookingForGroupPrivate>(atk, out var lfgp))
                    DrawEntries([lfgp.JoinButton, lfgp.CancelButton]);
                if (TryGetAddonMasterIfFocused<LotteryWeeklyInput>(atk, out var lwi))
                    DrawEntries([lwi.PurchaseButton, lwi.RandomButton]);
                if (TryGetAddonMasterIfFocused<LotteryWeeklyRewardList>(atk, out var lwrl))
                    DrawEntries(lwrl.CloseButton);
                if (TryGetAddonMasterIfFocused<MateriaAttachDialog>(atk, out var mad))
                    DrawEntries([mad.MeldButton, mad.ReturnButton]);
                if (TryGetAddonMasterIfFocused<MaterializeDialog>(atk, out var md))
                    DrawEntries([md.Addon->YesButton, md.Addon->NoButton]);
                if (TryGetAddonMasterIfFocused<MateriaRetrieveDialog>(atk, out var mrd))
                    DrawEntries([mrd.BeginButton, mrd.ReturnButton]);
                if (TryGetAddonMasterIfFocused<MiragePrismExecute>(atk, out var mpe))
                    DrawEntries([mpe.CastButton, mpe.ReturnButton]);
                if (TryGetAddonMasterIfFocused<MiragePrismRemove>(atk, out var mpr))
                    DrawEntries([mpr.DispelButton, mpr.ReturnButton]);
                if (TryGetAddonMasterIfFocused<PurifyAutoDialog>(atk, out var pad))
                    DrawEntries(pad.CancelExitButton);
                if (TryGetAddonMasterIfFocused<PurifyResult>(atk, out var pr))
                    DrawEntries([pr.AutomaticButton, pr.CloseButton]);
                if (TryGetAddonMasterIfFocused<RecipeNote>(atk, out var rn))
                    DrawEntries([rn.Addon->SynthesizeButton, rn.Addon->QuickSynthesisButton, rn.Addon->TrialSynthesisButton]);
                if (TryGetAddonMasterIfFocused<RecommendEquip>(atk, out var re))
                    DrawEntries([re.EquipButton, re.CancelButton]);
                if (TryGetAddonMasterIfFocused<Repair>(atk, out var rp))
                    DrawEntries(rp.Addon->RepairAllButton);
                if (TryGetAddonMasterIfFocused<Request>(atk, out var rq))
                    DrawEntries([rq.HandOverButton, rq.CancelButton]);
                if (TryGetAddonMasterIfFocused<RetainerItemTransferList>(atk, out var ritl))
                    DrawEntries([ritl.Addon->ConfirmButton, ritl.Addon->CancelButton]);
                if (TryGetAddonMasterIfFocused<RetainerItemTransferProgress>(atk, out var ritp))
                    DrawEntries(ritp.Addon->CloseWindowButton);
                if (TryGetAddonMasterIfFocused<RetainerSell>(atk, out var rs))
                    DrawEntries([rs.ConfirmButton, rs.CancelButton, rs.ComparePricesButton]);
                if (TryGetAddonMasterIfFocused<RetainerTaskAsk>(atk, out var rta))
                    DrawEntries([rta.AssignButton, rta.ReturnButton]);
                if (TryGetAddonMasterIfFocused<RetainerTaskResult>(atk, out var rtr))
                    DrawEntries([rtr.ReassignButton, rtr.ConfirmButton]);
                if (TryGetAddonMasterIfFocused<ReturnerDialog>(atk, out var rd))
                    DrawEntries([rd.AcceptButton, rd.DeclineButton, rd.DecideLaterButton]);
                if (TryGetAddonMasterIfFocused<SalvageAutoDialog>(atk, out var sad))
                    DrawEntries(sad.EndDesynthesisButton);
                if (TryGetAddonMasterIfFocused<SalvageResult>(atk, out var sr))
                    DrawEntries(sr.CloseButton);
                if (TryGetAddonMasterIfFocused<SelectYesno>(atk, out var yn))
                    DrawEntries([yn.Addon->YesButton, yn.Addon->NoButton]); // TODO: if the third button is present, no becomes wait and the third becomes no
                if (TryGetAddonMasterIfFocused<SelectOk>(atk, out var ok))
                    DrawEntries(ok.Addon->OkButton); // TODO: these have a second button?
                if (TryGetAddonMasterIfFocused<ShopCardDialog>(atk, out var scd))
                    DrawEntries([scd.SellButton, scd.CancelButton]);
                if (TryGetAddonMasterIfFocused<TripleTriadRequest>(atk, out var ttr))
                    DrawEntries([ttr.ChallengeButton, ttr.QuitButton]);
                if (TryGetAddonMasterIfFocused<TripleTriadResult>(atk, out var ttrr))
                    DrawEntries([ttrr.RematchButton, ttrr.QuitButton]);
                if (TryGetAddonMasterIfFocused<WeeklyBingoResult>(atk, out var wbr))
                    DrawEntries([wbr.AcceptButton, wbr.CancelButton]);

                // generic callback addons
                //if (TryGetAddonByName<AtkUnitBase>("FrontlineRecord", out var fl) && IsAddonReady(fl))
                //    DrawEntries(fl, fl->UldManager.NodeList[3]->GetAsAtkComponentButton(), -1);
                //if (TryGetAddonByName<AtkUnitBase>("MKSRecord", out var cc) && IsAddonReady(cc))
                //    DrawEntries(cc, cc->UldManager.NodeList[4]->GetAsAtkComponentButton(), -1);
                //if (TryGetAddonByName<AtkUnitBase>("DawnStory", out var ds) && IsAddonReady(ds))
                //    DrawEntries(ds, ds->UldManager.NodeList[84]->GetAsAtkComponentButton(), 14);
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
        private void DrawEntries(AtkComponentButton* btn)
        {
            ActiveButtons.Clear();
            keyWatcher.Enabled = true;
            var button = new Button(btn);
            if (button.Active)
            {
                ActiveButtons.Add(new(btn));
                ActiveButtons[0].DrawKey(0);
            }
        }

        private void DrawEntries(List<Pointer<AtkComponentButton>> btns)
        {
            ActiveButtons.Clear();
            keyWatcher.Enabled = true;
            var buttons = new List<Button>();
            btns.ForEach(x => buttons.Add(new(x)));
            foreach (var btn in buttons)
            {
                if (btn.Active)
                {
                    ActiveButtons.Add(btn);
                    btn.DrawKey(buttons.IndexOf(btn));
                }
            }
        }

        private void DrawEntries(AtkUnitBase* atk, AtkComponentButton* btn, params object[] callback)
        {
            ActiveButtons.Clear();
            keyWatcher.Enabled = true;
            var button = new Button(btn, () => Callback.Fire(atk, true, callback));
            if (button.Active)
            {
                ActiveButtons.Add(button);
                ActiveButtons[0].DrawKey(0);
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

        public static bool TryGetAddonMasterIfFocused<T>(AtkUnitBase* atk, out T addonMaster) where T : IAddonMasterBase
        {
            addonMaster = (T)Activator.CreateInstance(typeof(T), [(nint)atk]);
            return addonMaster.Base == atk;
        }

        /// <summary>
        /// Gets the addon that is currently focused OR, if FocusedUnitsList has one entry, that
        /// </summary>
        private static AtkUnitBase* GetFocusedAddon()
        {
            if (RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count == 1)
                return RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[0].Value;

            var focus = AtkStage.Instance()->GetFocus();
            if (focus == null) return null;
            for (var i = 0; i < RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count; i++)
            {
                var atk = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[i].Value;
                if (atk != null && atk->RootNode == GetRootNode(focus))
                    return atk;
            }
            return null;
        }

        private static AtkUnitBase* GetFocusedAddonFromNode(AtkResNode* node)
        {
            for (var i = 0; i < RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count; i++)
            {
                var atk = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[i].Value;
                if (atk != null && atk->RootNode == GetRootNode(node))
                    return atk;
            }
            return null;
        }

        //public static bool AddonMasterFocused<T>(AtkUnitBase* addon, out T addonMaster) where T : IAddonMasterBase
        //{
        //    if (TryGetAddonMaster<T>(out var am) && (am.Base == addon || am.IsAddonOnlyFocus))
        //    {
        //        addonMaster = am;
        //        return true;
        //    }
        //    else
        //    {
        //        addonMaster = default;
        //        return false;
        //    }
        //}

        //public static bool GetFocusedAddonMaster<T>(AtkUnitBase* addon, out T addonMaster) where T : IAddonMasterBase
        //{
        //    addonMaster = (T)Activator.CreateInstance(typeof(T), (nint)addon);
        //    return true;
        //}
    }
}
