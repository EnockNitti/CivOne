// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.UserInterface;
using System;
using System.Linq;

namespace CivOne.Screens
{
    [Modal]
	internal class LoadGame : BaseScreen
	{
		private MouseCursor _cursor = MouseCursor.None;
		public override MouseCursor Cursor => _cursor;
		
		private char _driveLetter = 'C';
		private bool _update = true;
		private Menu _menu;
		
		public bool Cancel { get; private set; }
        
		private void LoadSaveFile(object sender, MenuItemEventArgs<int> args)
		{
			int item = args.Value;
			
            SaveGameFile file = SaveGameFile.GetSaveGames(_driveLetter).ToArray()[item];

            SaveGame.SelectedGame = (item > 3 ? 3 : item);
			
			Log("Load game: {0}", file.Name);
			
			Destroy();
			
			Game.LoadGame(file.SveFile, file.MapFile);
			Common.AddScreen(new GamePlay());
		}
		
		private void LoadEmptyFile(object sender, MenuItemEventArgs<int> args)
		{
			Log("Empty save file, cancel");
			Cancel = true;
			_update = true;
		}

		private MenuItemEventHandler<int> LoadFileHandler(SaveGameFile file)
		{
			if (file.ValidFile)
				return LoadSaveFile;
			return LoadEmptyFile;
		}
		
		private void DrawDriveQuestion()
		{
			Bitmap.Clear();
			this.Clear(15)
				.DrawText("Which drive contains your", 0, 5, 92, 72, TextAlign.Left)
				.DrawText("saved game files?", 0, 5, 104, 80, TextAlign.Left)
				.DrawText($"{_driveLetter}:", 0, 5, 146, 96, TextAlign.Left)
				.DrawText("Press drive letter and", 0, 5, 104, 112, TextAlign.Left)
				.DrawText("Return when disk is inserted", 0, 5, 80, 120, TextAlign.Left)
				.DrawText("Press Escape to cancel", 0, 5, 104, 128, TextAlign.Left);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_menu != null)
			{
				if (_menu.Update(gameTick))
				{
					Bitmap.Clear();
					this.Clear(15)
						.AddLayer(_menu);
					return true;
				}
				return Cancel;
			}
			else if (_update)
			{
				DrawDriveQuestion();
				_update = false;
				return true;
			}
			return Cancel;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (Cancel) return false;
			
			char c = Char.ToUpper(args.KeyChar);
			if (args.Key == Key.Escape)
			{
				Log("Cancel");
				Cancel = true;
				_update = true;
				return true;
			}
			else if (_menu != null)
			{
				return _menu.KeyDown(args);
			}
			else if (args.Key == Key.Enter)
			{
				_menu = new Menu(Palette)
				{
					Title = "Select Load File...",
					X = 51,
					Y = 70,
					MenuWidth = 217,
					TitleColour = 12,
					ActiveColour = 11,
					TextColour = 5,
					FontId = 0,
					IndentTitle = 2,
					RowHeight = 8
				};
				
				int i = 0;
				foreach (SaveGameFile file in SaveGameFile.GetSaveGames(_driveLetter))
				{
					_menu.Items.Add(file.Name, i++).OnSelect(LoadFileHandler(file));
				}
				_cursor = MouseCursor.Pointer;
			}
			else if (c >= 'A' && c <= 'Z')
			{
				_driveLetter = c;
				_update = true;
				return true;
			}
			return false;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_menu != null)
				return _menu.MouseDown(args);
			return false;
		}
		
		public override bool MouseUp(ScreenEventArgs args)
		{
			if (_menu != null)
				return _menu.MouseUp(args);
			return false;
		}
		
		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (_menu != null)
				return _menu.MouseDrag(args);
			return false;
		}
		
		public LoadGame(Palette palette)
		{
			Palette = palette;
		}
	}
}