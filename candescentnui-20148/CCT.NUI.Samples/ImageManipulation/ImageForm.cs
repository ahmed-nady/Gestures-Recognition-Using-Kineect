using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using CCT.NUI.Core;
using CCT.NUI.HandTracking;
using CCT.NUI.Visual;

using Recognizer.NDollar;

namespace CCT.NUI.Samples.ImageManipulation
{
    public partial class ImageForm : Form
    {
        private IList<InteractiveImage> images = new List<InteractiveImage>();
        private IList<HandTracker> handTracks = new List<HandTracker>();

        private IHandDataSource handDataSource;
        private HandLayer handLayer;


        GeometricRecognizer rec;
        enum gestureName { TranslateLeft, TranslateRight, RotateRight, RotateLeft,ZoomIn,ZoomOut };
        gestureName gesture = gestureName.TranslateLeft;
        int numOfGestures =6;
        public ImageForm()
        {
            InitializeComponent();

           // this.images = new ImageLoader(100, 100, this.Width).LoadImages();
            this.Paint += new PaintEventHandler(ImageForm_Paint);
            this.FormClosing += new FormClosingEventHandler(ImageForm_FormClosing);

            rec = new GeometricRecognizer();

            //load all template for each gesture
            for (int i = 1; i <= 10; i++)
            {
                rec.LoadGesture(@"C:\Users\ahmed nady\Documents\" + gesture + "" + i + ".xml");
                if (i == 10 && numOfGestures > 0)
                {
                   
                    i = 1;
                    numOfGestures--;
                    gesture ++;
                }
            }
            ////rec.LoadGesture(@"C:\Users\ahmed nady\Documents\TranslateRightLine.xml");
            ////rec.LoadGesture(@"C:\Users\ahmed nady\Documents\RotateLeftCircle.xml");
            ////rec.LoadGesture(@"C:\Users\ahmed nady\Documents\RotateRightPI.xml");
            ////rec.LoadGesture(@"C:\Users\ahmed nady\Documents\ZoomInCaret.xml");
            ////rec.LoadGesture(@"C:\Users\ahmed nady\Documents\ZoomOutCaret.xml");

           // rec.LoadGesture(@"C:\Users\ahmed nady\Documents\ZoomIn2Fingers.xml");
          //  rec.LoadGesture(@"C:\Users\ahmed nady\Documents\ZoomOut2Fingers.xml");
            
        }

        public ImageForm(IHandDataSource handDataSource)
            : this()
        {
            this.handDataSource = handDataSource;
            this.handDataSource.NewDataAvailable += new NewDataHandler<HandCollection>(handDataSource_NewDataAvailable);
            this.handLayer = new HandLayer(this.handDataSource);
            this.handLayer.ShowConvexHull = true;
        }



        private void ResetHands()
        {
            this.handTracks.Clear();
            this.Invalidate();
        }

        private void UpdateHandTrackData(HandCollection handData)
        {
            foreach (var newHand in handData.Hands.Where(h => !this.handTracks.Any(t => t.Id == h.Id)))
            {
                this.handTracks.Add(new HandTracker(newHand));
            }
            foreach (var handTrack in this.handTracks.ToList())
            {
                var newHand = handData.Hands.Where(h => h.Id == handTrack.Id).FirstOrDefault();
                if (newHand == null)
                {
                    this.handTracks.Remove(handTrack);
                }
                else
                {
                    if (!newHand.HasPalmPoint)
                    {
                        continue;
                    }
                    handTrack.SetHandData(newHand);

                    handTrack.HandleTranslation(rec);

                    //var hoveredImage = this.images.Where(i => this.ImageContains(i, newHand.PalmPoint.Value)).LastOrDefault();
                    //if (hoveredImage != null)
                    //{
                    //    MoveImageToFront(hoveredImage);
                    //    handTrack.HandleTranslation(hoveredImage, this.handLayer.ZoomFactor);
                    //}
                }
            }
        }                             
            
        private void MoveImageToFront(InteractiveImage image)
        {
            this.images.Remove(image);
            this.images.Add(image);
        }

        private bool ImageContains(InteractiveImage image, Point center)
        {
            return image.Area.Contains(MapPoint(center, this.handDataSource.Size));
        } 

        private void UnhoverImages()
        {
            foreach (var localImage in this.images)
            {
                localImage.Hovered = false;
            }
        }

        void handDataSource_NewDataAvailable(HandCollection handData)
        {
            if (handData.IsEmpty)
            {
                this.ResetHands();
                return;
            }

            this.UnhoverImages();
            this.UpdateHandTrackData(handData);

            var handsOverImages = this.handTracks.Where(h => h.IsOverImage);
            foreach (var handTrack in handsOverImages)
            {
                this.HandleTwoHandedActions(handTrack);
            }

            this.Invalidate();
        }

        private void HandleTwoHandedActions(HandTracker handTrack)
        {
            var otherHand = this.handTracks.Where(h => h != handTrack && h.HoveredImage == handTrack.HoveredImage).FirstOrDefault();
            if (otherHand == null)
            {
                handTrack.performLastGestureNumberOfTimes();
                //handTrack.ResizeSingleHand(rec);
            }
            else
            {
                handTrack.ResizeTwoHands(otherHand);
            }
        }

        private Point MapPoint(Point point, IntSize originalSize)
        {
            return new Point(point.X / originalSize.Width * this.ClientSize.Width, point.Y / originalSize.Height * this.ClientSize.Height, 0);
        }

        private System.Drawing.Point MapToScreen(Point point, CCT.NUI.Core.Size originalSize)
        {
            int border = 50;
            return new System.Drawing.Point(-border + (int)(point.X / originalSize.Width * (Screen.PrimaryScreen.Bounds.Width + 2 * border)), - border + (int)(point.Y / originalSize.Height * (Screen.PrimaryScreen.Bounds.Height + 2 * border)));
        }

        private void ResetImages()
        {
            foreach (var image in this.images.ToList())
            {
                image.Reset();
            }
            this.Invalidate();
        }

        void ImageForm_Paint(object sender, PaintEventArgs e)
        {
            foreach (var image in this.images.ToList())
            {
                image.Draw(e.Graphics);
            }
            this.handLayer.SetTargetSize(this.Size);
            this.handLayer.SetZoomHandFactor(1 / this.handLayer.ZoomFactor);
            this.handLayer.Paint(e.Graphics);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ResetImages();
        }

        void ImageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.handDataSource.Stop();
        }
    }
}
