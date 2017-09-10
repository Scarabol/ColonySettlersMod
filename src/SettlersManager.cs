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
    int settlementTargetSize = 30;
    BotAPIWrapper api = null;
    Banner banner = null;
    Stockpile stockpile = null;

    bool hasWall = false;

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
      this.api = new BotAPIWrapper (this.player);
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
      api.Connect (this.ID);
      stockpile = Stockpile.GetStockPile (this.player);
      new Thread (() => {
        Thread.CurrentThread.IsBackground = true;
        Pipliz.Log.Write ($"Started AI thread for {player.Name}");
        while (true) {
          try {
            // primary goal: have a banner at settlement origin
            banner = BannerTracker.Get (this.player);
            if (banner == null) {
              bool result = api.PlaceBlock (this.settlementOrigin, VoxelSide.yPlus, BuiltinBlocks.BannerTool, BuiltinBlocks.Banner);
              if (!result) {
                Pipliz.Log.Write ($"AI: Can't place banner at {this.settlementOrigin}");
              } else {
                Pipliz.Log.Write ($"AI: Banner placed at {this.settlementOrigin}");
              }
            } else if (!hasWall) {
              hasWall = true;
              for (int c = -settlementTargetSize; c < settlementTargetSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (-settlementTargetSize, y, c);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              for (int c = -settlementTargetSize; c < settlementTargetSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (c, y, settlementTargetSize);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              for (int c = -settlementTargetSize; c < settlementTargetSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (settlementTargetSize, y, -c);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              for (int c = -settlementTargetSize; c < settlementTargetSize; c++) {
                for (int y = 0; y > -4; y--) {
                  Vector3Int absPos = settlementOrigin + new Vector3Int (-c, y, -settlementTargetSize);
                  // TODO use TryChangeBlockUser instead (permissions, sounds, ect.)
                  ServerManager.TryChangeBlock (absPos, BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
                }
              }
              Vector3Int bridgePos = new Vector3Int (settlementOrigin.x, 0, settlementOrigin.z - settlementTargetSize);
              bridgePos.y = api.GetAvgHeight (bridgePos.x, bridgePos.z, 1);
              ServerManager.TryChangeBlock (bridgePos, ItemTypes.IndexLookup.GetIndex ("planks"), ServerManager.SetBlockFlags.DefaultAudio);
              Pipliz.Log.Write ("AI: Wall done");
            } else {
              Colony colony = Colony.Get (this.player);
              if (api.GetBedCount () < 6) {
                for (int z = -1; z <= 1; z++) {
                  api.PlaceBlock (settlementOrigin.Add (-5, 0, z), VoxelSide.yPlus, BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXN);
                  api.PlaceBlock (settlementOrigin.Add (5, 0, z), VoxelSide.yPlus, BuiltinBlocks.Bed, BuiltinBlocks.BedHeadXP);
                }
              } else {
                Vector3Int archerPos = new Vector3Int (settlementOrigin.x, 0, settlementOrigin.z - 15);
                archerPos.y = TerrainGenerator.GetHeight (archerPos.x, archerPos.z);
                ushort actualType;
                if (World.TryGetTypeAt (archerPos.Add (0, 1, 0), out actualType) && actualType != BuiltinBlocks.QuiverZN) {
                  if (api.PlaceBlock (archerPos, VoxelSide.yPlus, ItemTypes.IndexLookup.GetIndex ("quiver"), BuiltinBlocks.QuiverZN)) {
                    Pipliz.Log.Write ($"AI: placed my archer at {archerPos}");
                    if (colony.LaborerCount < 1) {
                      colony.TryAddLaborer ();
                      Pipliz.Log.Write ("AI: tried to hire a missing laborer");
                    }
                  }
                } else {
                  ushort itemTypeBow = ItemTypes.IndexLookup.GetIndex ("bow");
                  if (stockpile.AmountContained (itemTypeBow) < 1) {
                    foreach (Recipe recipe in RecipePlayer.AllRecipes) {
                      bool crafted = false;
                      foreach (InventoryItem result in recipe.Results) {
                        if (result.Type == itemTypeBow) {
                          if (recipe.CanBeMade (stockpile) > 0) {
                            stockpile.Remove (recipe.Requirements);
                            stockpile.Add (recipe.Results);
                            crafted = true;
                            Pipliz.Log.Write ("AI: just crafted a bow");
                            break;
                          }
                        }
                      }
                      if (crafted) {
                        break;
                      }
                    }
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