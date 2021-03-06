﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.UnitTests
{
    public class MockRuntime : IRuntime, IDisposable
    {
        public event EventHandler Initialize;
        public event EventHandler Draw;
        public event UpdateEventHandler Update;
        public event KeyboardEventHandler KeyboardUp;
        public event KeyboardEventHandler KeyboardDown;
        public event ScreenEventHandler MouseUp;
        public event ScreenEventHandler MouseDown;
        public event ScreenEventHandler MouseMove;
        public Platform CurrentPlatform { get; }

        // TODO fire-eggs this needs to point at Civ Data to load earth map. Needs to be able to be changed.
        public string StorageDirectory => @"E:\proj\CivOne";

        public string GetSetting(string key)
        {
            return null;
        }

        public void SetSetting(string key, string value)
        {
            throw new NotImplementedException();
        }

        public RuntimeSettings Settings { get; }
        public MouseCursor CurrentCursor { get; set; }
        public Bytemap[] Layers { get; set; }
        public Palette Palette { get; set; }
        public IBitmap Cursor { get; set; }
        public int CanvasWidth { get; }
        public int CanvasHeight { get; }
        public void Log(string text, params object[] parameters)
        {
            Console.WriteLine(text, parameters);
        }

        public string BrowseFolder(string caption = "")
        {
            throw new NotImplementedException();
        }

        public string WindowTitle { get; set; }
        public void PlaySound(string file)
        {
            throw new NotImplementedException();
        }

        public void StopSound()
        {
            throw new NotImplementedException();
        }

        public void Quit()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public MockRuntime(RuntimeSettings settings)
        {
            Settings = settings;
            // TODO fire-eggs this needs to be false if you want to use Earth! and must have a pointer to the Civ data files!
            settings.Free = false;
            RuntimeHandler.Register(this);
        }
    }
}
