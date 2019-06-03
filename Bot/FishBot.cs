﻿using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace FishingFun
{
    public class FishingBot
    {
        public static ILog logger = LogManager.GetLogger("Fishbot");

        private ConsoleKey castKey;
        private IBobberFinder bobberFinder;
        private IBiteWatcher biteWatcher;
        private bool isEnabled;
        private bool isRunning = false;

        public FishingBot(IBobberFinder bobberFinder, IBiteWatcher biteWatcher, ConsoleKey castKey)
        {
            this.bobberFinder = bobberFinder;
            this.biteWatcher = biteWatcher;
            this.castKey = castKey;

            logger.Info("FishBot cstr.");
        }

        public void Start()
        {
            isEnabled = true;
            isRunning = true;

            while (isEnabled)
            {
                try
                {
                    logger.Info($"Pressing key {castKey} to Cast.");

                    WowProcess.PressKey(castKey);
                    Sleep(2000);
                    WaitForBite();
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                }
            }

            isRunning = false;
            logger.Error("Bot has Stopped.");
        }

        public void Stop()
        {
            isEnabled = false;
            logger.Error("Bot is Stopping...");
        }

        private void WaitForBite()
        {
            bobberFinder.Reset();

            var bobberPosition = FindBobber();
            if (bobberPosition == Point.Empty)
            {
                return;
            }

            this.biteWatcher.Reset(bobberPosition);

            // reposition mouse to indicate where the bobber is close to
            //System.Windows.Forms.Cursor.Position = new Point(bobberPosition.X, bobberPosition.Y + 50);

            logger.Info("Bobber start position: " + bobberPosition);

            var timedTask = new TimedAction((a) => { logger.Info("Fishing timed out!"); }, 25*1000, 25);

            // Wait for the bobber to move
            while (true)
            {
                var currentBobberPosition = FindBobber();
                if (currentBobberPosition == Point.Empty || currentBobberPosition.X == 0) { return; }

                if (this.biteWatcher.IsBite(currentBobberPosition))
                {
                    Loot(bobberPosition);
                    return;
                }

                if (!timedTask.ExecuteIfDue()) { return; }
            }
        }

        private static void Loot(Point bobberPosition)
        {
            System.Windows.Forms.Cursor.Position = bobberPosition;

            Sleep(1500);
            logger.Info($"Right clicking mouse to Loot.");
            WowProcess.RightClickMouse();
            System.Windows.Forms.Cursor.Position = (new Point(200, 200));
            Sleep(1000);
        }

        public static void Sleep(int ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMilliseconds < ms)
            {
                FlushBuffers();
                System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new ThreadStart(delegate { }));
                Thread.Sleep(100);
            }
        }

        public static void FlushBuffers()
        {
            ILog log = LogManager.GetLogger("Fishbot");
            var logger = log.Logger as Logger;
            if (logger != null)
            {
                foreach (IAppender appender in logger.Appenders)
                {
                    var buffered = appender as BufferingAppenderSkeleton;
                    if (buffered != null)
                    {
                        buffered.Flush();
                    }
                }
            }
        }

        private Point FindBobber()
        {
            var timer = new TimedAction((a) => { logger.Info("Waited seconds for target: " + a.ElapsedSecs); }, 1000, 5);

            while (true)
            {
                var target = this.bobberFinder.Find();
                if (target != Point.Empty || !timer.ExecuteIfDue()) { return target; }
            }
        }
    }
}