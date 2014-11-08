using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree.Examples
{
    class DestructibleBody
    {
        public Vector2f Position
        {
            get
            {
                return new Vector2f(
                    ConvertUnits.ToDisplayUnits(body.Position.X), 
                    ConvertUnits.ToDisplayUnits(body.Position.Y));
            }
        }

        private RegionQuadtree<Color> quadtree;

        // Physics
        private Body body;
        private Dictionary<AABB2i, Fixture> fixtures;

        // Rendering
        private Dictionary<AABB2i, QuadData> rects = new Dictionary<AABB2i,QuadData>();
        private VertexArray quadVertexArray = new VertexArray(PrimitiveType.Quads);
        private VertexArray outlineVertexArray = new VertexArray(PrimitiveType.Lines);
        private List<uint> freeQuadIndexes = new List<uint>();
        private List<uint> freeOutlineIndexes = new List<uint>();

        private float rectSize;

        struct QuadData
        {
            public uint quadIndex;
            public uint outlineIndex;
        }

        public DestructibleBody(World world, int power, float rectSize)
        {
            body = new Body(world);
            body.BodyType = BodyType.Dynamic;

            fixtures = new Dictionary<AABB2i, Fixture>();

            this.rectSize = rectSize;

            quadtree = new RegionQuadtree<Color>(power);
            quadtree.OnQuadAdded += quadtree_OnQuadAdded;
            quadtree.OnQuadRemoving += quadtree_OnQuadRemoving;
            quadtree.OnQuadChanged += quadtree_OnQuadChanged;
        }

        void quadtree_OnQuadAdded(object sender, RegionQuadtree<Color>.QuadEventArgs<Color> a)
        {
            var aabb = a.AABB;

            var rectPoints = new List<Vector2f>
                {
                    new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y) * rectSize,
                    new Vector2f(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y) * rectSize,
                    new Vector2f(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y + aabb.Height) * rectSize,
                    new Vector2f(aabb.LowerBound.X, aabb.LowerBound.Y + aabb.Height) * rectSize,
                };

            #region render
            var quadColor = a.Value;
            var outlineColor = new Color(0, 0, 0, 100);

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
            #endregion

            #region physics
            var fixture = FixtureFactory.AttachPolygon(new FarseerPhysics.Common.Vertices()
            {
                ConvertUnits.ToSimUnits(rectPoints[0].X, rectPoints[0].Y),
                ConvertUnits.ToSimUnits(rectPoints[1].X, rectPoints[1].Y),
                ConvertUnits.ToSimUnits(rectPoints[2].X, rectPoints[2].Y),
                ConvertUnits.ToSimUnits(rectPoints[3].X, rectPoints[3].Y),
                
            }, 1f, body);

            fixtures.Add(aabb, fixture);
            #endregion
        }

        void quadtree_OnQuadRemoving(object sender, RegionQuadtree<Color>.QuadEventArgs<Color> a)
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

            var fixture = fixtures[a.AABB];
            body.DestroyFixture(fixture);
            fixtures.Remove(a.AABB);
        }

        void quadtree_OnQuadChanged(object sender, RegionQuadtree<Color>.QuadChangedEventArgs<Color> a)
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
        }

        public void SetCircle(Vector2f point, float radius, Color? value)
        {
            var bodyLocal = body.GetLocalPoint(ConvertUnits.ToSimUnits(point.X, point.Y));
            var sfmlLocal = new Vector2f(ConvertUnits.ToDisplayUnits(bodyLocal.X), ConvertUnits.ToDisplayUnits(bodyLocal.Y));

            var sfmlPos = sfmlLocal * (1f / rectSize);
            var qtPos = new Point2i((int)sfmlPos.X, (int)sfmlPos.Y);

            bool anyChanged = false;
            if (value != null)
            {
                anyChanged |= quadtree.SetCircle(qtPos, (int)(radius / rectSize), value.Value);
            }
            else
            {
                anyChanged |= quadtree.UnsetCircle(qtPos, (int)(radius / rectSize));
            }

            if (anyChanged)
            {
                checkConnections();
            }
        }

        private void checkConnections()
        {

        }

        public void Draw(RenderTarget target)
        {
            var states = RenderStates.Default;
            states.Transform.Translate(Position);
            states.Transform.Rotate(body.Rotation * (float)(180d / Math.PI));

            target.Draw(quadVertexArray, states);
            target.Draw(outlineVertexArray, states);
        }
    }
}
