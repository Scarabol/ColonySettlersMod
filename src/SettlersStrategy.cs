using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using NPC;
using System.Threading;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  public class SettlersStrategy
  {
    private List<StrategyStep> steps = new List<StrategyStep> ();

    public static SettlersStrategy GetSimple ()
    {
      /* steps to found a colony
       * 
       * build wall (dig trench?)
       * place beds
       * place archer at only entrance
       * produce berries
       * dig a mine
       * gather iron
       * melt iron
       * produce arrows (AI itself), don't need a crafter
       * hire a harvester (fallback kill trees bare hand)
       * produce planks
       * hire a coal miner
       */
      SettlersStrategy result = new SettlersStrategy ();
      result.steps.Add (new ClearArea ());
      result.steps.Add (new PlaceBanner ());
      result.steps.Add (new DigTrench ());
      result.steps.Add (new PlaceBeds ());
      result.steps.Add (new PlaceQuiver ());
      result.steps.Add (new CraftingBow ());
      result.steps.Add (new Laborers ());
      result.steps.Add (new BerryFarmers ());
      result.steps.Add (new Foresters ());
      result.steps.Add (new CraftingAxe ());
//      result.steps.Add (new DigMineStairs ());
//      result.steps.Add (new DigMineRoom ());
//      result.steps.Add (new CraftingPickaxe ());
//      result.steps.Add (new HireCoalMiner ());
//      result.steps.Add (new HireIronMiner ());
//      result.steps.Add (new PlaceFurnace ());
      return result;
    }

    public bool Execute (SettlersManager manager)
    {
      foreach (StrategyStep step in steps) {
        if (!step.IsComplete (manager)) {
          return step.Execute (manager);
        }
      }
      return false;
    }
  }
}