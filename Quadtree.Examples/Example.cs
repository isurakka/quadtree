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
        const int qtResolution = 6;
        const float qtMultiplier = 8f;
        Dictionary<AABB2i, QuadData> rects;

        int selection = 0;
        List<Tuple<string, Color>> selections;
        Text selectionText;
        Text helpText;

        VertexArray quadVertexArray = new VertexArray(PrimitiveType.Quads);
        VertexArray outlineVertexArray = new VertexArray(PrimitiveType.Lines);
        List<uint> freeQuadIndexes = new List<uint>();
        List<uint> freeOutlineIndexes = new List<uint>(); 

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

                rw.Draw(quadVertexArray);
                rw.Draw(outlineVertexArray);

                rw.Display();
            }
        }

        struct QuadData
        {
            public uint quadIndex;
            public uint outlineIndex;
        }

        private void initQuadtree()
        {
            quadtree = new RegionQuadtree<Color>(qtResolution);

            rects = new Dictionary<AABB2i, QuadData>();

            quadtree.OnQuadAdded += (s, a) =>
            {
                var aabb = a.AABB;
                var pos = new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y) * qtMultiplier;
                var size = new Vector2f(aabb.Width, aabb.Height) * qtMultiplier;
                var rect = new RectangleShape(size);
                var quadColor = a.Value;
                var outlineColor = new Color(0, 0, 0, 80);

                var rectPoints = new List<Vector2f>
                {
                    new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y) * qtMultiplier,
                    new Vector2f(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y) * qtMultiplier,
                    new Vector2f(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y + aabb.Height) * qtMultiplier,
                    new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y + aabb.Height) * qtMultiplier,
                };

                var outlinePoints = new List<Vector2f>
                {
                    rectPoints[0] + new Vector2f(0.5f, 0.5f),
                    rectPoints[1] + new Vector2f(-0.5f, 0.5f),
                    rectPoints[2] + new Vector2f(-0.5f, -0.5f),
                    rectPoints[3] + new Vector2f(0.5f, -0.5f),
                };

                var quadData = new QuadData();

                // quad
                if (freeQuadIndexes.Count > 0)
                {
                    quadData.quadIndex = freeQuadIndexes[freeQuadIndexes.Count - 1];
                    freeQuadIndexes.RemoveAt(freeQuadIndexes.Count - 1);
                }
                else
                {
                    quadData.quadIndex = quadVertexArray.VertexCount;
                    quadVertexArray.Resize(quadVertexArray.VertexCount + 4);
                }

                for (uint i = 0; i < 4; i++)
                {
                    quadVertexArray[quadData.quadIndex + i] = new Vertex(rectPoints[(int)i], quadColor);
                }

                // outline
                if (freeOutlineIndexes.Count > 0)
                {
                    quadData.outlineIndex = freeOutlineIndexes[freeOutlineIndexes.Count - 1];
                    freeOutlineIndexes.RemoveAt(freeOutlineIndexes.Count - 1);
                }
                else
                {
                    quadData.outlineIndex = outlineVertexArray.VertexCount;
                    outlineVertexArray.Resize(outlineVertexArray.VertexCount + 8);
                }

                outlineVertexArray[quadData.outlineIndex] = new Vertex(outlinePoints[0], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 1] = new Vertex(outlinePoints[1], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 2] = new Vertex(outlinePoints[1], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 3] = new Vertex(outlinePoints[2], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 4] = new Vertex(outlinePoints[2], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 5] = new Vertex(outlinePoints[3], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 6] = new Vertex(outlinePoints[3], outlineColor);
                outlineVertexArray[quadData.outlineIndex + 7] = new Vertex(outlinePoints[0], outlineColor);

                rects.Add(aabb, quadData);
            };

            quadtree.OnQuadRemoving += (s, a) =>
            {
                var quadData = rects[a.AABB];

                // quad
                freeQuadIndexes.Add(quadData.quadIndex);
                for (uint i = 0; i < 4; i++)
                {
                    quadVertexArray[quadData.quadIndex + i] = new Vertex(new Vector2f(), Color.Transparent);
                }

                // outline
                freeOutlineIndexes.Add(quadData.outlineIndex);
                for (uint i = 0; i < 8; i++)
                {
                    outlineVertexArray[quadData.outlineIndex + i] = new Vertex(new Vector2f(), Color.Transparent);
                }

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
                        quadtree.Set(Color.White);
                        break;
                    }
                }
            };

            selectionText = new Text("", fontBold, 32u);
            selectionText.Position = new Vector2f(0f, -40f);

            helpText = new Text("", fontNormal, 20u);
            helpText.Position = new Vector2f(512 + 40, -40f + 34f);
            helpText.DisplayedString = "Use numbers 1 - 8 to select a color.\n\nLeft click to place current color.\n\nRight click to remove.\n\nR to reset.";
        }
    }
}
