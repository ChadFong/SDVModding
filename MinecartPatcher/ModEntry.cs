﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecartPatcher
{
	public class ModEntry : StardewModdingAPI.Mod
	{
		public Dictionary<string, MinecartInstance> Minecarts;

		private int LastPage = 0;
		private int PageCount = 0;

		public override void Entry(IModHelper helper)
		{
			helper.Content.AssetLoaders.Add(new AssetLoader());
			new MCPModHooks(this);
			helper.Events.GameLoop.DayStarted += (obj, dsea) => LoadData();
		}

		public void LoadData()
		{
			Minecarts = Helper.GameContent.Load<Dictionary<string, MinecartInstance>>("MinecartPatcher.Minecarts");
		}

		public bool OnMinecartActivation(MinecartInstance mc, GameLocation l, Vector2 vec)
		{
			if (Game1.player.mount != null) return true;
			if (Game1.MasterPlayer.mailReceived.Contains("ccBoilerRoom"))
			{
				if (!Game1.player.isRidingHorse() || Game1.player.mount == null)
				{
					drawMinecartDialogue(mc, l, 0, false);
					return true;
				}
				Game1.player.mount.checkAction(Game1.player, l);
			}
			else Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:MineCart_OutOfOrder"));
			return true;
		}

		public int RawDistance(int x1, int y1, int x2, int y2)
		{
			return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
		}

		public void drawMinecartDialogue(MinecartInstance src, GameLocation l, int page, bool finalPage)
		{
			LoadData();
			List<Response> responses = new List<Response>();
			if (page > 0) responses.Add(new Response("MCP.PaginationMinus", Helper.Translation.Get("previous")));
			int counter = 0;
			int startCount = (page * 4) + 2;
			if (page == 0) startCount -= 2;
			int endCount = ((page + 1) * 4) + 1;
			if (finalPage) endCount++;
			foreach (var mc in Minecarts.OrderBy(x => x.Value.DisplayName))
			{
				if (mc.Value.NetworkId != src.NetworkId) continue;
				if (mc.Value.LocationName == l.Name)
				{
					if (RawDistance(mc.Value.LandingPointX, mc.Value.LandingPointY, Game1.player.getTileX(), Game1.player.getTileY()) < 6) continue;
				}
				if (Game1.getLocationFromName(mc.Value.LocationName) == null) continue;
				if (mc.Value.MailCondition != null && !Game1.MasterPlayer.mailReceived.Contains(mc.Value.MailCondition)) continue;
				counter += 1;
				if (counter >= startCount && counter <= endCount) responses.Add(new Response(mc.Key, mc.Value.DisplayName));
			}
			PageCount = counter / 5;
			if (counter % 5 > 0) PageCount += 1;
			if (page < PageCount - 1) responses.Add(new Response("MCP.PaginationPlus", Helper.Translation.Get("next")));
			responses.Add(new Response("Cancel", Game1.content.LoadString("Strings\\Locations:MineCart_Destination_Cancel")));
			LastPage = page;
			Game1.activeClickableMenu = new DialogueBox(src, this, responses);
			Game1.dialogueUp = true;
			Game1.player.CanMove = false;
		}

		public bool onDialogueSelect(MinecartInstance src, string key)
		{
			if (key == "Cancel") return true;
			if (key == "MCP.PaginationMinus")
			{
				bool fp = false;
				int pageNum = Math.Max(0, LastPage - 1);
				if (pageNum == PageCount - 1) fp = true;
				drawMinecartDialogue(src, Game1.player.currentLocation, pageNum, fp);
				return true;
			}
			if (key == "MCP.PaginationPlus")
			{
				bool fp = false;
				int pageNum = Math.Min(PageCount - 1, LastPage + 1);
				if (pageNum == PageCount - 1) fp = true;
				drawMinecartDialogue(src, Game1.player.currentLocation, pageNum, fp);
				return true;
			}
			if (!Minecarts.ContainsKey(key)) return true;
			//Monitor.Log("Minecart key: " + key, LogLevel.Alert);
			WarpFarmer(Minecarts[key]);
			return true;
		}

		public MinecartInstance FindMinecart(GameLocation l, Vector2 vec)
		{
			if (l == null || string.IsNullOrWhiteSpace(l.Name)) return null;
			var tids = new List<int> { 958, 1080, 1081, 179, 195, 208, 224 };
			int tid = l.getTileIndexAt(Utility.Vector2ToPoint(vec), "Buildings");
			if (tids.Contains(tid))
			{
				//this is a minecart, but is it one of ours?
				foreach (var mc in Minecarts.Where(mc => mc.Value.LocationName == l.Name))
				{
					if (RawDistance(mc.Value.LandingPointX, mc.Value.LandingPointY, (int)vec.X, (int)vec.Y) < 6)
					{
						return mc.Value; //found it!
					}
				}
			}
			return null;
		}

		public void WarpFarmer(MinecartInstance mc)
		{
			if (mc.VanillaPassthrough != null)
			{
				//let vanilla handle this one (in case it's been overridden. usually just used for the vanilla carts.)
				Game1.player.currentLocation.answerDialogueAction(mc.VanillaPassthrough, new string[] { });
			}
			else
			{
				//let's handle it ourselves!
				Game1.player.Halt();
				Game1.player.freezePause = 700;
				Game1.warpFarmer(mc.LocationName, mc.LandingPointX, mc.LandingPointY, mc.LandingPointDirection);
				if (mc.IsUnderground)
				{
					if (Game1.getMusicTrackName() == "springtown")
					{
						Game1.changeMusicTrack("none");
					}
				}
			}
		}
	}
}
