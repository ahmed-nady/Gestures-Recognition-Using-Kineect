using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCT.NUI.Core;
using CCT.NUI.HandTracking;
using OpenNI;

using System.Runtime.InteropServices;
using System.Windows.Forms;


using Recognizer.NDollar;
namespace CCT.NUI.Samples.ImageManipulation
{
    public class HandTracker
    {
        private bool isDragging = false;
        private bool isResizing = false;

        private Point startDragPoint;
        private Point startDragPoint2;

        private InteractiveImage hoveredImage;

        private HandData handData;


        List<PointR> TargetsListofPoints = new List<PointR>();
        List<PointR>  twoFingerPoints = new List<PointR>();


        int missedSuccessiveFrame = 0;
        string lastRecognizedGesture = "";

        public HandTracker(HandData handData)
        {
            this.handData = handData;
        }

        public int Id { get { return this.handData.Id; } }

        public void SetHandData(HandData newData)
        {
            this.handData = newData;
        }
        public InteractiveImage HoveredImage 
        {
            get { return this.hoveredImage; }
        }

        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        public void controlModel(String gesture, String commands)
        {
            // Get a handle to the ImageJ 3D Viewer application. The window class 
            // and window name were obtained using the Spy++ tool.
            // IntPtr calculatorHandle = FindWindow("CalcFrame", "Calculator");
            IntPtr ImageJHandle = FindWindow("SunAwtFrame", "ImageJ 3D Viewer");


            // Verify that ImageJ 3D Viewer is a running process. 
            if (ImageJHandle == IntPtr.Zero)
            {
                System.Windows.Forms.MessageBox.Show("ImageJ 3D Viewer is not running.");
                return;
            }
            //  if(gesture =="zoom")
            //       runCommand("/c java -jar D:\\Working\\ImageJ\\ij.jar -macro rotate");
            //// Make ImageJ 3D Viewer the foreground application and send it  
            // a set of commands.
            SetForegroundWindow(ImageJHandle);

            // SendKeys.SendWait("{NUMLOCK}");
            //arrow right {RIGHT} ,arrow left {LEFT} {DOWN} {UP} for rotation
            //+{LEFT} for translation
            // for (int i = 0; i < 20; i++)
            //     SendKeys.SendWait(commands);
            SendKeys.SendWait(commands);


        }
        public void HandleTranslation(InteractiveImage image, float zoomFactory)
        {
            this.hoveredImage = image;
            hoveredImage.Hovered = true;

            if (isResizing)
            {
                return;
            }
            var handClosed = handData.FingerCount <= 1;
            if (isDragging)
            {

               


              ////  hoveredImage.Translate((handData.PalmPoint.Value.X - startDragPoint.X) * zoomFactory, (handData.PalmPoint.Value.Y - startDragPoint.Y) * zoomFactory);
              //  //deteremine the direction of translation
              //  float dx = (handData.PalmPoint.Value.X - startDragPoint.X);
              //  float dy = (handData.PalmPoint.Value.Y - startDragPoint.Y);
              //  //move from right to left in x direction ,top to down in y direction
              //  if (dx > 0 && dy > 0)
              //  {
              //      controlModel("translate", "+{LEFT 3}");
              //      controlModel("translate", "+{DOWN 3}");

              //  }
              //  else if (dx > 0 && dy <0)
              //  {
              //      controlModel("translate", "+{LEFT 3}");
              //      controlModel("translate", "+{UP 3}");
              //  }
              //  else if (dx < 0 && dy < 0)
              //  {
              //      controlModel("translate", "+{RIGHT 3}");
              //      controlModel("translate", "+{UP 3}");
              //  }
              //  else
              //  {
              //      controlModel("translate", "+{RIGHT 10}");
              //      controlModel("translate", "+{DOWN 10}");
              //  }

            }
            if (handClosed)
            {
                startDragPoint = new Point(handData.FingerPoints[0].Location.X, handData.FingerPoints[0].Location.Y, 0);
                TargetsListofPoints.Add(new PointR(handData.FingerPoints[0].X, handData.FingerPoints[0].Y, Environment.TickCount));
                    
            }
            isDragging = handClosed;
        }

        private void recognizeGestureUsingNRecognizer(GeometricRecognizer recognizser, List<PointR> TargetsListofPoints)
        {

            NBestList result = recognizser.Recognize(TargetsListofPoints, 1);
            if (result.Score > .7)
            {
                if (result.Name.StartsWith("TranslateRight"))
                {
                    controlModel("translate", "+{RIGHT 10}");
                    lastRecognizedGesture = "TranslateRight";
                }
                else if (result.Name.StartsWith("TranslateLeft"))
                {
                    controlModel("translate", "+{LEFT 10}");
                    lastRecognizedGesture = "TranslateLeft";
                }
                else if (result.Name.StartsWith("ZoomIn"))
                {
                    controlModel("Zoom", "%{UP 10}");
                    lastRecognizedGesture = "ZoomIn";
                }
                else if (result.Name.StartsWith("ZoomOut"))
                {
                    controlModel("Zoom", "%{DOWN 10}");
                    lastRecognizedGesture = "ZoomOut";
                }
                else if (result.Name.StartsWith("RotateLeft"))
                {
                    controlModel("Rotate", "{LEFT 10}");
                    lastRecognizedGesture = "RotateLeft";
                }
                else if (result.Name.StartsWith("RotateRight"))
                {
                    controlModel("Rotate", "{RIGHT 10}");
                    lastRecognizedGesture = "RotateRight";
                }
                else if (result.Name.StartsWith("ZoomIn2Fingers"))
                {
                    controlModel("Zoom", "%{UP 10}");
                }
                else if (result.Name.StartsWith("ZoomOut2Fingers"))
                {
                    controlModel("Zoom", "%{DOWN 10}");
                }


                TargetsListofPoints.Clear();

            }
        }
     

