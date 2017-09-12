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
  public class BotAPIWrapper
  {
    private Players.Player player;

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
      if (World.TryGetTypeAt (position, out data.typeTillNow) && (data.typeTillNow == typeSelected || data.typeTillNow == typeToBuild)) {
        return true;
      } else if (data.typeTillNow != BuiltinBlocks.Air && data.typeTillNow != BuiltinBlocks.Water) {
        if (!RemoveBlock (position)) {
          Pipliz.Log.Write ($"AI: Can't build at {position}, place is occupied");
          return false;
        }
      }
      if (ItemTypes.IsPlaceable (data.typeSelected) && !GetStockpile ().TryRemove (data.typeSelected)) {
        if (!TryCraftItem (data.typeSelected)) {
          string typename;
          if (ItemTypes.IndexLookup.TryGetName (data.typeSelected, out typename)) {
            Pipliz.Log.Write ($"AI: No {typename} item in stockpile to place");
          }
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
        GetStockpile ().Add (typeSelected);
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
          dummyInv.Add (ItemTypes.RemovalItems (data.typeTillNow));
          dummyInv.TryDump (GetStockpile ());
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
      // TODO remove existing block at this position? (add to stockpile, remove delay)
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
          avgHeight += TerrainGenerator.GetHeight (centerx + x, centerz + z);
        }
      }
      return Pipliz.Math.RoundToInt (avgHeight / System.Math.Pow (1 + range * 2, 2));
    }

    public Banner GetBanner ()
    {
      return BannerTracker.Get (this.player);
    }

    public Stockpile GetStockpile ()
    {
      return Stockpile.GetStockPile (this.player);
    }

    public int GetItemAmountStockpile (string name)
    {
      return GetStockpile ().AmountContained (ItemTypes.IndexLookup.GetIndex (name));
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
      Stockpile stockpile = GetStockpile ();
      foreach (Recipe recipe in RecipePlayer.AllRecipes) {
        foreach (InventoryItem result in recipe.Results) {
          if (result.Type == itemTypeResult && stockpile.TryRemove (recipe.Requirements)) {
            stockpile.Add (recipe.Results);
            string name;
            if (ItemTypes.IndexLookup.TryGetName (itemTypeResult, out name)) {
              Pipliz.Log.Write ($"AI: just crafted a {name}");
            }
            return true;
          }
        }
      }
      return false;
    }

    public int GetLaborerCount ()
    {
      return Colony.Get (this.player).LaborerCount - JobTracker.GetCount (this.player);
    }

    public bool AddLaborer ()
    {
      Colony colony = Colony.Get (this.player);
      int followerBefore = colony.FollowerCount;
      colony.TryAddLaborer ();
      return colony.FollowerCount > followerBefore;
    }

    public int GetBerryAreaJobsCount ()
    {
      return BerryAreaJobTracker.GetCount (this.player);
    }

    public void AddBerryAreaJob (UnityEngine.Bounds bounds)
    {
      BerryAreaJobTracker.Add (bounds, this.player);
    }
  }
}