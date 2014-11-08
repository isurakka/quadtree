using FarseerPhysics.Dynamics;
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

        float step = 1f / 60f;
        float acc = 0f;

        RenderWindow rw;
        const int qtResolution = 7;
        const float qtMultiplier = 4f;
        Dictionary<AABB2i, QuadData> rects;

        int selectionRadius = 40;
        int selection = 0;
        List<Tuple<string, Color>> selections;
        Text selectionText;
        Text radiusText;
        Text helpText;

        World world;
        List<DestructibleBody> bodies = new List<DestructibleBody>();

        public Example()
        {
            rw = new RenderWindow(new VideoMode(1024u, 512u + 128u), "Quadtree example", Styles.Close, new ContextSettings() { AntialiasingLevel = 8 });
            rw.Closed += (s, a) => rw.Close();

            var view = rw.GetView();
            view.Move(new Vector2f(-60f, -60f));
            rw.SetView(view);

            initInput();

            world = new World(new Microsoft.Xna.Framework.Vector2(0f, 0f));
            bodies.Add(new DestructibleBody(world, qtResolution, qtMultiplier));
        }

        public void Run()
        {
            var sw = new Stopwatch();

            while (rw.IsOpen())
            {
                rw.DispatchEvents();

                var dt = (float)TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds;
                sw.Restart();

                acc += dt;

                if (acc >= step)
                {
                    acc -= step;

                    world.Step(step);
                }

                // Update
                bool anyInput = Mouse.IsButtonPressed(Mouse.Button.Left) || Mouse.IsButtonPressed(Mouse.Button.Right);
                if (anyInput)
                {
                    var worldMouse = rw.MapPixelToCoords(Mouse.GetPosition(rw));
                    var sfmlPos = worldMouse * (1f / qtMultiplier);
                    var aabbMin = (worldMouse - new Vector2f(selectionRadius, selectionRadius)) * (1f / qtMultiplier);
                    var aabbMax = (worldMouse + new Vector2f(selectionRadius, selectionRadius)) * (1f / qtMultiplier);
                    var qtAABB = new AABB2i(new Point2i((int)aabbMin.X, (int)aabbMin.Y), new Point2i((int)aabbMax.X, (int)aabbMax.Y));
                    var qtPos = new Point2i((int)sfmlPos.X, (int)sfmlPos.Y);

                    bool anyChanged = false;
                    if (Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        foreach (var item in bodies)
                        {
                            item.SetCircle(worldMouse, selectionRadius, selections[selection].Item2);
                        }
                    }
                    else if (Mouse.IsButtonPressed(Mouse.Button.Right))
                    {
                        foreach (var item in bodies)
                        {
                            item.SetCircle(worldMouse, selectionRadius, null);
                        }
                    }
                }
                
                rw.Clear();

                // Draw
                var sel = selections[selection];
                selectionText.DisplayedString = sel.Item1;
                selectionText.Color = sel.Item2;
                rw.Draw(selectionText);
                rw.Draw(radiusText);
                rw.Draw(helpText);

                foreach (var item in bodies)
                {
                    item.Draw(rw);
                }

                rw.Display();
            }
        }

        struct QuadData
        {
            public uint quadIndex;
            public uint outlineIndex;
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
                new Tuple<string, Color>("White", Color.White),
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
                    case Keyboard.Key.Num8:
                        selection = 7;
                        break;
                    case Keyboard.Key.R:
                    {
                        //quadtree.Set(Color.White);
                        break;
                    }
                }
            };

            rw.MouseWheelMoved += (s, a) =>
            {
                selectionRadius += 10 * a.Delta;
                if (selectionRadius < 0)
                    selectionRadius = 0;

                radiusText.DisplayedString = "Radius " + selectionRadius;
            };

            selectionText = new Text("", fontBold, 32u);
            selectionText.Position = new Vector2f(0f, -40f);

            radiusText = new Text("Radius " + selectionRadius, fontBold, 32u);
            radiusText.Position = new Vector2f(312f, -40f);

            helpText = new Text("", fontNormal, 20u);
            helpText.Position = new Vector2f(512 + 40, -40f + 34f);
            helpText.DisplayedString = "Use numbers 1 - 8 to select a color.\n\nLeft click to place current color.\n\nRight click to remove.\n\nMouse wheel to change radius.\n\nR to reset.";
        }
    }
}
