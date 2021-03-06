﻿using SFML.Graphics;
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
        int qtResolution = 6;
        const float qtMultiplier = 4f;
        Dictionary<AABB2i, QuadData> rects;

        int selectionRadius = 40;
        int selection = 0;
        List<Tuple<string, Color>> selections;
        Text selectionText;
        Text radiusText;
        Text helpText;
        Text resolutionText;
        List<List<RegionQuadtree<Color>>> lastRegions;

        VertexArray quadVertexArray = new VertexArray(PrimitiveType.Quads);
        VertexArray outlineVertexArray = new VertexArray(PrimitiveType.Lines);
        List<uint> freeQuadIndexes = new List<uint>();
        List<uint> freeOutlineIndexes = new List<uint>();
        Vector2f position;

        public Example()
        {
            rw = new RenderWindow(new VideoMode(1600u, 900u), "Quadtree example", Styles.Close, new ContextSettings() { AntialiasingLevel = 8 });
            rw.Closed += (s, a) => rw.Close();

            //var view = rw.GetView();
            //view.Move(new Vector2f(512f, 256f));
            //rw.SetView(view);

            initQuadtree();
            initInput();

            position = getGUIPos(0.3f, 0.5f) - new Vector2f(quadtree.AABB.Width / 2f, quadtree.AABB.Height / 2f) * qtMultiplier;

            for (int i = 0; i < 1; i++)
            {
               // quadtree.ExpandFromCenter();
            }

            lastRegions = quadtree.FindConnectedComponents();
            lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v1.Count.CompareTo(v2.Count)));
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
                bool anyInput = Mouse.IsButtonPressed(Mouse.Button.Left) || Mouse.IsButtonPressed(Mouse.Button.Right);
                if (anyInput)
                {
                    var worldMouse = rw.MapPixelToCoords(Mouse.GetPosition(rw)) - position;
                    var sfmlPos = worldMouse * (1f / qtMultiplier);
                    var aabbMin = (worldMouse - new Vector2f(selectionRadius, selectionRadius)) * (1f / qtMultiplier);
                    var aabbMax = (worldMouse + new Vector2f(selectionRadius, selectionRadius)) * (1f / qtMultiplier);
                    var qtAABB = new AABB2i(new Point2i((int)aabbMin.X, (int)aabbMin.Y), new Point2i((int)aabbMax.X, (int)aabbMax.Y));
                    var qtPos = new Point2i((int)sfmlPos.X, (int)sfmlPos.Y);

                    bool anyChanged = false;
                    if (Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        //quadtree.Set(qtPos, selections[selection].Item2);
                        anyChanged |= quadtree.SetCircle(qtPos, (int)(selectionRadius / qtMultiplier), selections[selection].Item2);
                        //quadtree.SetAABB(qtAABB, selections[selection].Item2);
                    }
                    else if (Mouse.IsButtonPressed(Mouse.Button.Right))
                    {
                        //quadtree.Unset(qtPos);
                        anyChanged |= quadtree.UnsetCircle(qtPos, (int)(selectionRadius / qtMultiplier));
                    }

                    if (anyChanged)
                    {
                        lastRegions = quadtree.FindConnectedComponents();
                        lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v2.Count.CompareTo(v1.Count)));

                        StringBuilder sb = new StringBuilder();
                        sb.Append("{ ");
                        for (int i = 0; i < lastRegions.Count; i++)
                        {
                            sb.Append(lastRegions[i].Count);
                            sb.Append(", ");
                        }
                        sb.Append(" }");
                        Debug.WriteLine(sb.ToString());
                    }
                }
                
                rw.Clear();

                // Draw

                // Draw quadtree
                var states = RenderStates.Default;
                states.Transform.Translate(position);

                rw.Draw(quadVertexArray, states);
                rw.Draw(outlineVertexArray, states);

                for (int i = 0; i < lastRegions.Count; i++)
                {
                    var maxAABB = new AABB2i(new Point2i(), new Point2i());
                    for (int j = 0; j < lastRegions[i].Count; j++)
                    {
                        if (lastRegions[i][j].AABB.Width * lastRegions[i][j].AABB.Height > maxAABB.Width * maxAABB.Height)
                        {
                            maxAABB = lastRegions[i][j].AABB;
                        }
                    }

                    var center = new Vector2f(
                        maxAABB.LowerBound.X + maxAABB.Width / 2f,
                        maxAABB.LowerBound.Y + maxAABB.Height / 2f) * qtMultiplier;
                    var text = new Text((i + 1).ToString(), fontBold, 17u);
                    text.Position = center - new Vector2f(text.GetGlobalBounds().Width / 2f, text.GetGlobalBounds().Height / 1.4f);
                    text.Color = Color.Black;
                    rw.Draw(text, states);
                }

                // For jonah
                var outerRect = new RectangleShape(new Vector2f(quadtree.AABB.Width * qtMultiplier, quadtree.AABB.Height * qtMultiplier));
                outerRect.Position = position;
                outerRect.FillColor = Color.Transparent;
                outerRect.OutlineColor = new Color(255, 255, 255, 40);
                outerRect.OutlineThickness = 3f;
                rw.Draw(outerRect);

                // Draw help texts
                var view = rw.GetView();
                rw.SetView(rw.DefaultView);

                var sel = selections[selection];
                selectionText.DisplayedString = sel.Item1;
                selectionText.Color = sel.Item2;
                rw.Draw(selectionText);
                rw.Draw(radiusText);
                rw.Draw(resolutionText);
                rw.Draw(helpText);

                rw.SetView(view);

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
            quadtree.AutoExpand = true;

            rects = new Dictionary<AABB2i, QuadData>();

            Action<object, RegionQuadtree<Color>.QuadEventArgs<Color>> onQuadAdded = (s, a) =>
            {
                var aabb = a.AABB;
                var quadColor = a.Value;
                var outlineColor = new Color(0, 0, 0, 100);

                var rectPoints = new List<Vector2f>
                {
                    new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y) * qtMultiplier,
                    new Vector2f(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y) * qtMultiplier,
                    new Vector2f(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y + aabb.Height) * qtMultiplier,
                    new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y + aabb.Height) * qtMultiplier,
                };

                const float outlineIntend = 1f;
                var outlinePoints = new List<Vector2f>
                {
                    rectPoints[0] + new Vector2f(outlineIntend, outlineIntend),
                    rectPoints[1] + new Vector2f(-outlineIntend, outlineIntend),
                    rectPoints[2] + new Vector2f(-outlineIntend, -outlineIntend),
                    rectPoints[3] + new Vector2f(outlineIntend, -outlineIntend),
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

                outlineVertexArray[quadData.outlineIndex] = new Vertex(outlinePoints[0] + new Vector2f(-outlineIntend, 0f), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 1] = new Vertex(outlinePoints[1] + new Vector2f(-outlineIntend, 0f), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 2] = new Vertex(outlinePoints[1] + new Vector2f(0f, -outlineIntend), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 3] = new Vertex(outlinePoints[2] + new Vector2f(0f, -outlineIntend), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 4] = new Vertex(outlinePoints[2] + new Vector2f(outlineIntend, 0f), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 5] = new Vertex(outlinePoints[3] + new Vector2f(outlineIntend, 0f), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 6] = new Vertex(outlinePoints[3] + new Vector2f(0f, outlineIntend), outlineColor);
                outlineVertexArray[quadData.outlineIndex + 7] = new Vertex(outlinePoints[0] + new Vector2f(0f, outlineIntend), outlineColor);

                rects.Add(aabb, quadData);
            };
            quadtree.OnQuadAdded += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadAdded);

            Action<object, RegionQuadtree<Color>.QuadEventArgs<Color>> onQuadRemoving = (s, a) =>
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
            quadtree.OnQuadRemoving += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadRemoving);

            Action<object, RegionQuadtree<Color>.QuadChangedEventArgs<Color>> onQuadChanged = (s, a) =>
            {
                var quadData = rects[a.AABB];
                var quadColor = a.Value;
                var outlineColor = new Color(0, 0, 0, 100);

                for (uint i = 0; i < 4; i++)
                {
                    quadVertexArray[quadData.quadIndex + i] = new Vertex(quadVertexArray[quadData.quadIndex + i].Position, quadColor);
                }

                for (uint i = 0; i < 8; i++)
                {
                    outlineVertexArray[quadData.outlineIndex + i] = new Vertex(outlineVertexArray[quadData.outlineIndex + i].Position, outlineColor);
                }
            };
            quadtree.OnQuadChanged += new EventHandler<RegionQuadtree<Color>.QuadChangedEventArgs<Color>>(onQuadChanged);

            Action<object, RegionQuadtree<Color>.QuadExpandEventArgs<Color>> onExpand = (s, a) =>
            {
                var oldRoot = quadtree;
                var newRoot = a.NewRoot;

                this.quadtree = newRoot;

                qtResolution++;

                position -= new Vector2f(a.Offset.X, a.Offset.Y) * qtMultiplier;

                resolutionText.DisplayedString = "Resolution " + quadtree.AABB.Width * qtMultiplier + "x" + quadtree.AABB.Height * qtMultiplier;
            };
            quadtree.OnExpand += new EventHandler<RegionQuadtree<Color>.QuadExpandEventArgs<Color>>(onExpand);

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
                        lastRegions = quadtree.FindConnectedComponents();
                        lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v1.Count.CompareTo(v2.Count)));
                        break;
                    }
                    case Keyboard.Key.E:
                    {
                        if (qtResolution > 18)
                        {
                            break;
                        }

                        quadtree.ExpandFromCenter();
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
            selectionText.Position = getGUIPos(0.03f, 0.03f);

            radiusText = new Text("Radius " + selectionRadius, fontBold, 32u);
            radiusText.Position = getGUIPos(0.3f, 0.03f);

            resolutionText = new Text("Resolution " + quadtree.AABB.Width * qtMultiplier + "x" + quadtree.AABB.Height * qtMultiplier, fontBold, 32u);
            resolutionText.Position = getGUIPos(0.6f, 0.03f);

            helpText = new Text("", fontNormal, 20u);
            helpText.Position = getGUIPos(0.6f, 0.1f);
            helpText.DisplayedString = "Use numbers 1 - 8 to select a color.\n\nLeft click to place current color.\n\nRight click to remove.\n\nMouse wheel to change radius.\n\nR to reset.";
        }

        private Vector2f getGUIPos(float x, float y)
        {
            return new Vector2f(rw.Size.X * x, rw.Size.Y * y);
        }
    }
}
