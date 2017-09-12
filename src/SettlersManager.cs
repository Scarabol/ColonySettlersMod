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

    public NetworkID ID;
    public string Name;
    public int SettlementTargetSize = 30;
    public Vector3Int SettlementOrigin;
    public BotAPIWrapper Api = null;

    public SettlersManager (ulong steamId, Vector3Int settlementOrigin)
    {
      this.SteamID = steamId;
      this.ID = new NetworkID (new Steamworks.CSteamID (this.SteamID));
      this.Name = $"BOT {this.SteamID}";
      this.SettlementOrigin = settlementOrigin;
      this.Api = new BotAPIWrapper ();
    }

    public SettlersManager (JSONNode jsonNode)
      : this (jsonNode.GetAs<ulong> ("steamId"), (Vector3Int)jsonNode.GetAs<JSONNode> ("settlementOrigin"))
    {
    }

    public JSONNode toJSON ()
    {
      JSONNode result = new JSONNode ();
      result.SetAs ("steamId", this.SteamID);
      result.SetAs ("settlementOrigin", (JSONNode)this.SettlementOrigin);
      return result;
    }

    public void Connect ()
    {
      Api.Connect (this.ID, this.Name);
      Api.UpdatePosition (SettlementOrigin.Add (0, 0, -3).Vector, 0);
      SettlersStrategy strategy = SettlersStrategy.GetSimple ();
      new Thread (() => {
        Thread.CurrentThread.IsBackground = true;
        Pipliz.Log.Write ($"Started AI thread for '{this.Name}'");
        while (true) {
          try {
            strategy.Execute (this);
          } catch (Exception exception) {
            Pipliz.Log.WriteError ($"Exception in settlers thread; {exception.Message}");
          }
          Thread.Sleep (5000);
        }
      }).Start ();
    }
  }
}