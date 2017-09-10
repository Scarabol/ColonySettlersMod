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
  public class BotAPIWrapper
  {
    private Players.Player player;

    public BotAPIWrapper (Players.Player player)
    {
      this.player = player;
    }

    public bool PlaceBlock (Vector3Int voxelHit, VoxelSide voxelHitSide, ushort typeSelected, ushort typeToBuild)
    {
      ModLoader.OnTryChangeBlockUserData data = new ModLoader.OnTryChangeBlockUserData ();
      data.isPrimaryAction = false;
      data.requestedBy = player;
      data.voxelHit = voxelHit;
      data.voxelHitSide = voxelHitSide;
      data.typeSelected = typeSelected;
      data.typeToBuild = typeToBuild;
      return ChangeBlock (data);
    }

    public bool ChangeBlock (ModLoader.OnTryChangeBlockUserData data)
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
          avgHeight += TerrainGenerator.GetHeight (centerx + x, centerz + z);
        }
      }
      return (int)System.Math.Round (avgHeight / ((1 + range * 2) ^ 2));
    }
  }
}