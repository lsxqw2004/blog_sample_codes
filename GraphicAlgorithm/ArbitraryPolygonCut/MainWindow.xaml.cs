using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DefaultBtnBackground = ButtonDrawS.Background;
            LoadFileList();

            WindowLog.Default.WhenLogCommon(log =>
            {
                LogTextbox.Text = LogTextbox.Text + (log + Environment.NewLine);
            });
            WindowLog.Default.WhenAddLabel((c, p) =>
            {
                var label = new Label() { Content = c, IsHitTestVisible = false };
                Canvas.SetTop(label, p.Y - 20);
                Canvas.SetLeft(label, p.X - 25);
                Canvas.Children.Add(label);
            });
            WindowLog.Default.WhenReversePolygon(()=>_polyC.ArrowReverse=true);


            Canvas.Children.Add(_cross);
        }

        private const double Near = 3;

        private bool _drawingPolygonS = false;
        private bool _drawingPolygonC = false;

        private int _polygonSVertexNum { get { return _polyS.PointList.Count; } }
        private int _polygonCVertexNum { get { return _polyC.PointList.Count; } }

        private Brush DefaultBtnBackground;

        private MyPolygon _polyS = new MyPolygon()
        {
            StrokeColor = Colors.CadetBlue,
            StrokeThickess = 1
        };

        private MyPolygon _polyC = new MyPolygon()
        {
            StrokeColor = Colors.Chocolate,
            StrokeThickess = 1
        };

        private CrossLine _cross = new CrossLine()
        {
            Visible = false,
            Stroke = new SolidColorBrush(Colors.Red),
            CrossRadius = 5,
            StrokeThickness = 1
        };

        private void LoadFileList()
        {
            var dir = Path.Combine(Environment.CurrentDirectory, "CutFile");
            var files = Directory.GetFiles(dir).ToList();
            files.ForEach(f =>
            {
                var fi = new FileInfo(f);
                var selectItem = new ComboBoxItem()
                {
                    Text = fi.Name.Split('.')[0],
                    Value = fi.FullName
                };
                LoadBox.Items.Add(selectItem);
            });
        }



        private void DrawAPoint(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(Canvas);

            if (_drawingPolygonS)
            {
                if (_polygonSVertexNum == 0)
                {
                    _polyS.PointList.Add(new Vertex(point.X, point.Y) { Name = "S" + (_polygonSVertexNum + 1) });
                }
                _polyS.PointList.Add(new Vertex(point.X, point.Y) { Name = "S" + (_polygonSVertexNum + 1) });
                _polyS.Render(Canvas);
            }
            if (_drawingPolygonC)
            {
                if (_polygonCVertexNum == 0)
                {
                    _polyC.PointList.Add(new Vertex(point.X, point.Y) { Name = "C" + (_polygonCVertexNum + 1) });
                }
                _polyC.PointList.Add(new Vertex(point.X, point.Y) { Name = "C" + (_polygonCVertexNum + 1) });
                _polyC.Render(Canvas);
            }
        }

        private void BeginPolygonS(object sender, RoutedEventArgs e)
        {
            Status.Text = "正在绘制实体多边形";
            ResetButton();
            ButtonDrawS.Background = new SolidColorBrush(Colors.CadetBlue);
            _drawingPolygonS = true;
            _drawingPolygonC = false;
        }

        private void BeginPolygonC(object sender, RoutedEventArgs e)
        {
            Status.Text = "正在绘制切割多边形";
            ResetButton();
            ButtonDrawC.Background = new SolidColorBrush(Colors.Chocolate);
            _drawingPolygonC = true;
            _drawingPolygonS = false;
        }

        private void ResetButton()
        {
            ButtonDrawS.Background = DefaultBtnBackground;
            ButtonDrawC.Background = DefaultBtnBackground;
            ButtonCut.Background = DefaultBtnBackground;
        }

        private void Cut(object sender, RoutedEventArgs e)
        {
            Status.Text = "裁剪结果";
            ResetButton();
            ButtonCut.Background = new SolidColorBrush(Colors.DodgerBlue);

            var polylineLst = ArbitraryPolygonCut.Cut(_polyS.PointList, _polyC.PointList);

            if (polylineLst.Count == 0)
            {
                MessageBox.Show("没有交点");
                return;
            }
            foreach (var pointLst in polylineLst)
            {
                var polyResult = new MyPolygon(RenderType.Polygon)
                {
                    StrokeColor = Colors.Transparent,
                    StrokeThickess = 0,
                    FillColor = Color.FromArgb(80, 30, 144, 255),
                    Filled = true
                };
                polyResult.PointList.AddRange(pointLst);
                polyResult.Render(Canvas);
            }
        }

        private void MoveOver(object sender, MouseEventArgs e)
        {
            _cross.Visible = false;

            var point = e.GetPosition(Canvas);
            PointVec.Text = "坐标位置：" + point.X + "," + point.Y;
            var newVector = new Vertex(point.X, point.Y);

            if (Keyboard.IsKeyDown(Key.LeftShift) && (_drawingPolygonC || _drawingPolygonS))
            {
                VertexBase pointPre = null;
                if (_drawingPolygonS)
                    pointPre = _polyS.PointList[_polygonSVertexNum - 2];
                else if (_drawingPolygonC)
                    pointPre = _polyC.PointList[_polygonCVertexNum - 2];
                var dx = pointPre.X - point.X;
                double tan = 0;
                if (dx != 0)
                {
                    var dy = pointPre.Y - point.Y;
                    tan = Math.Abs(dy / dx);
                }
                else
                {
                    tan = double.MaxValue;
                }
                if (tan <= 1)
                    newVector = new Vertex(point.X, (double)pointPre.Y);
                else if (tan > 1)
                {
                    newVector = new Vertex((double)pointPre.X, point.Y);
                }
            }

            //抓去最近点
            Point nearestPoint = new Point();
            bool hasNearestSite = false;
            if (_drawingPolygonS)
                hasNearestSite = IsCloseToPolygon(point, _polyC, ref nearestPoint);
            else if (_drawingPolygonC)
                hasNearestSite = IsCloseToPolygon(point, _polyS, ref nearestPoint);

            if (hasNearestSite)
            {
                _cross.Center = nearestPoint;
                _cross.Visible = true;
                var screenPoint = Canvas.PointToScreen(nearestPoint);
                SetCursorPos((int)screenPoint.X,(int)screenPoint.Y);
            }

            if (_drawingPolygonS && _polygonSVertexNum > 0)
            {
                _polyS.PointList[_polygonSVertexNum - 1].SetXY(newVector.X, newVector.Y);
                _polyS.Render(Canvas);
            }
            if (_drawingPolygonC && _polygonCVertexNum > 0)
            {
                _polyC.PointList[_polygonCVertexNum - 1].SetXY(newVector.X, newVector.Y);
                _polyC.Render(Canvas);
            }
        }

        [DllImport("User32")]
        public extern static void SetCursorPos(int x, int y);

        private void EndPolyline(object sender, MouseButtonEventArgs e)
        {
            if (_drawingPolygonS)
            {
                if (_polygonSVertexNum < 3)
                {
                    _polyS.PointList.Clear();
                    return;
                }

                _polyS.PointList.RemoveAt(_polygonSVertexNum - 1);
                _drawingPolygonS = false;
                _polyS.Render(Canvas);
            }
            if (_drawingPolygonC)
            {
                if (_polygonCVertexNum < 3)
                {
                    _polyC.PointList.Clear();
                    return;
                }
                _polyC.PointList.RemoveAt(_polygonCVertexNum - 1);
                _drawingPolygonC = false;
                _polyC.Render(Canvas);
            }
        }


        private void Reset(object sender, RoutedEventArgs e)
        {
            _polyS.Reset();
            _polyC.Reset();
            _drawingPolygonC = false;
            _drawingPolygonS = false;
            Canvas.Children.Clear();
            ResetButton();
        }

        private void ShowDirectionChanged(object sender, RoutedEventArgs e)
        {
            if (_polyS != null && _polyS.PointList.Count > 1)
            {
                _polyS.ShowArrow = CkbShowDirection.IsChecked ?? false;
                _polyS.Render(Canvas);
            }
            if (_polyC != null && _polyC.PointList.Count > 1)
            {
                _polyC.ShowArrow = CkbShowDirection.IsChecked ?? false;
                _polyC.Render(Canvas);
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (_polyC.PointList.Count < 3 || _polyS.PointList.Count < 3)
            {
                MessageBox.Show("多边形不完整");
                return;
            }
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "源多边形|*.cut";
            saveFileDialog.Title = "保存多边形";
            saveFileDialog.InitialDirectory = Path.Combine(Environment.CurrentDirectory, "CutFile");
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName != "")
            {
                FileStream fs = (FileStream)saveFileDialog.OpenFile();

                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(JsonConvert.SerializeObject(_polyS.PointList));
                sw.WriteLine(JsonConvert.SerializeObject(_polyC.PointList));
                sw.Close();
                fs.Close();
            }
        }

        private void LoadCut(object sender, RoutedEventArgs e)
        {
            var fileName = (LoadBox.SelectionBoxItem as ComboBoxItem).Value;
            var fs = File.OpenRead(fileName);
            var sr = new StreamReader(fs);
            Reset(sender, e);
            _polyS.PointList.AddRange(JsonConvert.DeserializeObject<List<Vertex>>(sr.ReadLine()));
            _polyC.PointList.AddRange(JsonConvert.DeserializeObject<List<Vertex>>(sr.ReadLine()));
            fs.Close();
            _polyS.Render(Canvas);
            _polyC.Render(Canvas);
        }

        private bool IsCloseToPolygon(Point mousePoint, MyPolygon polygon, ref Point nearestPoint)
        {
            var pointCount = polygon.PointList.Count;
            if (pointCount < 3)
                return false;

            for (int i = 0; i < polygon.PointList.Count; i++)
            {
                var vertex1 = polygon.PointList[i% pointCount];
                var vertex2 = polygon.PointList[(i + 1)%pointCount];
                var result = IsCloseTo(mousePoint, vertex1.ToPoint(), vertex2.ToPoint(),ref nearestPoint);
                if (result)
                    return true;
            }
            return false;
        }

        private bool IsCloseTo(Point mousePoint, Point linePoint1, Point linePoint2, ref Point nearestPoint)
        {
            var mousePointVector = new Point(mousePoint.X - linePoint1.X, mousePoint.Y - linePoint1.Y);
            var lineVector = new Point(linePoint2.X - linePoint1.X, linePoint2.Y - linePoint1.Y);

            var dotProduct = mousePointVector.X * lineVector.X + mousePointVector.Y * lineVector.Y;
            var mousePointSquare = mousePointVector.X*mousePointVector.X + mousePointVector.Y*mousePointVector.Y;
            var mousePointMod = Math.Sqrt(mousePointSquare);
            var lineMod = Math.Sqrt(lineVector.X*lineVector.X + lineVector.Y*lineVector.Y);
            var modProdcut =  mousePointMod*lineMod;

            if (lineMod == 0)
                return false;

            var cos = dotProduct/(modProdcut==0?double.Epsilon:modProdcut);

            if (cos <= 0)
                return false;

            var linePartMod = cos*mousePointMod;

            if (linePartMod > lineMod)
                return false;

            var heightSquare = mousePointSquare - linePartMod * linePartMod;
            if (heightSquare > Near*Near)
                return false;
            
            var orthocenterVector = new Point(lineVector.X/lineMod*linePartMod,lineVector.Y/lineMod*linePartMod);
            nearestPoint = new Point(linePoint1.X+orthocenterVector.X,linePoint1.Y+orthocenterVector.Y);
            return true;
        }
    }

    public class ComboBoxItem
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
