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
  public class SettlersManager
  {
    public ulong SteamID { get; private set; }

    NetworkID ID;
    Players.Player player;
    Vector3Int settlementOrigin;
    Banner banner = null;
    Stockpile stockpile = null;

    bool hasWall = false;
    bool hasArcher = false;

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

    public SettlersManager (ulong steamId, Vector3Int settlementOrigin)
    {
      this.SteamID = steamId;
      this.ID = new NetworkID (new Steamworks.CSteamID (this.SteamID));
      this.player = new Players.Player (this.ID);
      this.player.Name = $"BOT {this.SteamID}";
      this.settlementOrigin = settlementOrigin;
    }

    public SettlersManager (JSONNode jsonNode)
      : this (jsonNode.GetAs<ulong> ("steamId"), (Vector3Int)jsonNode.GetAs<JSONNode> ("settlementOrigin"))
    {
    }

    public JSONNode toJSON ()
    {
      JSONNode result = new JSONNode ();
      result.SetAs ("steamId", this.SteamID);
      result.SetAs ("settlementOrigin", (JSONNode)this.settlementOrigin);
      return result;
    }

    public void Connect ()
    {
      Players.Connect (this.ID);
      stockpile = Stockpile.GetStockPile (this.player);
      new Thread (() => {
        Thread.CurrentThread.IsBackground = true;
        Pipliz.Log.Write ($"Started AI thread for {player.Name}");
        while (true) {
          try {
            // primary goal: have a banner at settlement origin
            banner = BannerTracker.Get (this.player);
            if (banner == null) {
              bool result = BotAPIWrapper.PlaceBlock (this.player, this.settlementOrigin, VoxelSide.yPlus, BuiltinBlocks.BannerTool, BuiltinBlocks.Banner);
              if (!result) {
                Chat.SendToAll ($"AI: Can't place banner at {this.settlementOrigin}");
              } else {
                Chat.SendToAll ($"AI: Banner placed at {this.settlementOrigin}");
              }
            } else if (!hasWall) {
              hasWall = true;
              int baseSize = 30;
              for (int c = -baseSize; c < baseSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (-baseSize, y, c);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              for (int c = -baseSize; c < baseSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (c, y, baseSize);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              for (int c = -baseSize; c < baseSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (baseSize, y, -c);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              for (int c = -baseSize; c < baseSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (-c, y, -baseSize);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              Vector3Int bridgePos = new Vector3Int (settlementOrigin.x, 0, settlementOrigin.z - baseSize);
              int avgHeight = 0;
              for (int x = -1; x <= 1; x++) {
                for (int z = -1; z <= 1; z++) {
                  avgHeight += TerrainGenerator.GetHeight (bridgePos.x + x, bridgePos.z + z);
                }
              }
              bridgePos.y = avgHeight / 9;
              ServerManager.TryChangeBlock (bridgePos, ItemTypes.IndexLookup.GetIndex ("planks"), ServerManager.SetBlockFlags.DefaultAudio);
              Chat.SendToAll ("AI: Wall done");
            } else {
              Colony colony = Colony.Get (this.player);
              if (BotAPIWrapper.GetBedCount (this.player) < 6) {
                for (int z = -1; z <= 1; z++) {
                  BotAPIWrapper.PlaceBlock (this.player, settlementOrigin.Add (-5, 0, z), VoxelSide.yPlus, BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXN);
                  BotAPIWrapper.PlaceBlock (this.player, settlementOrigin.Add (5, 0, z), VoxelSide.yPlus, BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXN);
                }
              } else if (!hasArcher) {
                Vector3Int archerPos = new Vector3Int (settlementOrigin.x, 0, settlementOrigin.z - 15);
                archerPos.y = TerrainGenerator.GetHeight (archerPos.x, archerPos.z);
                hasArcher = BotAPIWrapper.PlaceBlock (this.player, archerPos, VoxelSide.yPlus, ItemTypes.IndexLookup.GetIndex ("quiver"), BuiltinBlocks.QuiverZN);
                if (hasArcher) {
                  Chat.SendToAll ($"AI: placed my archer at {archerPos}");
                  if (colony.LaborerCount < 1) {
                    colony.TryAddLaborer ();
                    Chat.SendToAll ("AI: tried to hire a missing laborer");
                  }
                }
              }
            }
          } catch (Exception exception) {
            Pipliz.Log.WriteError ($"Exception in settlers thread; {exception.Message}");
          }
          Thread.Sleep (10000);
        }
      }).Start ();
    }
  }
}