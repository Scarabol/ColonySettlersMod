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
using Pipliz.Mods.BaseGame.BlockNPCs;

namespace ScarabolMods
{
  public class BotAPIWrapper
  {
    private Players.Player player;

    public Stockpile Stockpile {
      get {
        return Stockpile.GetStockPile (this.player);
      }
    }

    public void Connect (NetworkID id, string name)
    {
      Players.Connect (id);
      this.player = Players.GetPlayer (id);
      ByteBuilder builder = ByteBuilder.Get ();
      builder.Write (name);
      Players.SetName (this.player, ByteReader.Get (builder.ToArray ()));
    }

    public void UpdatePosition (UnityEngine.Vector3 position, float yAngle)
    {
      ByteBuilder builder = ByteBuilder.Get ();
      builder.Write (position);
      builder.Write (yAngle);
      NetworkWrapper.ProcessMessage (General.Networking.ServerMessageType.UpdatePlayerPosition, this.player, ByteReader.Get (builder.ToArray ()));
      Pipliz.Log.Write ($"AI: moved to {position}, angle is {yAngle}");
    }

    public bool PlaceBlock (Vector3Int position, ushort typeSelected, ushort typeToBuild)
    {
      ModLoader.OnTryChangeBlockUserData data = new ModLoader.OnTryChangeBlockUserData ();
      data.isPrimaryAction = false;
      data.requestedBy = player;
      data.voxelHit = position.Add (0, -1, 0);
      data.voxelHitSide = VoxelSide.yPlus;
      data.typeSelected = typeSelected;
      data.typeToBuild = typeToBuild;
      bool solid;
      if (World.TryGetTypeAt (position, out data.typeTillNow) && (data.typeTillNow == typeSelected || data.typeTillNow == typeToBuild)) {
        return true;
      } else if (!World.TryIsSolid (position, out solid)) {
        Pipliz.Log.WriteError ($"Could not check for solid block at {position}");
      } else if (solid && !RemoveBlock (position)) {
        Pipliz.Log.Write ($"AI: Can't build at {position}, place is occupied");
        return false;
      }
      if (ItemTypes.GetType (data.typeSelected).IsPlaceable && !Stockpile.TryRemove (data.typeSelected) && !TryCraftItem (data.typeSelected)) {
        string typename;
        if (ItemTypes.IndexLookup.TryGetName (data.typeSelected, out typename)) {
          Pipliz.Log.Write ($"AI: No {typename} item in stockpile to place");
        }
      }
      bool result = ChangeBlock (data);
      if (result) {
        Thread.Sleep (200);
      } else {
        string typename;
        if (ItemTypes.IndexLookup.TryGetName (data.typeSelected, out typename)) {
          Pipliz.Log.Write ($"AI: Could not place {typename} at {position}");
        }
        Stockpile.Add (typeSelected);
      }
      return result;
    }

    public bool RemoveBlock (Vector3Int position)
    {
      ModLoader.OnTryChangeBlockUserData data = new ModLoader.OnTryChangeBlockUserData ();
      data.isPrimaryAction = true;
      data.requestedBy = player;
      data.voxelHit = position;
      data.voxelHitSide = VoxelSide.yPlus;
      data.typeSelected = BuiltinBlocks.Air;
      data.typeToBuild = BuiltinBlocks.Air;
      bool result = false;
      if (World.TryGetTypeAt (data.VoxelToChange, out data.typeTillNow)) {
        if (data.typeTillNow == BuiltinBlocks.Air) {
          return true;
        }
        result = ChangeBlock (data);
        if (result) {
          NPCInventory dummyInv = new NPCInventory (float.MaxValue);
          dummyInv.Add (ItemTypes.GetType (data.typeTillNow).OnRemoveItems);
          dummyInv.TryDump (Stockpile);
          // TODO sleep actual destructionTime minus delay in main thread
          Thread.Sleep (500);
        }
      } else {
        Pipliz.Log.WriteError ($"AI: Could not determine type at {position}");
      }
      return result;
    }

    private bool ChangeBlock (ModLoader.OnTryChangeBlockUserData data)
    {
      ByteBuilder builder = ByteBuilder.Get ();
      builder.Write (data.isPrimaryAction);
      builder.Write (data.voxelHit);
      builder.Write ((byte)data.voxelHitSide);
      builder.Write (data.typeSelected);
      builder.Write (data.typeToBuild);
      return  ServerManager.TryChangeBlockUser (ByteReader.Get (builder.ToArray ()), data.requestedBy);
    }

    public int GetBedCount ()
    {
      return BedBlockTracker.GetCount (player);
    }

    public int GetAvgHeight (int centerx, int centerz, int range)
    {
      float avgHeight = 0.0f;
      for (int x = -range; x <= range; x++) {
        for (int z = -range; z <= range; z++) {
          avgHeight += GeneralAPIWrapper.GetTerrainHeight (centerx + x, centerz + z);
        }
      }
      return Pipliz.Math.RoundToInt (avgHeight / System.Math.Pow (1 + range * 2, 2));
    }

