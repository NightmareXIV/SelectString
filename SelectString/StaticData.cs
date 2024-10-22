using ECommons.UIHelpers.AddonMasterImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace SelectString;
public static class StaticData
{
    public static Type[] AddonMasterOpts = [typeof(AddonMaster.SelectString), typeof(SelectIconString), typeof(ContextMenu), typeof(RetainerList), typeof(_CharaSelectListMenu), typeof(BankaCraftworksSupply), typeof(_TitleMenu), typeof(AirShipExploration), typeof(AirShipExplorationResult), typeof(Bank), typeof(CollectablesShop), typeof(ColorantColoring), typeof(CompanyCraftRecipeNoteBook), typeof(CompanyCraftSupply), typeof(ContentsFinderSetting), typeof(Dialogue), typeof(DifficultySelectYesNo), typeof(GcArmyChangeClass), typeof(GcArmyExpedition), typeof(GcArmyExpeditionResult), typeof(GcArmyMenberProfile), typeof(GcArmyTraining), typeof(GearSetList), typeof(ItemInspectionResult), typeof(InputNumeric), typeof(ItemFinder), typeof(JournalDetail), typeof(JournalResult), typeof(LetterHistory), typeof(LetterList), typeof(LetterViewer), typeof(LookingForGroup), typeof(LookingForGroupCondition), typeof(LookingForGroupDetail), typeof(LookingForGroupPrivate), typeof(LotteryWeeklyInput), typeof(LotteryWeeklyRewardList), typeof(ManeuversArmorBoarding), typeof(ManeuversRecord), typeof(MateriaAttachDialog), typeof(MaterializeDialog), typeof(MateriaRetrieveDialog), typeof(MiragePrismExecute), typeof(MiragePrismRemove), typeof(PurifyAutoDialog), typeof(PurifyResult), typeof(PvpProfile), typeof(PvpReward), typeof(RecipeNote), typeof(RecommendEquip), typeof(Repair), typeof(Request), typeof(RetainerItemTransferList), typeof(RetainerItemTransferProgress), typeof(RetainerTaskAsk), typeof(RetainerTaskResult), typeof(ReturnerDialog), typeof(SalvageAutoDialog), typeof(SalvageResult), typeof(SelectYesno), typeof(SelectOk), typeof(ShopCardDialog), typeof(ShopExchangeCurrencyDialog), typeof(ShopExchangeItemDialog), typeof(TripleTriadRequest), typeof(TripleTriadResult), typeof(VoteMvp), typeof(WeeklyBingoResult)];

    private static Dictionary<Type, string> AddonMasterTypeCache = [];

    public static string GetAddonMasterName(Type type)
    {
        if(AddonMasterTypeCache.TryGetValue(type, out var v)) return v;
        if(type.IsAssignableTo(typeof(IAddonMasterBase)))
        {
            var isntance = (IAddonMasterBase)Activator.CreateInstance(type, [(nint)0]);
            AddonMasterTypeCache[type] = isntance.AddonDescription;
            return isntance.AddonDescription;
        }
        return $"Can't get name: {type.Name}";
    }
}
