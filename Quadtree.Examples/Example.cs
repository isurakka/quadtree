using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FarseerPhysics;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Transform = SFML.Graphics.Transform;

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
        RegionQuadtree<Color> renderQuadtree;
        RegionQuadtree<Color> physicsQuadtree;
        int qtResolution = 7;
        const float qtMultiplier = 8f;
        Dictionary<AABB2i, QuadData> rects;
        private Dictionary<Fixture, QuadData> qtData; 

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

        private World world;
        private Body body;
        private BatchedBodyRenderer bodyRenderer;
        
        private float step = 1f / 120f;
        private float acc = 0f;

        public Example()
        {
            rw = new RenderWindow(new VideoMode(1600u, 900u), "Quadtree example", Styles.Close, new ContextSettings() { AntialiasingLevel = 8 });
            rw.Closed += (s, a) => rw.Close();

            //var view = rw.GetView();
            //view.Move(new Vector2f(512f, 256f));
            //rw.SetView(view);

            world = new World(new Vector2());
            body = new Body(world, new Vector2());
            body.BodyType = BodyType.Dynamic;
            bodyRenderer = new BatchedBodyRenderer();

            initQuadtree();
            initInput();

            position = getGUIPos(0.3f, 0.5f) - new Vector2f(renderQuadtree.AABB.Width / 2f, renderQuadtree.AABB.Height / 2f) * qtMultiplier;

            body.Position = position.DisplayToSim().ToXNA();
            body.ApplyTorque(100000f);

            lastRegions = renderQuadtree.FindConnectedComponents();
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
                    var worldMouseDis = rw.MapPixelToCoords(Mouse.GetPosition(rw));
                    var worldMouse = body.GetLocalPoint(worldMouseDis.DisplayToSim().ToXNA()).ToSFML().SimToDisplay();
                    var sfmlPos = worldMouse * (1f / qtMultiplier);
                    var aabbMin = (worldMouse - new Vector2f(selectionRadius, selectionRadius)) * (1f / qtMultiplier);
                    var aabbMax = (worldMouse + new Vector2f(selectionRadius, selectionRadius)) * (1f / qtMultiplier);
                    var qtAABB = new AABB2i(new Point2i((int)aabbMin.X, (int)aabbMin.Y), new Point2i((int)aabbMax.X, (int)aabbMax.Y));
                    var qtPos = new Point2i((int)sfmlPos.X, (int)sfmlPos.Y);

                    bool anyChanged = false;
                    if (Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        //quadtree.Set(qtPos, selections[selection].Item2);
                        anyChanged |= physicsQuadtree.SetCircle(qtPos, (int)(selectionRadius / qtMultiplier), Color.Transparent);
                        renderQuadtree.SetCircle(qtPos, (int)(selectionRadius / qtMultiplier), selections[selection].Item2);
                        //quadtree.SetAABB(qtAABB, selections[selection].Item2);
                    }
                    else if (Mouse.IsButtonPressed(Mouse.Button.Right))
                    {
                        //quadtree.Unset(qtPos);
                        anyChanged |= physicsQuadtree.UnsetCircle(qtPos, (int)(selectionRadius / qtMultiplier));
                        renderQuadtree.UnsetCircle(qtPos, (int)(selectionRadius / qtMultiplier));
                    }

                    if (anyChanged)
                    {
                        lastRegions = physicsQuadtree.FindConnectedComponents();
                        lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v2.Count.CompareTo(v1.Count)));

#if DEBUG
                        StringBuilder sb = new StringBuilder();
                        sb.Append("{ ");
                        for (int i = 0; i < lastRegions.Count; i++)
                        {
                            sb.Append(lastRegions[i].Count);
                            sb.Append(", ");
                        }
                        sb.Append(" }");
                        Debug.WriteLine(sb.ToString());
