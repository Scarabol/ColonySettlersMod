using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Recipes;
using Pipliz.APIProvider.Jobs;
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
      result.steps.Add (new Crafting ());
      result.steps.Add (new Laborers ());
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