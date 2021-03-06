// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Graphics;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens.Dialogs
{
	internal class DiplomatCity : BaseDialog
	{
		private const int FONT_ID = 0;

		private readonly City _enemyCity;
		private readonly Diplomat _diplomat;

		private void EstablishEmbassy(object sender, EventArgs args)
		{
			Human.EstablishEmbassy(_enemyCity.Player);
			Game.DisbandUnit(_diplomat);
			Cancel();
		}

		private void InvestigateCity(object sender, EventArgs args)
		{
			GameTask.Enqueue(Show.ViewCity(_enemyCity));
			Game.DisbandUnit(_diplomat);
			Cancel();
		}

		private void InciteRevolt(object sender, EventArgs args)
		{
			GameTask.Enqueue(Tasks.Show.DiplomatIncite(_enemyCity, _diplomat));
			Cancel();
		}

		private void IndustrialSabotage(object sender, EventArgs args)
		{
            GameTask.Enqueue(Message.Spy("Spies report:", $"{_diplomat.Sabotage(_enemyCity)}", $"in {_enemyCity.Name}"));
			Cancel();
		}

		private void MeetWithKing(object sender, EventArgs args)
		{
			GameTask.Enqueue(Tasks.Show.MeetKing(_enemyCity.Player));
			Cancel();
		}

		private void StealTechnology(object sender, EventArgs args)
		{
			IAdvance advance = _diplomat.GetAdvanceToSteal(_enemyCity.Player);

			if (advance == null)
			{
				GameTask.Insert(Message.General($"No new technology found"));
			}
			else
			{
				GameTask task = new Tasks.GetAdvance(_diplomat.Player, advance);

				task.Done += (s1, a1) =>
				{
					Game.DisbandUnit(_diplomat);
					if (_diplomat.Player == Human || _enemyCity.Player == Human)
						GameTask.Insert(Message.Spy("Spies report:", $"{_diplomat.Player.TribeName} steal", $"{advance.Name}"));
				};

				GameTask.Enqueue(task);
			}

			Cancel();
		}

		protected override void FirstUpdate()
		{
			int choices = 6;

            int high = (2 * Resources.GetFontHeight(FONT_ID)) + (choices * Resources.GetFontHeight(FONT_ID)) + 8;
			Menu menu = new Menu(Palette, Selection(3, 5 + (2 * Resources.GetFontHeight(FONT_ID)), 125, high ))
			{
				X = 103,
				Y = 100,
				MenuWidth = 130,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = FONT_ID
			};

			menu.Items.Add("Establish Embassy").OnSelect(EstablishEmbassy).SetEnabled(!Human.HasEmbassy(_enemyCity.Player));
			menu.Items.Add("Investigate City").OnSelect(InvestigateCity);
			menu.Items.Add("Steal Technology").OnSelect(StealTechnology);
			menu.Items.Add("Industrial Sabotage").OnSelect(IndustrialSabotage);
			menu.Items.Add("Incite a Revolt").OnSelect(InciteRevolt).SetEnabled(!_enemyCity.HasBuilding<Palace>());
			menu.Items.Add("Meet with King").OnSelect(MeetWithKing).SetEnabled(!(_enemyCity.Player.Civilization is Barbarian));
			
			AddMenu(menu);

            // TODO fire-eggs is there any dialog-based menu which DOESN'T want these established?
            menu.Cancel += Cancel;
            menu.MissClick += Cancel;
        }

		private static int DialogHeight()
		{
			int choices = 6 + 3;

			return (choices * Resources.GetFontHeight(FONT_ID));
		}

		internal DiplomatCity(City enemyCity, Diplomat diplomat) : base(100, 80, 145, DialogHeight())
		{
			_enemyCity = enemyCity ?? throw new ArgumentNullException(nameof(enemyCity));
			_diplomat = diplomat ?? throw new ArgumentNullException(nameof(diplomat));

			DialogBox.DrawText($"{_enemyCity.Player.TribeName} diplomat arrives", 0, 15, 5, 5);
			DialogBox.DrawText($"in {_enemyCity.Name}", 0, 15, 5, 5 + Resources.GetFontHeight(FONT_ID));
		}
	}
}