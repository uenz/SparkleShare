//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia;

namespace SparkleShare.UserInterface
{
    // Simple spinner control for Avalonia that mirrors the Windows Spinner behaviour
    public class Spinner : Image
    {
        private DispatcherTimer? timer;
        private CroppedBitmap[]? frames;
        private int currentFrame = 0;

        public Spinner() : this(22) { }

        public Spinner(int size) : base()
        {
            try {
                var spinnerGallery = UserInterfaceHelpers.GetImageSource("process-working-22");
                if (spinnerGallery == null)
                    return;

                var gallerySize = spinnerGallery.PixelSize;
                int framesInWidth  = gallerySize.Width / size;
                int framesInHeight = gallerySize.Height / size;
                int frameCount     = (framesInWidth * framesInHeight) - 1;

                if (frameCount <= 0)
                    return;

                frames = new CroppedBitmap[frameCount];
                int i = 0;

                for (int y = 0; y < framesInHeight; y++) {
                    for (int x = 0; x < framesInWidth; x++) {
                        // skip top-left (transparent) frame to match Windows implementation
                        if (y == 0 && x == 0)
                            continue;

                        var rect = new PixelRect(size * x, size * y, size, size);
                        frames[i++] = new CroppedBitmap(spinnerGallery, rect);
                    }
                }

                timer = new DispatcherTimer {
                    Interval = TimeSpan.FromMilliseconds(400.0 / frameCount)
                };

                timer.Tick += (sender, e) => {
                    if (frames == null || frames.Length == 0) return;

                    if (currentFrame < frames.Length - 1)
                        currentFrame++;
                    else
                        currentFrame = 0;

                    Source = frames[currentFrame];
                };

                // set initial size so layout can measure correctly
                Width  = size;
                Height = size;

            } catch (Exception) {
                // don't throw in UI construction; leave spinner empty if anything fails
            }
        }

        public void Start()
        {
            try { timer?.Start(); }
            catch (Exception) { }
        }

        public void Stop()
        {
            try { timer?.Stop(); }
            catch (Exception) { }
        }
    }
}
