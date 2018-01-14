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
using Server.TerrainGeneration;

namespace ScarabolMods
{
  // TODO this stuff should be included in the game and be removed soon
  public static class GeneralAPIWrapper
  {
    public static int GetTerrainHeight (float x, float z)
    {
      return (int)TerrainGenerator.UsedGenerator.GetHeight (x, z);
    }
  }
}