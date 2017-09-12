﻿using System;
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

  public class PlaceBanner : StrategyStep
  {
    public virtual bool IsComplete (SettlersManager manager)
    {
      return manager.Api.GetBanner () != null;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      bool result = manager.Api.PlaceBlock (manager.SettlementOrigin, VoxelSide.yPlus, BuiltinBlocks.BannerTool, BuiltinBlocks.Banner);
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
    private bool hasWall = false;

    public virtual bool IsComplete (SettlersManager manager)
    {
      return hasWall;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      hasWall = true;
      for (int c = -manager.SettlementTargetSize; c < manager.SettlementTargetSize; c++) {
        for (int y = 0; y > -4; y--) {
          Vector3Int absPos = manager.SettlementOrigin + new Vector3Int (-manager.SettlementTargetSize, y, c);
          manager.Api.RemoveBlock (absPos);
        }
      }
      for (int c = -manager.SettlementTargetSize; c < manager.SettlementTargetSize; c++) {
        for (int y = 0; y > -4; y--) {
          Vector3Int absPos = manager.SettlementOrigin + new Vector3Int (c, y, manager.SettlementTargetSize);
          manager.Api.RemoveBlock (absPos);
        }
      }
      for (int c = -manager.SettlementTargetSize; c < manager.SettlementTargetSize; c++) {
        for (int y = 0; y > -4; y--) {
          Vector3Int absPos = manager.SettlementOrigin + new Vector3Int (manager.SettlementTargetSize, y, -c);
          manager.Api.RemoveBlock (absPos);
        }
      }
      for (int c = -manager.SettlementTargetSize; c < manager.SettlementTargetSize; c++) {
        for (int y = 0; y > -4; y--) {
          Vector3Int absPos = manager.SettlementOrigin + new Vector3Int (-c, y, -manager.SettlementTargetSize);
          manager.Api.RemoveBlock (absPos);
        }
      }
      Vector3Int bridgePos = new Vector3Int (manager.SettlementOrigin.x, 0, manager.SettlementOrigin.z - manager.SettlementTargetSize);
      bridgePos.y = manager.Api.GetAvgHeight (bridgePos.x, bridgePos.z, 1);
      ushort itemTypePlanks = ItemTypes.IndexLookup.GetIndex ("planks");
      if (!manager.Api.PlaceBlock (bridgePos, itemTypePlanks, itemTypePlanks)) {
        Pipliz.Log.Write ("AI: Could not place bridge block");
      }
      Pipliz.Log.Write ("AI: Trench done");
      // TODO aggregate result over all placements
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
      // TODO check if bed is already there or improve placeblock to handle rotatables
      for (int z = -1; z <= 1; z++) {
        manager.Api.PlaceBlock (manager.SettlementOrigin.Add (-5, 0, z), VoxelSide.yPlus, BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXN);
        manager.Api.PlaceBlock (manager.SettlementOrigin.Add (5, 0, z), VoxelSide.yPlus, BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXP);
      }
      // TODO aggregate state over all placements
      return true;
    }
  }

  public class PlaceQuiver : StrategyStep
  {
    private Vector3Int GetQuiverPos (SettlersManager manager)
    {
      Vector3Int quiverPos = new Vector3Int (manager.SettlementOrigin.x, 0, manager.SettlementOrigin.z - manager.SettlementTargetSize + 15);
      quiverPos.y = TerrainGenerator.GetHeight (quiverPos.x, quiverPos.z) + 1;
      return quiverPos;
    }

    public virtual bool IsComplete (SettlersManager manager)
    {
      ushort actualType;
      return World.TryGetTypeAt (GetQuiverPos (manager), out actualType) && actualType == BuiltinBlocks.QuiverZN;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      Vector3Int quiverPos = GetQuiverPos (manager);
      bool result = manager.Api.PlaceBlock (quiverPos, ItemTypes.IndexLookup.GetIndex ("quiver"), BuiltinBlocks.QuiverZN);
      if (result) {
        Pipliz.Log.Write ($"AI: placed my quiver at {quiverPos}");
      } else {
        Pipliz.Log.Write ($"AI: Could not place quiver at {quiverPos}");
      }
      return result;
    }
  }

  public class Crafting : StrategyStep
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
      return manager.Api.GetColony ().LaborerCount > 0;
    }

    public virtual bool Execute (SettlersManager manager)
    {
      Pipliz.Log.Write ("AI: tried to hire a missing laborer");
      Colony colony = manager.Api.GetColony ();
      int followerBefore = colony.FollowerCount;
      colony.TryAddLaborer ();
      return colony.FollowerCount > followerBefore;
    }
  }
}