    public Banner GetBanner ()
    {
      return BannerTracker.Get (this.player);
    }

    public int GetItemAmountStockpile (string name)
    {
      return Stockpile.AmountContained (ItemTypes.IndexLookup.GetIndex (name));
    }

    public bool TryCraftItem (string name)
    {
      ushort itemTypeResult;
      if (!ItemTypes.IndexLookup.TryGetIndex (name, out itemTypeResult)) {
        Pipliz.Log.Write ($"AI: Could not find item type {name}");
        return false;
      }
      return TryCraftItem (itemTypeResult);
    }

    public bool TryCraftItem (ushort itemTypeResult)
    {
      Stockpile stockpile = Stockpile;
      foreach (Recipe recipe in RecipePlayer.DefaultRecipes) {
        foreach (InventoryItem result in recipe.Results) {
          if (result.Type == itemTypeResult && stockpile.TryRemove (recipe.Requirements)) {
            stockpile.Add (recipe.Results);
            string name;
            if (ItemTypes.IndexLookup.TryGetName (itemTypeResult, out name)) {
              Pipliz.Log.Write ($"AI: just crafted {name}");
            }
            return true;
          }
        }
      }
      return false;
    }

    public int GetLaborerCount ()
    {
      int laborer = Colony.Get (this.player).LaborerCount;
      Log.Write ($"Bot has {laborer} laborer");
      return laborer;
    }

    public bool AddLaborer ()
    {
      Colony colony = Colony.Get (this.player);
      if (Stockpile.TotalFood < 3 * colony.FoodUsePerHour) {
        Pipliz.Log.Write ($"Could not hire, food too low");
        return false;
      }
      int followerBefore = colony.FollowerCount;
      colony.TryAddLaborer ();
      return colony.FollowerCount > followerBefore;
    }

    // TODO this is shit
    private int berryJobs = 0;

    public int GetBerryAreaJobsCount ()
    {
      return berryJobs;
    }

    public void AddBerryAreaJob (Vector3Int min, Vector3Int max)
    {
      AreaJobTracker.CreateNewAreaJob ("pipliz.berryfarm", this.player, min, max);
      berryJobs++;
    }

    private int foresterJobs = 0;

    public int GetForesterJobsCount ()
    {
      return foresterJobs;
    }

    public void AddForesterJob (Vector3Int min, Vector3Int max)
    {
      AreaJobTracker.CreateNewAreaJob ("pipliz.temperateforest", this.player, min, max);
      foresterJobs++;
      PlaceBlock (min.Add (0, 0, -1), BuiltinBlocks.Crate, BuiltinBlocks.Crate);
    }

    public bool AddMinerJob (MinerSpot.MinerSpotType spotType, List<MinerSpot> minerSpots)
    {
      foreach (MinerSpot spot in minerSpots) {
        if (spot.SpotType == spotType && spot.IsFree) {
          spot.IsFree = false;
          Vector3Int minerPos = spot.Position.Add (0, 1, 0);
          MinerJob job = new MinerJob ();
          // TODO check job orientation
          job.InitializeOnAdd (minerPos, BuiltinBlocks.MinerJobXN, this.player);
          Pipliz.Log.Write ($"Added a coal miner job");
          ushort itemTypeCrate = ItemTypes.IndexLookup.GetIndex ("crate");
          Vector3Int closestCrate = Stockpile.ClosestCrate (minerPos);
          if (closestCrate.IsValid && (closestCrate - minerPos).SqrMagnitudeLong < 3) {
            return true;
          }
          for (int x = -1; x <= 1; x++) {
            for (int z = -1; z <= 1; z++) {
              Vector3Int cratePos = minerPos.Add (x, 0, z);
              bool solid;
              ushort actualType;
              if ((x != 0 || z != 0) && World.TryIsSolid (cratePos, out solid) && !solid && World.TryGetTypeAt (cratePos.Add (0, -1, 0), out actualType) && actualType == BuiltinBlocks.InfiniteStone) {
                PlaceBlock (cratePos, itemTypeCrate, itemTypeCrate);
                Pipliz.Log.Write ($"AI: Placed crate at {cratePos}");
                return true;
              }
            }
          }
          return true;
        }
      }
      Pipliz.Log.Write ($"Could not find a free {spotType} miner spot");
      return false;
    }
  }

  public class MinerSpot
  {
    public enum MinerSpotType
    {
      Coal,
      Iron,
      Gold
    }

    public MinerSpotType SpotType;
    public Vector3Int Position;
    public bool IsFree;

    public MinerSpot (MinerSpotType spotType, Vector3Int position)
    {
      this.SpotType = spotType;
      this.Position = position;
      this.IsFree = true;
    }
  }
}