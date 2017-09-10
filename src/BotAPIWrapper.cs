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

    public bool PlaceBlock (Vector3Int position, ushort typeSelected, ushort typeToBuild)
    {
      Vector3Int voxelHit;
      VoxelSide voxelHitSide;
      ushort actualType;
      if (World.TryGetTypeAt (position.Add (0, -1, 0), out actualType) && actualType != BuiltinBlocks.Air) {
        voxelHit = position.Add (0, -1, 0);
        voxelHitSide = VoxelSide.yPlus;
      } else if (World.TryGetTypeAt (position.Add (1, 0, 0), out actualType) && actualType != BuiltinBlocks.Air) {
        voxelHit = position.Add (1, 0, 0);
        voxelHitSide = VoxelSide.xMin;
      } else if (World.TryGetTypeAt (position.Add (-1, 0, 0), out actualType) && actualType != BuiltinBlocks.Air) {
        voxelHit = position.Add (-1, 0, 0);
        voxelHitSide = VoxelSide.xPlus;
      } else if (World.TryGetTypeAt (position.Add (0, 0, -1), out actualType) && actualType != BuiltinBlocks.Air) {
        voxelHit = position.Add (0, 0, -1);
        voxelHitSide = VoxelSide.zPlus;
      } else if (World.TryGetTypeAt (position.Add (0, 0, 1), out actualType) && actualType != BuiltinBlocks.Air) {
        voxelHit = position.Add (0, 0, 1);
        voxelHitSide = VoxelSide.zMin;
      } else if (World.TryGetTypeAt (position.Add (0, 1, 0), out actualType) && actualType != BuiltinBlocks.Air) {
        voxelHit = position.Add (0, 1, 0);
        voxelHitSide = VoxelSide.yMin;
      } else {
        return false;
      }
      return PlaceBlock (voxelHit, voxelHitSide, typeSelected, typeToBuild);
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

    public bool RemoveBlock (Vector3Int position)
    {
      ModLoader.OnTryChangeBlockUserData data = new ModLoader.OnTryChangeBlockUserData ();
      data.isPrimaryAction = true;
      data.requestedBy = player;
      data.voxelHit = position;
      data.voxelHitSide = VoxelSide.yPlus;
      data.typeSelected = BuiltinBlocks.Air;
      data.typeToBuild = BuiltinBlocks.Air;
      return ChangeBlock (data);
    }

    public bool ChangeBlock (ModLoader.OnTryChangeBlockUserData data)
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
      return (int)System.Math.Round (avgHeight / ((1 + range * 2) ^ 2));
    }

    public void Connect (NetworkID id)
    {
      ByteBuilder builder = ByteBuilder.Get ();
      builder.Write (player.Name);
      Players.Connect (id);
      Players.SetName (player, ByteReader.Get (builder.ToArray ()));
    }

    public Banner GetBanner ()
    {
      return BannerTracker.Get (this.player);
    }
  }
}