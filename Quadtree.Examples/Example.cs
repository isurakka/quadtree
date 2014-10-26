using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree.Examples
{
    class Example
    {
        static void Main(string[] args)
        {
            new Example().Run();
        }

        static Font fontNormal = new Font("assets/DejaVuSans.ttf");
        static Font fontBold = new Font("assets/DejaVuSans-Bold.ttf");

        RenderWindow rw;
        RegionQuadtree<Color> quadtree;
        const float qtMultiplier = 8f;
        Dictionary<AABB2i, RectangleShape> rects;

        int selection = 0;
        List<Tuple<string, Color>> selections;
        Text selectionText;
        Text helpText;

        public Example()
        {
            rw = new RenderWindow(new VideoMode(1024u, 512u + 128u), "Quadtree example", Styles.Close, new ContextSettings() { AntialiasingLevel = 8 });
            rw.Closed += (s, a) => rw.Close();

            var view = rw.GetView();
            view.Move(new Vector2f(-60f, -60f));
            rw.SetView(view);

            initQuadtree();
            initInput();
        }

        public void Run()
        {
            var sw = new Stopwatch();

            while (rw.IsOpen())
            {
                rw.DispatchEvents();

                var dt = (float)TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds;
                sw.Restart();

                // Update
                var worldMouse = rw.MapPixelToCoords(Mouse.GetPosition(rw));
                var sfmlPos = worldMouse * (1f / qtMultiplier);
                var qtPos = new Point2i((int)sfmlPos.X, (int)sfmlPos.Y);
                if (Mouse.IsButtonPressed(Mouse.Button.Left))
                {
                    quadtree.Set(qtPos, selections[selection].Item2);
                }
                else if (Mouse.IsButtonPressed(Mouse.Button.Right))
                {
                    quadtree.Unset(qtPos);
                }

                rw.Clear();

                // Draw
                var sel = selections[selection];
                selectionText.DisplayedString = sel.Item1;
                selectionText.Color = sel.Item2;
                rw.Draw(selectionText);
                rw.Draw(helpText);

                foreach (var rect in rects)
                {
                    rw.Draw(rect.Value);
                }

                rw.Display();
            }
        }

        private void initQuadtree()
        {
            quadtree = new RegionQuadtree<Color>(6);

            rects = new Dictionary<AABB2i, RectangleShape>();

            quadtree.OnQuadAdded += (s, a) =>
            {
                var aabb = a.AABB;
                var pos = new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y) * qtMultiplier;
                var size = new Vector2f(aabb.Width, aabb.Height) * qtMultiplier;
                var rect = new RectangleShape(size);
                rect.Position = pos;
                rect.FillColor = a.Value;
                rect.OutlineThickness = -1;
                rect.OutlineColor = new Color(0, 0, 0, 60);
                rects[aabb] = rect;
                //rects.Add(aabb, rect);
            };

            quadtree.OnQuadRemoving += (s, a) =>
            {
                rects.Remove(a.AABB);
            };

            quadtree.Set(Color.White);
        }

        private void initInput()
        {
            selections = new List<Tuple<string, Color>>
            {
                new Tuple<string, Color>("Red", new Color(231, 76, 60)),
                new Tuple<string, Color>("Green", new Color(46, 204, 113)),
                new Tuple<string, Color>("Blue", new Color(52, 152, 219)),
                new Tuple<string, Color>("Yellow", new Color(241, 196, 15)),
                new Tuple<string, Color>("Turquoise", new Color(26, 188, 156)),
                new Tuple<string, Color>("Purple", new Color(155, 89, 182)),
                new Tuple<string, Color>("Orange", new Color(230, 126, 34)),
            };

            rw.KeyPressed += (s, a) =>
            {
                switch (a.Code)
                {
                    case Keyboard.Key.Num1:
                        selection = 0;
                        break;
                    case Keyboard.Key.Num2:
                        selection = 1;
                        break;
                    case Keyboard.Key.Num3:
                        selection = 2;
                        break;
                    case Keyboard.Key.Num4:
                        selection = 3;
                        break;
                    case Keyboard.Key.Num5:
                        selection = 4;
                        break;
                    case Keyboard.Key.Num6:
                        selection = 5;
                        break;
                    case Keyboard.Key.Num7:
                        selection = 6;
                        break;
                }
            };

            selectionText = new Text("", fontBold, 32u);
            selectionText.Position = new Vector2f(0f, -40f);

            helpText = new Text("", fontNormal, 20u);
            helpText.Position = new Vector2f(512 + 40, -40f + 34f);
            helpText.DisplayedString = "Use numbers 1 - 7 to select a color.\n\nLeft click to place current color.\n\nRight click to remove.";
        }
    }
}
