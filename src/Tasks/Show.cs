// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Screens;
using CivOne.Screens.Dialogs;
using CivOne.Units;

namespace CivOne.Tasks
{
	internal class Show : GameTask
	{
		private readonly IScreen _screen;

		public void Closed(object sender, EventArgs args) => EndTask();

		public override void Run()
		{
			_screen.Closed += Closed;
			Common.AddScreen(_screen);
		}

		public static Show Empty => new Show(Overlay.Empty);

		public static Show InterfaceHelp => new Show(Overlay.InterfaceHelp);

		public static Show Terrain
		{
			get
			{
				GamePlay gamePlay = (GamePlay)Common.Screens.First(s => (s is GamePlay));
				return new Show(Overlay.TerrainView(gamePlay.X, gamePlay.Y));
			}
		}

		public static Show Goto
		{
			get
			{
				GamePlay gamePlay = (GamePlay)Common.Screens.First(s => (s is GamePlay));
				Goto gotoScreen = new Goto(gamePlay.X, gamePlay.Y);
				gotoScreen.Closed += (s, a) =>
				{
					if (Human != Game.CurrentPlayer) return;
					if (Game.ActiveUnit == null) return;
					if (gotoScreen.X == -1 || gotoScreen.Y == -1) return;
					Game.ActiveUnit.Goto = new Point(gotoScreen.X, gotoScreen.Y);
				};
				return new Show(gotoScreen);
			}
		}

		public static Show TaxRate => new Show(SetRate.Taxes);

		public static Show LuxuryRate => new Show(SetRate.Luxuries);

		public static Show AutoSave
		{
			get
			{
				if (Game.GameTurn % 50 != 0) return null;
				int gameId = ((Game.GameTurn / 50) % 6) + 4;
				return new Show(new SaveGame(gameId));
			}
		}

		public static Show CityManager(City city) => new Show(new CityManager(city));

		public static Show ViewCity(City city) => new Show(new CityManager(city, true));

		public static Show UnitStack(int x, int y) => new Show(new UnitStack(x, y));

		public static Show Search
		{
			get
			{
				Search search = new Search();
				search.Accept += (s, a) =>
				{
					City city = (s as Search).City;
					if (city == null) return;
					GamePlay gamePlay = (GamePlay)Common.Screens.First(x => x.GetType() == typeof(GamePlay));
					gamePlay.CenterOnPoint(city.X, city.Y);
				};
				return new Show(search);
			}
		}

		public static Show ChooseGovernment
		{
			get
			{
				ChooseGovernment chooseGovernment = new ChooseGovernment();
				chooseGovernment.Closed += (s, a) => {
					Human.Government = (s as ChooseGovernment).Result;
					GameTask.Insert(Message.NewGoverment(null, $"{Human.TribeName} government", $"changed to {Human.Government.Name}!"));
				};
				return new Show(chooseGovernment);
			}
		}

		public static Show Nuke(int x, int y) => new Show(new Nuke(x, y));

		public static Show DestroyUnit(IUnit unit, bool stack) => new Show(new DestroyUnit(unit, stack));

		public static Show CaptureCity(City city, string [] message) => new Show(CityView.Capture(city, message));

		public static Show DisorderCity(City city) => new Show(CityView.Disorder(city));

 		public static Show WeLovePresidentDayCity(City city) => new Show(CityView.WeLovePresidentDay(city));

		public static Show BuildPalace() => new Show(new PalaceView(true));

		public static Show CaravanChoice(Caravan unit, City city) => new Show(new CaravanChoice(unit, city));

		public static Show DiplomatBribe(BaseUnitLand unitToBribe, Diplomat diplomat) => new Show(new DiplomatBribe(unitToBribe, diplomat));

		public static Show DiplomatCity(City enemyCity, Diplomat diplomat) => new Show(new DiplomatCity(enemyCity, diplomat));

		public static Show DiplomatIncite(City enemyCity, Diplomat diplomat) => new Show(new DiplomatIncite(enemyCity, diplomat));

		public static Show SelectAdvanceAfterCityCapture(Player player, IList<IAdvance> advances) => new Show(new SelectAdvanceAfterCityCapture(player, advances));

		public static Show MeetKing(Player player) => new Show(new King(player));

		public static Show Screen<T>() where T : IScreen, new() => new Show(new T());

        private static Show Screen(Type type)
		{
			if (!typeof(IScreen).IsAssignableFrom(type)) return null;
			return new Show((IScreen)Activator.CreateInstance(type));
		}

		public static Show Screens(IEnumerable<Type> types)
		{
			Queue<Type> screenTypeQueue = new Queue<Type>(types.Where(x => typeof(IScreen).IsAssignableFrom(x)));
			if (screenTypeQueue.Count == 0) return null;
			Func<Show> nextTask = null;
			nextTask = () =>
			{
				if (screenTypeQueue.Count == 0) return null;
				Show showScreen = Show.Screen(screenTypeQueue.Dequeue());
				showScreen.Done += (s, a) => GameTask.Insert(nextTask());
				return showScreen;
			};
			return nextTask();
		}

		public static Show Screens(params Type[] types) => Screens(types.ToList());

		public static Show Screen(IScreen screen) => new Show(screen);

		private Show(IScreen screen)
		{
			_screen = screen;
		}
	}
}