        public void HandleTranslation(GeometricRecognizer rec)
        {
            
            if (isResizing)
            {
                return;
            }
            var handWithSingleFinger = handData.FingerCount == 1;
            missedSuccessiveFrame = handWithSingleFinger ? 0 : missedSuccessiveFrame++;

            if (missedSuccessiveFrame > 10 )//&& TargetsListofPoints.Count <20)
            {
                TargetsListofPoints.Clear();
                missedSuccessiveFrame = 0;
            }

            if (handWithSingleFinger)
            {
                TargetsListofPoints.Add(new PointR(handData.FingerPoints[0].X, handData.FingerPoints[0].Y, Environment.TickCount));
                //TargetsListofPoints.Add(new PointR(handData.PalmX, handData.PalmY, Environment.TickCount));
            }
                // recognize gesture when user pause for 10 ms
                if (handWithSingleFinger==false && TargetsListofPoints.Count > 30)
                {
                        recognizeGestureUsingNRecognizer(rec,TargetsListofPoints);
                        TargetsListofPoints.Clear();
                }
            if(handData.FingerCount >= 2)
                performLastGestureNumberOfTimes();

            }
            //if (handWithSingleFinger)
            //{
            //    startDragPoint = new Point(handData.FingerPoints[0].Location.X, handData.FingerPoints[0].Location.Y, 0);
            //    TargetsListofPoints.Add(new PointR(handData.FingerPoints[0].X, handData.FingerPoints[0].Y, Environment.TickCount));

            //   // startDragPoint = new Point(handData.PalmX, handData.PalmY, 0);
            //  //  TargetsListofPoints.Add(new PointR(handData.PalmX, handData.PalmY, Environment.TickCount));
            
            //}
            //isDragging = handWithSingleFinger;
       // }

        public float distanceEuclidean(Point a, Point b)
        {
            float d = (float)Math.Sqrt(Math.Abs(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
            return d;
        }
        //perform the last gesture #of times ==fingertips.count
        public void performLastGestureNumberOfTimes()
        {
            if (handData.FingerCount >= 2)
            {
                switch (lastRecognizedGesture)
                {
                    case "TranslateLeft": { controlModel("translate", "+{LEFT "+handData.FingerCount+"}"); break; }
                    case "TranslateRight": { controlModel("translate", "+{RIGHT " +  handData.FingerCount + "}"); break; }
                    case "RotateLeft": { controlModel("Rotate", "{LEFT "+  handData.FingerCount + "}"); break; }
                    case "RotateRight": { controlModel("Rotate", "{RIGHT " +  handData.FingerCount + "}"); break; }
                    case "ZoomIn": {  controlModel("Rotate", "%{UP " +  handData.FingerCount + "}"); break; }
                    case "ZoomOut": { controlModel("Rotate", "%{DOWN " +  handData.FingerCount + "}"); break; } 
                    default: break;
                }
            }
        }
        public void ResizeSingleHand(GeometricRecognizer rec)
        {
            if (handData.FingerCount == 2)
            {
                twoFingerPoints.Add(new PointR(handData.FingerPoints[0].X, handData.FingerPoints[0].Y, Environment.TickCount));
                twoFingerPoints.Add(new PointR(handData.FingerPoints[1].X, handData.FingerPoints[1].Y, Environment.TickCount));

              //  this.HandleResize(handData.FingerPoints[0].Location, handData.FingerPoints[1].Location);
                this.isResizing = true;
            }
            else
            {
                this.isResizing = false;
            }

            // recognize gesture when user pause for 10 ms
            if (isResizing == false && twoFingerPoints.Count > 20)
            {
                recognizeGestureUsingNRecognizer(rec, twoFingerPoints);
                twoFingerPoints.Clear();

            } 
        }

        public void ResizeTwoHands(HandTracker otherHand)
        {
            if (!handData.HasPalmPoint || !otherHand.handData.HasPalmPoint)
            {
                return;
            }
            if (handData.FingerCount <= 1 && otherHand.handData.FingerCount <= 1)
            {
                this.HandleResize(handData.PalmPoint.Value, otherHand.handData.PalmPoint.Value);
                this.isResizing = true;
            }
            else
            {
                this.isResizing = false;
            }
        }

        private void HandleResize(Point p1, Point p2)
        {
            if (isResizing)
            {
                hoveredImage.Resize(startDragPoint, startDragPoint2, p1, p2);
            }
            startDragPoint = p1;
            startDragPoint2 = p2;
        }

        public bool IsOverImage { get { return this.hoveredImage != null; } }
    }
}
