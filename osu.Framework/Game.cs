﻿//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Windows.Forms;
using osu.Framework.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;
using System.Threading;
using osu.Framework.Audio;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Graphics.Shaders;
using osu.Framework.IO.Stores;
using Scheduler = osu.Framework.Threading.Scheduler;

namespace osu.Framework
{
    public class Game : LargeContainer
    {
        public BasicGameWindow Window => host?.Window;

        internal Scheduler Scheduler;

        private ThrottledFrameClock clock = new ThrottledFrameClock();

        protected override IFrameBasedClock Clock => clock;

        public ResourceStore<byte[]> Resources;

        public TextureStore Textures;

        /// <summary>
        /// This should point to the main resource dll file. If not specified, it will use resources embedded in your executable.
        /// </summary>
        protected virtual string MainResourceFile => AppDomain.CurrentDomain.FriendlyName;

        protected int MaximumFramesPerSecond
        {
            get { return clock.MaximumUpdateHz; }
            set { clock.MaximumUpdateHz = value; }
        }

        internal Thread MainThread;

        private BasicGameForm form => host?.Window?.Form;
        private BasicGameHost host;

        public BasicGameHost Host => host;

        private bool exitRequested;
        private bool isActive;

        public AudioManager Audio;

        public ShaderManager Shaders;

        public TextureStore Fonts;

        public Game()
        {
            Game = this;
        }

        public void SetHost(BasicGameHost host)
        {
            MainThread = Thread.CurrentThread;

            this.host = host;
            host.Exiting += (sender, args) => { OnExiting(this, args); };

            form.FormClosing += OnFormClosing;
            form.DragEnter += dragEnter;
            form.DragDrop += dragDrop;
        }

        public override void Load()
        {
            base.Load();

            Scheduler = new Scheduler();

            Resources = new ResourceStore<byte[]>();
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources"));
            Resources.AddStore(new DllResourceStore(MainResourceFile));

            Textures = Textures = new TextureStore(new NamespacedResourceStore<byte[]>(Resources, @"Textures"));

            Audio = new AudioManager(new NamespacedResourceStore<byte[]>(Resources, @"Tracks"), new NamespacedResourceStore<byte[]>(Resources, @"Samples"));

            Shaders = new ShaderManager(new NamespacedResourceStore<byte[]>(Resources, @"Shaders"));

            Fonts = new TextureStore(new GlyphStore(Game.Resources, @"Fonts/OpenSans")) { ScaleAdjust = 0.2f };

            AddProcessingContainer(new UserInputManager());
        }

        protected override void Update()
        {
            clock.ProcessFrame();
            Scheduler.Update();
            Audio.Update();

            base.Update();
        }

        private void dragDrop(object sender, DragEventArgs e)
        {
            Array fileDrop = e.Data.GetData(DataFormats.FileDrop) as Array;
            string textDrop = e.Data.GetData(DataFormats.Text) as string;

            if (fileDrop != null)
            {
                for (int i = 0; i < fileDrop.Length; i++)
                    OnDroppedFile(fileDrop.GetValue(i).ToString());
            }

            if (!string.IsNullOrEmpty(textDrop))
                OnDroppedText(textDrop);
        }

        private void dragEnter(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
            bool isUrl = e.Data.GetDataPresent(DataFormats.Text);
            e.Effect = isFile || isUrl ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        /// Whether the Game environment is active (in the foreground).
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            private set
            {
                if (value == isActive)
                    return;
                isActive = value;

                if (isActive)
                    OnActivated();
                else
                    OnDeactivated();
            }
        }

        public void Exit()
        {
            host.Exit();
        }

        protected virtual void OnDroppedText(string text)
        {
        }

        protected virtual void OnDroppedFile(string file)
        {
        }

        protected virtual void OnFormClosing(object sender, FormClosingEventArgs args)
        {
        }

        protected virtual void OnDragEnter(object sender, EventArgs args)
        {
        }

        protected virtual void OnActivated()
        {
            
        }

        protected virtual void OnDeactivated()
        {
        }

        protected virtual void OnExiting(object sender, EventArgs args)
        {
        }

        /// <summary>
        /// Called before a frame cycle has started (Update and Draw).
        /// </summary>
        protected virtual void PreFrame()
        {
        }

        /// <summary>
        /// Called after a frame cycle has been completed (Update and Draw).
        /// </summary>
        protected virtual void PostFrame()
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Scheduler?.Dispose();
            Scheduler = null;
            Audio?.Dispose();
            Audio = null;
        }
    }
}
