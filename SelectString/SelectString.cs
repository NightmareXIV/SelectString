global using static SelectString.SelectString;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Logging;
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

namespace SelectString;

public unsafe class SelectString : IDalamudPlugin
{
    public string Name => "SelectString";
    bool exec = true;
    static List<(float X, float Y, string Text)> DrawList = [];
    KeyStateWatcher keyWatcher;
    TaskManager TM;
    const int horizontalOffset = 10;
    public static Config C;

    public static List<Button> ActiveButtons = [];

    public SelectString(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this);
        keyWatcher = new();
        TM = new();
        C = EzConfig.Init<Config>();
        Svc.Framework.Update += Tick;
        KeyStateWatcher.NumKeyPressed += OnNumKeyPress;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        EzCmd.Add("/ss", OnCommand, $"Toggles {Name}");
        SingletonServiceManager.Initialize(typeof(ServiceManager));
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.StartsWith('d'))
        {
            Svc.Chat.Print(GetFocusedAddonOrLast()->NameString);
            Svc.Chat.Print(string.Join("\n", ActiveButtons.Select(x => x.ToString())));
        }
        else
            exec ^= true;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= Tick;
        KeyStateWatcher.NumKeyPressed -= OnNumKeyPress;
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        keyWatcher.Dispose();
        ECommonsMain.Dispose();
    }

    public class Button(AtkComponentButton* btn, Action buttonAction = null)
    {
        public AtkComponentButton* Base => btn;
        public bool Active => ButtonActive(btn);
        public Action ClickOverride => buttonAction;
        public string Id
        {
            get
            {
                if (btn->ButtonTextNode == null || btn->ButtonTextNode->NodeText.Length == 0)
                    return $"[{GetAddonFromNode(btn->AtkResNode)->NameString}:x{btn->AtkResNode->X}/y{btn->AtkResNode->Y}/w{btn->AtkResNode->Width}/h{btn->AtkResNode->Height}]";
                return btn->ButtonTextNode->NodeText.ToString();
            }
        }

        public void DrawKey(int idx, AtkResNode* drawNodeOverride = null)
        {
            var drawNode = drawNodeOverride != null ? drawNodeOverride : btn->AtkResNode;
            var pos = GetNodePosition(drawNode);
            var text = IndexToText(idx);
            // check whether an element with the same text already exists, and if it does, replace it with the new
            DrawList.RemoveAll(x => x.Text == text);
            // only offset if you passed a text node, otherwise it looks bad
            DrawList.Add((drawNode->GetAsAtkTextNode() == null ? pos.X : pos.X - horizontalOffset, pos.Y + drawNode->Height / 2, text));
        }

        public bool Click()
        {
            if (ClickOverride != null)
            {
                PluginLog.Debug($"Performing {ClickOverride.Method.Name}");
                ClickOverride();
                return true;
            }
            else
            {
                var adn = GetAddonFromNode(btn->AtkResNode);
                if (adn == null)
                {
                    PluginLog.Error("Failed to find addon from button.");
                    return false;
                }

                PluginLog.Debug($"Clicking {Id}");
                btn->ClickAddonButton(adn);
                return true;
            }
        }

        public override string ToString() => $"{Id}: [A:{Active} C:{(ClickOverride != null ? ClickOverride.Method.Name : "null")} Atk:{GetAddonFromNode(btn->AtkResNode)->NameString}]";
    }

    private void OnNumKeyPress(int idx)
    {
        if (ActiveButtons.Count == 0 || idx >= ActiveButtons.Count) return;
        PluginLog.Verbose($"Pressed {idx} out of {ActiveButtons.Count}");
        ActiveButtons[idx].Click();
    }

    private void Tick(object framework)
    {
        if (!exec) return;
        DrawList.Clear();
        ActiveButtons.Clear();
        keyWatcher.Enabled = false;
        try
        {
            var atk = GetFocusedAddonOrLast();
            if (atk == null || !IsAddonReady(atk)) return;
            // requires special handling
            if (TryGetAddonMasterIfFocused<AddonMaster.SelectString>(atk, out var ss))
                DrawEntries(ss);
            if (TryGetAddonMasterIfFocused<SelectIconString>(atk, out var sis))
                DrawEntries(sis);
            if (TryGetAddonMasterIfFocused<ContextMenu>(atk, out var cm))
                DrawEntries(cm);
            if (TryGetAddonMasterIfFocused<RetainerList>(atk, out var rl))
                DrawEntries(rl);
            if (TryGetAddonMasterIfFocused<_CharaSelectListMenu>(atk, out var cslm))
                DrawEntries(cslm);
            if (TryGetAddonMasterIfFocused<BankaCraftworksSupply>(atk, out var bcs))
                DrawEntries(bcs);

            // generic button addons
            if (TryGetAddonMasterIfFocused<_TitleMenu>(atk, out var tm))
                DrawEntries([tm.StartButton, tm.DataCenterButton, tm.MoviesAndTitlesButton, tm.OptionsButton, tm.LicenseButton, tm.ExitButton]);
            if (TryGetAddonMasterIfFocused<AirShipExploration>(atk, out var ase))
                DrawEntries(ase.DeployButton);
            if (TryGetAddonMasterIfFocused<AirShipExplorationResult>(atk, out var aser))
                DrawEntries([aser.RedeployButton, aser.FinalizeReportButton]);
            if (TryGetAddonMasterIfFocused<Bank>(atk, out var b))
                DrawEntries([b.ProceedButton, b.CancelButton]);
            if (TryGetAddonMasterIfFocused<BannerMIP>(atk, out var bmip))
                DrawEntries([bmip.OkButton, bmip.CancelButton]);
            if (TryGetAddonMasterIfFocused<CollectablesShop>(atk, out var cs))
                DrawEntries(cs.TradeButton);
            if (TryGetAddonMasterIfFocused<ColorantColoring>(atk, out var cc))
                DrawEntries([cc.ApplyButton, cc.SelectAnotherButton]);
            if (TryGetAddonMasterIfFocused<CompanyCraftRecipeNoteBook>(atk, out var ccrn))
                DrawEntries(ccrn.BeginButton);
            if (TryGetAddonMasterIfFocused<CompanyCraftSupply>(atk, out var ccs))
                DrawEntries(ccs.CloseButton);
            if (TryGetAddonMasterIfFocused<ContentsFinderSetting>(atk, out var cfs))
                DrawEntries([cfs.ConfirmButton, cfs.CloseButton]);
            if (TryGetAddonMasterIfFocused<ContentsFinderStatus>(atk, out var cfs2))
                DrawEntries(cfs2.WithdrawButton);
            if (TryGetAddonMasterIfFocused<Dialogue>(atk, out var d))
                DrawEntries([d.OkButton]);
            if (TryGetAddonMasterIfFocused<DifficultySelectYesNo>(atk, out var dyn))
                DrawEntries([dyn.ProceedButton, dyn.LeaveButton]); // TODO: the radio buttons
            if (TryGetAddonMasterIfFocused<GcArmyChangeClass>(atk, out var gcac))
                DrawEntries([gcac.GladiatorButton, gcac.MarauderButton, gcac.PugilistButton, gcac.LancerButton, gcac.RogueButton, gcac.ArcherButton, gcac.ConjurerButton, gcac.ThaumaturgeButton, gcac.ArcanistButton]);
            if (TryGetAddonMasterIfFocused<GcArmyExpedition>(atk, out var gae))
                DrawEntries(gae.Addon->DeployButton);
            if (TryGetAddonMasterIfFocused<GcArmyExpeditionResult>(atk, out var gaer))
                DrawEntries(gaer.Addon->CompleteButton);
            if (TryGetAddonMasterIfFocused<GcArmyMenberProfile>(atk, out var gcp))
            {
                if (gcp.Addon->AtkValues[18].Bool) // afaik this bool indicates whether the buttons are shown or the radio buttons. Could also be #23 but the value is inverted.
                    DrawEntries([gcp.QuestionButton, gcp.PostponeButton, gcp.DismissButton]);
                // TODO: radio buttons
            }
            if (TryGetAddonMasterIfFocused<GcArmyTraining>(atk, out var gat))
                DrawEntries(gat.CloseButton);
            if (TryGetAddonMasterIfFocused<GearSetList>(atk, out var gsl))
                DrawEntries(gsl.EquipSetButton);
            if (TryGetAddonMasterIfFocused<ItemDetailCompare>(atk, out var idc))
                DrawEntries(idc.CloseButton);
            if (TryGetAddonMasterIfFocused<ItemInspectionResult>(atk, out var iir))
                DrawEntries([iir.NextButton, iir.CloseButton]);
            if (TryGetAddonMasterIfFocused<InputNumeric>(atk, out var inu))
                DrawEntries([inu.OkButton, inu.CancelButton]);
            if (TryGetAddonMasterIfFocused<ItemFinder>(atk, out var ifr))
                DrawEntries(ifr.CloseButton);
            if (TryGetAddonMasterIfFocused<JournalDetail>(atk, out var jd))
                DrawEntries([jd.Addon->AcceptMapButton, jd.Addon->InitiateButton, jd.Addon->AbandonDeclineButton]);
            if (TryGetAddonMasterIfFocused<JournalResult>(atk, out var jr))
                DrawEntries([jr.Addon->CompleteButton, jr.Addon->DeclineButton]);
            if (TryGetAddonMasterIfFocused<LetterHistory>(atk, out var lh))
                DrawEntries(lh.CloseButton);
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
            if (TryGetAddonMasterIfFocused<ManeuversArmorBoarding>(atk, out var mab))
                DrawEntries(mab.MountButton); // TODO: individual mount buttons. They behave weirdly
            if (TryGetAddonMasterIfFocused<ManeuversRecord>(atk, out var mr))
                DrawEntries(mr.LeaveButton);
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
            if (TryGetAddonMasterIfFocused<MJIRecipeNoteBook>(atk, out var mjirn))
                DrawEntries(mjirn.CraftButton);
            if (TryGetAddonMasterIfFocused<PurifyAutoDialog>(atk, out var pad))
                DrawEntries(pad.CancelExitButton);
            if (TryGetAddonMasterIfFocused<PurifyResult>(atk, out var pr))
                DrawEntries([pr.AutomaticButton, pr.CloseButton]);
            if (TryGetAddonMasterIfFocused<PvpProfile>(atk, out var pp))
                DrawEntries(pp.SeriesMalmstonesButton);
            if (TryGetAddonMasterIfFocused<PvpReward>(atk, out var pw))
                DrawEntries(pw.CloseButton);
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
            // TODO: check the confirm button
            //if (TryGetAddonMasterIfFocused<RetainerSell>(atk, out var rs))
            //    DrawEntries([rs.ConfirmButton, rs.CancelButton]);
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
            {
                if (yn.ButtonsVisible == 2)
                    DrawEntries([yn.Addon->YesButton, yn.Addon->NoButton]);
                else
                    // This ensures that the "no" option is always tied to the same key, for muscle memory
                    DrawEntries([yn.Addon->YesButton, yn.ThirdButton, yn.Addon->NoButton]);
            }
            if (TryGetAddonMasterIfFocused<SelectOk>(atk, out var ok))
                DrawEntries(ok.Addon->OkButton); // TODO: these have a second button?
            if (TryGetAddonMasterIfFocused<ShopCardDialog>(atk, out var scd))
                DrawEntries([scd.SellButton, scd.CancelButton]);
            if (TryGetAddonMasterIfFocused<ShopExchangeCurrencyDialog>(atk, out var secd))
                DrawEntries([secd.ExchangeButton, secd.CancelButton]);
            if (TryGetAddonMasterIfFocused<ShopExchangeItemDialog>(atk, out var seid))
                DrawEntries([seid.ExchangeButton, seid.CancelButton]);
            if (TryGetAddonMasterIfFocused<SynthesisSimpleDialog>(atk, out var ssd))
                DrawEntries([ssd.SynthesizeButton, ssd.CancelButton]);
            if (TryGetAddonMasterIfFocused<TripleTriadRequest>(atk, out var ttr))
                DrawEntries([ttr.ChallengeButton, ttr.QuitButton]);
            if (TryGetAddonMasterIfFocused<TripleTriadResult>(atk, out var ttrr))
                DrawEntries([ttrr.RematchButton, ttrr.QuitButton]);
            if (TryGetAddonMasterIfFocused<VoteMvp>(atk, out var vm))
                DrawEntries([vm.OkButton, vm.CancelButton]);
            if(TryGetAddonMasterIfFocused<WeeklyBingoResult>(atk, out var wbr))
                DrawEntries([wbr.AcceptButton, wbr.CancelButton]);
            if(TryGetAddonMasterIfFocused<MiragePrismMiragePlateConfirm>(atk, out var miragePrismMiragePlateConfirm))
                DrawEntries([miragePrismMiragePlateConfirm.YesButton, miragePrismMiragePlateConfirm.NoButton]);

            // generic callback addons
            if (TryGetAddonByName<AtkUnitBase>("FrontlineRecord", out var fl) && IsAddonReady(fl))
                DrawEntries(fl, fl->UldManager.NodeList[3]->GetAsAtkComponentButton(), -1);
            if (TryGetAddonByName<AtkUnitBase>("MKSRecord", out var mks) && IsAddonReady(mks))
                DrawEntries(mks, mks->UldManager.NodeList[4]->GetAsAtkComponentButton(), -1);
            if (TryGetAddonByName<AtkUnitBase>("DawnStory", out var ds) && IsAddonReady(ds))
                DrawEntries(ds, ds->UldManager.NodeList[84]->GetAsAtkComponentButton(), 14);
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message + "\n" + e.StackTrace);
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
    private void DrawEntries(AtkComponentButton* btn) => DrawEntries([btn]);
    private void DrawEntries(List<Pointer<AtkComponentButton>> btns)
    {
        ActiveButtons.Clear();
        btns.ForEach(x => ActiveButtons.Add(new(x)));
        foreach (var btn in ActiveButtons)
            if (btn.Active)
                btn.DrawKey(ActiveButtons.IndexOf(btn));
        keyWatcher.Enabled = true;
    }

    private void DrawEntries(AtkUnitBase* atk, AtkComponentButton* btn, params object[] callback)
    {
        ActiveButtons.Clear();
        ActiveButtons.Add(new(btn, () => Callback.Fire(atk, true, callback)));
        var button = ActiveButtons.First();
        if (button.Active)
            button.DrawKey(0);
        keyWatcher.Enabled = true;
    }
    #endregion


    #region Custom Draws
    private void DrawEntries(AddonMaster.SelectString am)
    {
        ActiveButtons.Clear();
        if (am.Base->UldManager.NodeListCount < 3) return;

        for (ushort i = 0; i < Math.Min(am.EntryCount, 12); i++)
        {
            ActiveButtons.Add(new(null, am.Entries[i].Select));
            ActiveButtons[i].DrawKey(i, &am.Entries[i].TextNode->AtkResNode);
        }
        keyWatcher.Enabled = true;
    }

    private void DrawEntries(SelectIconString am)
    {
        ActiveButtons.Clear();
        if (am.Base->UldManager.NodeListCount < 3) return;

        for (ushort i = 0; i < Math.Min(am.EntryCount, 12); i++)
        {
            ActiveButtons.Add(new(null, am.Entries[i].Select));
            ActiveButtons[i].DrawKey(i, &am.Entries[i].TextNode->AtkResNode);
        }
        keyWatcher.Enabled = true;
    }

    private void DrawEntries(ContextMenu am)
    {
        ActiveButtons.Clear();
        if (am.EntriesCount < 1) return;

        for (ushort i = 0; i < Math.Min(am.EntriesCount, 12); i++)
        {
            if (!am.Entries[i].IsNativeEntry) continue;
            var idx = i;
            ActiveButtons.Add(new(null, () => am.Entries[idx].Select()));
            ActiveButtons[i].DrawKey(i, &am.Entries[idx].TextNode->AtkResNode);
        }
        keyWatcher.Enabled = true;
    }

    private void DrawEntries(BankaCraftworksSupply am)
    {
        ActiveButtons.Clear();

        ActiveButtons.AddRange([new(am.DeliverButton), new(am.CancelButton)]);
        foreach (var btn in ActiveButtons)
            if (btn.Active)
                btn.DrawKey(ActiveButtons.IndexOf(btn));

        if (am.FirstUnfilledSlot != null && am.SlotsFilled.Count < am.RequestedItemNumberAvailable)
        {
            ActiveButtons.Add(new(null, () => TM.Enqueue(() => am.TryHandOver(am.FirstUnfilledSlot.Value))));
            ActiveButtons.Last().DrawKey(ActiveButtons.Count - 1, &am.Addon->GetComponentNodeById(50)->AtkResNode);
        }
        keyWatcher.Enabled = true;
    }

    private void DrawEntries(_CharaSelectListMenu am)
    {
        ActiveButtons.Clear();
        var list = am.Addon->GetComponentListById(13);
        foreach (var node in Enumerable.Range(0, list->GetItemCount()))
        {
            var item = list->GetItemRenderer(node);
            if (item == null)
                continue;
            ActiveButtons.Add(new(null, am.Characters[node].Login));
            ActiveButtons[node].DrawKey(node, item->AtkResNode);
        }
        keyWatcher.Enabled = true;
    }

    private void DrawEntries(RetainerList am)
    {
        ActiveButtons.Clear();

        var list = am.Addon->GetComponentListById(27);
        foreach (var node in Enumerable.Range(0, list->GetItemCount()))
        {
            var item = list->GetItemRenderer(node);
            if (item == null)
                continue;

            var idx = node;
            ActiveButtons.Add(new(null, () => am.Retainers[idx].Select()));
            ActiveButtons[node].DrawKey(node, item->AtkResNode);
        }
        keyWatcher.Enabled = true;
    }
    #endregion

    private static string IndexToText(int idx) => $"{(idx == 11 ? "=" : (idx == 10 ? "-" : (idx == 9 ? 0 : idx + 1)))}";

    private static bool ButtonActive(AtkComponentButton* btn)
        => btn != null && btn->IsEnabled && GetNodeVisible(btn->AtkResNode);

    private static bool ButtonActive(AtkComponentRadioButton* btn)
        => btn != null && btn->IsEnabled && GetNodeVisible(btn->AtkResNode);

    private static bool GetNodeVisible(AtkResNode* node)
    {
        if (node == null) return false;
        while (node != null)
        {
            if (!node->IsVisible()) return false;
            node = node->ParentNode;
        }

        return true;
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

    public static bool TryGetAddonMasterIfFocused<T>(AtkUnitBase* atk, out T addonMaster) where T : IAddonMasterBase
    {
        if (C.DisabledAddons.Contains(typeof(T).Name))
        {
            addonMaster = default;
            return false;
        }
        if (typeof(T).Name.Split(".")[^1] == atk->NameString)
        {
            addonMaster = (T)Activator.CreateInstance(typeof(T), [(nint)atk]);
            return addonMaster.Base == atk;
        }
        addonMaster = default;
        return false;
    }

    /// <summary>
    /// Gets the addon that is currently focused, or the last if none are focused
    /// </summary>
    private static AtkUnitBase* GetFocusedAddonOrLast()
    {
        var focus = AtkStage.Instance()->GetFocus();
        if (focus == null)
        {
            // this checks for any addons that aren't one of the always loaded types as a fallback (in case you clicked off of the focused addon)
            var atk = RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries.ToArray().LastOrDefault(x => x.Value != null && (x.Value->Flags198 & 0b1100_0000) == 0 && x.Value->HostId == 0, null);
            return atk.Value;
        }
        for (var i = 0; i < RaptureAtkUnitManager.Instance()->FocusedUnitsList.Count; i++)
        {
            var atk = RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries[i].Value;
            if (atk != null && atk->RootNode == GetRootNode(focus))
            {
                // JournalDetail is never in focus when brought up, but is always up when Journal/GuildLeve is, which we don't care about so just return JD
                if ((TryGetAddonByName<AtkUnitBase>("Journal", out var addon) && addon == atk) || (TryGetAddonByName<AtkUnitBase>("GuildLeve", out addon) && addon == atk))
                    return (AtkUnitBase*)Svc.GameGui.GetAddonByName("JournalDetail");
                else
                    return atk;
            }
        }
        return null;
    }

    private static AtkUnitBase* GetAddonFromNode(AtkResNode* node)
    {
        for (var i = 0; i < RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Count; i++)
        {
            var atk = RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries[i].Value;
            if (atk == null || (atk->Flags198 & 0b1100_0000) != 0 || atk->HostId != 0) continue;
            if (atk->RootNode == GetRootNode(node))
                return atk;
        }
        return null;
    }
}