#endif
                    }
                }

                acc += dt;
                while (acc >= step)
                {
                    world.Step(step);
                    acc -= step;
                }
                
                rw.Clear();

                // Draw

                // Draw quadtree
                //BodyRenderer.DrawBody(body, rw, null, null, null, Color.Red);

                var rs = RenderStates.Default;
                var tr = rs.Transform;
                tr.Translate(body.Position.ToSFML().SimToDisplay());
                tr.Rotate(MathExtender.RadianToDegree(body.Rotation));
                rs.Transform = tr;
                bodyRenderer.Draw(rw, rs);

                /*
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
                */

                // For jonah
                var outerRect = new RectangleShape(new Vector2f(renderQuadtree.AABB.Width * qtMultiplier, renderQuadtree.AABB.Height * qtMultiplier));
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
            public Fixture fix;
            public IEnumerable<Vector2f> vertices;
        }

        private void initQuadtree()
        {
            renderQuadtree = new RegionQuadtree<Color>(qtResolution);
            renderQuadtree.AutoExpand = true;

            physicsQuadtree = new RegionQuadtree<Color>(qtResolution);
            physicsQuadtree.AutoExpand = true;

            rects = new Dictionary<AABB2i, QuadData>();

            Action<object, RegionQuadtree<Color>.QuadEventArgs<Color>> onQuadAdded = (s, a) =>
            {
                if (s == physicsQuadtree)
                {
                    return;
                }

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

                var quadData = new QuadData();

                var verts = new Vertices(rectPoints.Select(v => ConvertUnits.ToSimUnits(new Vector2(v.X, v.Y))));
                var fix = FixtureFactory.AttachPolygon(verts, 1f, body);
                quadData.fix = fix;

                bodyRenderer.AddFixture(fix, quadColor);

                rects.Add(aabb, quadData);
            };
            renderQuadtree.OnQuadAdded += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadAdded);
            physicsQuadtree.OnQuadAdded += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadAdded);

            Action<object, RegionQuadtree<Color>.QuadEventArgs<Color>> onQuadRemoving = (s, a) =>
            {
                if (s == physicsQuadtree)
                {
                    return;
                }

                var quadData = rects[a.AABB];

                // fix
                //body.FixtureList.Remove(quadData.fix);
                bodyRenderer.RemoveFixture(quadData.fix);
                body.DestroyFixture(quadData.fix);

                rects.Remove(a.AABB);
            };
            renderQuadtree.OnQuadRemoving += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadRemoving);
            physicsQuadtree.OnQuadRemoving += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadRemoving);

            Action<object, RegionQuadtree<Color>.QuadChangedEventArgs<Color>> onQuadChanged = (s, a) =>
            {
                if (s == physicsQuadtree)
                {
                    return;
                }

                var quadData = rects[a.AABB];
                var quadColor = a.Value;
                var outlineColor = new Color(0, 0, 0, 100);

                bodyRenderer.ModifyFixture(quadData.fix, quadColor);
            };
            renderQuadtree.OnQuadChanged += new EventHandler<RegionQuadtree<Color>.QuadChangedEventArgs<Color>>(onQuadChanged);
            physicsQuadtree.OnQuadChanged += new EventHandler<RegionQuadtree<Color>.QuadChangedEventArgs<Color>>(onQuadChanged);

            Action<object, RegionQuadtree<Color>.QuadExpandEventArgs<Color>> onExpand = (s, a) =>
            {
                if (s == physicsQuadtree)
                {
                    return;
                }

                var oldRoot = renderQuadtree;
                var newRoot = a.NewRoot;

                this.renderQuadtree = newRoot;

                qtResolution++;

                position -= new Vector2f(a.Offset.X, a.Offset.Y) * qtMultiplier;
                body.Position -= new Vector2f(a.Offset.X, a.Offset.Y).RotateRadians(body.Rotation).DisplayToSim().ToXNA() * qtMultiplier;

                resolutionText.DisplayedString = "Resolution " + renderQuadtree.AABB.Width * qtMultiplier + "x" + renderQuadtree.AABB.Height * qtMultiplier;
            };
            renderQuadtree.OnExpand += new EventHandler<RegionQuadtree<Color>.QuadExpandEventArgs<Color>>(onExpand);
            physicsQuadtree.OnExpand += new EventHandler<RegionQuadtree<Color>.QuadExpandEventArgs<Color>>(onExpand);

            renderQuadtree.Set(Color.White);
            physicsQuadtree.Set(Color.White);
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
                        renderQuadtree.Set(Color.White);
                        lastRegions = renderQuadtree.FindConnectedComponents();
                        lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v1.Count.CompareTo(v2.Count)));
                        break;
                    }
                    case Keyboard.Key.E:
                    {
                        if (qtResolution > 18)
                        {
                            break;
                        }

                        renderQuadtree.ExpandFromCenter();
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

            resolutionText = new Text("Resolution " + renderQuadtree.AABB.Width * qtMultiplier + "x" + renderQuadtree.AABB.Height * qtMultiplier, fontBold, 32u);
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
