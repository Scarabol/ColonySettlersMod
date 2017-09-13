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
  public interface StrategyStep
  {
    bool IsComplete (SettlersManager manager);

    bool Execute (SettlersManager manager);
  }

  public class ClearArea : StrategyStep
  {
    private bool cleared = false;

    public virtual bool IsComplete (SettlersManager manager)
    {
      return cleared || manager.Api.GetBanner () != null;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      for (int y = 1; y <= 5; y++) {
        for (int x = -manager.SettlementTargetSize; x < manager.SettlementTargetSize; x++) {
          for (int z = -manager.SettlementTargetSize; z < manager.SettlementTargetSize; z++) {
            Vector3Int absPos = manager.SettlementOrigin + new Vector3Int (x, y, z);
            if (!manager.Api.RemoveBlock (absPos)) {
              return false;
            }
          }
        }
      }
      cleared = true;
      Pipliz.Log.Write ($"AI: Area is cleared");
      return true;
    }
  }

  public class PlaceBanner : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetBanner () != null;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      bool result = manager.Api.PlaceBlock (manager.SettlementOrigin, BuiltinBlocks.BannerTool, BuiltinBlocks.Banner);
      if (!result) {
        Pipliz.Log.Write ($"AI: Can't place banner at {manager.SettlementOrigin}");
      } else {
        Pipliz.Log.Write ($"AI: Banner placed at {manager.SettlementOrigin}");
      }
      return result;
    }
  }

  public class DigTrench : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.DefenceLevel >= 1;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      Pipliz.Log.Write ("AI: Started digging trench");
      Vector3Int absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize, -1, -manager.SettlementTargetSize);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      for (int c = -manager.SettlementTargetSize + 1; c < manager.SettlementTargetSize - 1; c++) {
        absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize, -1, c);
        if (!manager.Api.RemoveBlock (absPos)) {
          return false;
        }
        for (int y = 0; y > -2; y--) {
          absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize + 1, y - 1, c);
          if (!manager.Api.RemoveBlock (absPos)) {
            return false;
          }
        }
      }
      absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize, -1, manager.SettlementTargetSize - 1);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize, -1, manager.SettlementTargetSize);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      for (int c = -manager.SettlementTargetSize + 1; c < manager.SettlementTargetSize - 1; c++) {
        absPos = manager.SettlementOrigin + new Vector3Int (c, -1, manager.SettlementTargetSize);
        if (!manager.Api.RemoveBlock (absPos)) {
          return false;
        }
        for (int y = 0; y > -2; y--) {
          absPos = manager.SettlementOrigin + new Vector3Int (c, y - 1, manager.SettlementTargetSize - 1);
          if (!manager.Api.RemoveBlock (absPos)) {
            return false;
          }
        }
      }
      absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize - 1, -1, manager.SettlementTargetSize);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize, -1, manager.SettlementTargetSize);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      for (int c = -manager.SettlementTargetSize + 1; c < manager.SettlementTargetSize - 1; c++) {
        absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize, -1, -c);
        if (!manager.Api.RemoveBlock (absPos)) {
          return false;
        }
        for (int y = 0; y > -2; y--) {
          absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize - 1, y - 1, -c);
          if (!manager.Api.RemoveBlock (absPos)) {
            return false;
          }
        }
      }
      absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize, -1, -manager.SettlementTargetSize + 1);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize, -1, -manager.SettlementTargetSize);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      for (int c = -manager.SettlementTargetSize + 1; c < manager.SettlementTargetSize - 1; c++) {
        absPos = manager.SettlementOrigin + new Vector3Int (-c, -1, -manager.SettlementTargetSize);
        if (!manager.Api.RemoveBlock (absPos)) {
          return false;
        }
        for (int y = 0; y > -2; y--) {
          absPos = manager.SettlementOrigin + new Vector3Int (-c, y - 1, -manager.SettlementTargetSize + 1);
          if (!manager.Api.RemoveBlock (absPos)) {
            return false;
          }
        }
      }
      absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize + 1, -1, -manager.SettlementTargetSize);
      if (!manager.Api.RemoveBlock (absPos)) {
        return false;
      }
      for (int z = 0; z < 2; z++) {
        Vector3Int bridgePos = new Vector3Int (manager.SettlementOrigin.x, -1, manager.SettlementOrigin.z + manager.SettlementTargetSize - z);
        bridgePos.y = manager.Api.GetAvgHeight (bridgePos.x, bridgePos.z, 1);
        ushort itemTypePlanks = ItemTypes.IndexLookup.GetIndex ("planks");
        if (!manager.Api.PlaceBlock (bridgePos, itemTypePlanks, itemTypePlanks)) {
          Pipliz.Log.Write ("AI: Could not place bridge block");
          return false;
        }
      }
      manager.DefenceLevel = 1;
      Pipliz.Log.Write ("AI: Trench done");
      return true;
    }
  }

  public class PlaceBeds : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetBedCount () >= 6;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      for (int z = -1; z <= 1; z++) {
        if (!manager.Api.PlaceBlock (manager.SettlementOrigin.Add (-5, 0, z), BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXN)) {
          return false;
        }
        if (!manager.Api.PlaceBlock (manager.SettlementOrigin.Add (5, 0, z), BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXP)) {
          return false;
        }
      }
      return true;
    }
  }

  public class PlaceQuiver : StrategyStep
  {
    private Vector3Int GetQuiverPos (SettlersManager manager)
    {
      Vector3Int quiverPos = new Vector3Int (manager.SettlementOrigin.x, 0, manager.SettlementOrigin.z + manager.SettlementTargetSize - 15);
      quiverPos.y = TerrainGenerator.GetHeight (quiverPos.x, quiverPos.z) + 1;
      return quiverPos;
    }

    public virtual bool IsComplete (SettlersManager manager)
    {
      ushort actualType;
      return World.TryGetTypeAt (GetQuiverPos (manager), out actualType) && actualType == BuiltinBlocks.QuiverZP;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      Vector3Int quiverPos = GetQuiverPos (manager);
      bool result = manager.Api.PlaceBlock (quiverPos, ItemTypes.IndexLookup.GetIndex ("quiver"), BuiltinBlocks.QuiverZP);
      if (result) {
        Pipliz.Log.Write ($"AI: placed my quiver at {quiverPos}");
      } else {
        Pipliz.Log.Write ($"AI: Could not place quiver at {quiverPos}");
      }
      return result;
    }
  }

  public class CraftingBow : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetItemAmountStockpile ("bow") >= 1;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      return manager.Api.TryCraftItem ("bow");
    }
  }

  public class Laborers : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetLaborerCount () >= 0;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      bool result = manager.Api.AddLaborer ();
      if (result) {
        Pipliz.Log.Write ("AI: hired missing laborer");
      } else {
        Pipliz.Log.Write ("AI: could not hire laborer");
      }
      return result;
    }
  }

  public class BerryFarmers : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetBerryAreaJobsCount () >= 2;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      ushort itemTypeCrate = ItemTypes.IndexLookup.GetIndex ("crate");
      Vector3Int absPos = manager.SettlementOrigin.Add (-15, 0, -15);
      manager.Api.AddBerryAreaJob (absPos, 6);
      manager.Api.PlaceBlock (absPos.Add (7, 0, 0), itemTypeCrate, itemTypeCrate);
      Pipliz.Log.Write ($"AI: placed berry farmer at {absPos}");
      absPos = manager.SettlementOrigin.Add (15, 0, -15);
      manager.Api.AddBerryAreaJob (absPos, 6);
      manager.Api.PlaceBlock (absPos.Add (-7, 0, 0), itemTypeCrate, itemTypeCrate);
      Pipliz.Log.Write ($"AI: placed berry farmer at {absPos}");
      return manager.Api.GetBerryAreaJobsCount () >= 2;
    }
  }

  public class DigMineStairs : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.HasMine;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      Pipliz.Log.Write ("AI: Started digging mine stairs");
      ushort itemTypeStonebricks = ItemTypes.IndexLookup.GetIndex ("stonebricks");
      ushort itemTypeTorch = ItemTypes.IndexLookup.GetIndex ("torch");
      Vector3Int stairOffset = new Vector3Int (0, 0, 0);
      for (int y = manager.MinePos.y; y > 0; y--) {
        for (int x = -1; x <= 1; x++) {
          for (int z = 0; z <= 2; z++) {
            if (!manager.Api.RemoveBlock (new Vector3Int (manager.MinePos.x + x, y, manager.MinePos.z + z))) {
              return false;
            }
          }
        }
        if (stairOffset.y == 1 || (stairOffset.y > 0 && (stairOffset.y % 8) == 0)) {
          manager.Api.PlaceBlock (manager.MinePos.Add (0, -stairOffset.y, 2), itemTypeTorch, BuiltinBlocks.TorchZP);
        }
        manager.Api.PlaceBlock (manager.MinePos.Add (1, 0, 2) - stairOffset, itemTypeStonebricks, itemTypeStonebricks);
        stairOffset.y++;
        if (stairOffset.z >= 2) {
          if (stairOffset.x >= 2) {
            stairOffset.z = 1;
          } else {
            stairOffset.x++;
          }
        } else if (stairOffset.x >= 2) {
          if (stairOffset.z > 0) {
            stairOffset.z--;
          } else {
            stairOffset.x = 1;
          }
        } else if (stairOffset.x > 0) {
          stairOffset.x--;
        } else {
          stairOffset.z++;
        }
      }
      manager.HasMine = true;
      Pipliz.Log.Write ("AI: Mine stairs done");
      return true;
    }
  }

  public class DigMineRoom : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      int coalSpots = 0;
      int ironSpots = 0;
      int goldSpots = 0;
      foreach (MinerSpot spot in manager.MinerSpots) {
        if (spot.SpotType == MinerSpot.MinerSpotType.Coal) {
          coalSpots++;
        } else if (spot.SpotType == MinerSpot.MinerSpotType.Iron) {
          ironSpots++;
        } else if (spot.SpotType == MinerSpot.MinerSpotType.Gold) {
          goldSpots++;
        }
      }
      return coalSpots >= 1 && ironSpots >= 2 && goldSpots >= 1;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      if (!manager.HasMine) {
        return false;
      }
      Pipliz.Log.Write ("AI: Started digging mine room");
      ushort itemTypeInfCoal = ItemTypes.IndexLookup.GetIndex ("infinitecoal");
      ushort itemTypeInfIron = ItemTypes.IndexLookup.GetIndex ("infiniteiron");
      ushort itemTypeInfGold = ItemTypes.IndexLookup.GetIndex ("infinitegold");
      int iteration = 0;
      while (!IsComplete (manager) && iteration < 10) {
        for (int x = -1 - iteration; x <= 3 + iteration * 2; x++) {
          for (int z = iteration; z < 5 + iteration; z++) {
            Vector3Int absPos = new Vector3Int (manager.MinePos.x + x, 0, manager.MinePos.z + 2 + z);
            for (int y = 1; y <= 3; y++) {
              if (!manager.Api.RemoveBlock (absPos.Add (0, y, 0))) {
                return false;
              }
            }
            ushort itemTypeInf;
            if (!World.TryGetTypeAt (absPos, out itemTypeInf)) {
              Pipliz.Log.WriteError ($"AI: Could not determine type at {absPos}");
              return false;
            } else if (itemTypeInf == itemTypeInfCoal) {
              manager.MinerSpots.Add (new MinerSpot (MinerSpot.MinerSpotType.Coal, absPos));
              Pipliz.Log.Write ($"AI: Found some coal at {absPos}");
            } else if (itemTypeInf == itemTypeInfIron) {
              manager.MinerSpots.Add (new MinerSpot (MinerSpot.MinerSpotType.Iron, absPos));
              Pipliz.Log.Write ($"AI: Found some iron at {absPos}");
            } else if (itemTypeInf == itemTypeInfGold) {
              manager.MinerSpots.Add (new MinerSpot (MinerSpot.MinerSpotType.Gold, absPos));
              Pipliz.Log.Write ($"AI: Found some gold at {absPos}");
            }
          }
        }
        iteration++;
      }
      bool result = IsComplete (manager);
      if (!result) {
        Pipliz.Log.WriteError ($"AI: Could not find enough spots, something is wrong");
      } else {
        Pipliz.Log.Write ("AI: Mine room done");
      }
      return result;
    }
  }

  public class CraftingPickaxe : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetItemAmountStockpile ("pickaxe") >= 1;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      return manager.Api.TryCraftItem ("pickaxe");
    }
  }

  public class HireCoalMiner : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      int miners = 0;
      foreach (MinerSpot spot in manager.MinerSpots) {
        if (!spot.IsFree && spot.SpotType == MinerSpot.MinerSpotType.Coal) {
          miners++;
        }
      }
      return miners >= 1;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      return manager.Api.AddMinerJob (MinerSpot.MinerSpotType.Coal, manager.MinerSpots);
    }
  }

  public class HireIronMiner : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      int miners = 0;
      foreach (MinerSpot spot in manager.MinerSpots) {
        if (!spot.IsFree && spot.SpotType == MinerSpot.MinerSpotType.Iron) {
          miners++;
        }
      }
      return miners >= 1;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      return manager.Api.AddMinerJob (MinerSpot.MinerSpotType.Iron, manager.MinerSpots);
    }
  }
}