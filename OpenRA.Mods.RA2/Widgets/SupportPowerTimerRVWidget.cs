#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SupportPowerTimerRVWidget : Widget
	{
		public readonly string Font = "Bold";
		public readonly string Format = "{0}: {1}";
		public readonly TextAlign Align = TextAlign.Left;
		public readonly TimerOrder Order = TimerOrder.Descending;

		readonly int timestep;
		readonly IEnumerable<SupportPowerInstance> powers;
		readonly Color bgDark, bgLight;
		Pair<string, Color>[] texts;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerRVWidget(World world)
		{
			powers = world.ActorsWithTrait<SupportPowerManager>()
				.Where(p => !p.Actor.IsDead && !p.Actor.Owner.NonCombatant)
				.SelectMany(s => s.Trait.Powers.Values)
				.Where(p => p.Instances.Any() && p.Info.DisplayTimerStances != Stance.None && !p.Disabled);

			// Timers in replays should be synced to the effective game time, not the playback time.
			timestep = world.Timestep;
			if (world.IsReplay)
				timestep = world.WorldActor.Trait<MapOptions>().GameSpeed.Timestep;

			bgDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
			bgLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
		}

		public override void Tick()
		{
			var displayedPowers = powers.Where(p =>
			{
				var owner = p.Instances[0].Self.Owner;
				var viewer = owner.World.RenderPlayer ?? owner.World.LocalPlayer;
				return viewer == null || p.Info.DisplayTimerStances.HasStance(owner.Stances[viewer]);
			});

			texts = displayedPowers.Select(p =>
			{
				var level = p.GetLevel();
				var time = WidgetUtils.FormatTime(p.RemainingTicks, false, timestep);
				var text = Format.F(p.Info.Descriptions.First(ld => ld.Key == level).Value, time);
				var self = p.Instances[0].Self;
				var playerColor = self.Owner.Color;

				if (Game.Settings.Game.UsePlayerStanceColors)
					playerColor = self.Owner.PlayerStanceColor(self);

				var color = !p.Ready || Game.LocalTick % 50 < 25 ? playerColor : Color.White;

				return Pair.New(text, color);
			}).ToArray();
		}

		public override void Draw()
		{
			if (!IsVisible() || texts == null)
				return;

			var y = 0;
			foreach (var t in texts)
			{
				var font = Game.Renderer.Fonts[Font];
				var textSize = font.Measure(t.First);
				var location = new float2(Bounds.Location) + new float2(0, y);

				if (Align == TextAlign.Center)
					location += new int2((Bounds.Width - textSize.X) / 2, 0);

				if (Align == TextAlign.Right)
					location += new int2(Bounds.Width - textSize.X, 0);

				font.DrawTextWithShadow(t.First, location, t.Second, bgDark, bgLight, 1);
				y += (textSize.Y + 5) * (int)Order;
			}
		}

		public enum TimerOrder { Ascending = -1, Descending = 1 }
	}
}
