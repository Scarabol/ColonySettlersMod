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
  public static class BotAPIWrapper
  {
    public static bool PlaceBlock (Players.Player causedBy, Vector3Int voxelHit, VoxelSide voxelHitSide, ushort typeSelected, ushort typeToBuild)
    {
      ModLoader.OnTryChangeBlockUserData data = new ModLoader.OnTryChangeBlockUserData ();
      data.isPrimaryAction = false;
      data.requestedBy = causedBy;
      data.voxelHit = voxelHit;
      data.voxelHitSide = voxelHitSide;
      data.typeSelected = typeSelected;
      data.typeToBuild = typeToBuild;
      return ChangeBlock (data);
    }

    public static  bool ChangeBlock (ModLoader.OnTryChangeBlockUserData data)
    {
      ByteBuilder builder = ByteBuilder.Get ();
      builder.Write (data.isPrimaryAction);
      builder.Write (data.voxelHit);
      builder.Write ((byte)data.voxelHitSide);
      builder.Write (data.typeSelected);
      builder.Write (data.typeToBuild);
      return  ServerManager.TryChangeBlockUser (ByteReader.Get (builder.ToArray ()), data.requestedBy);
    }

    public static int GetBedCount (Players.Player player)
    {
      return BedBlockTracker.GetCount (player);
    }
  }
}