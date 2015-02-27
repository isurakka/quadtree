using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using SFML.Graphics;
using SFML.Window;
using CircleShape = FarseerPhysics.Collision.Shapes.CircleShape;
using Shape = FarseerPhysics.Collision.Shapes.Shape;
using Transform = FarseerPhysics.Common.Transform;

namespace Quadtree.Examples
{
    public class BatchedBodyRenderer
    {
        private const float outlineThickness = -2f;

        private VertexArray va;

        // fix, (start, end)
        private Dictionary<Fixture, Vector2i> indices;

        // start, size
        private Dictionary<int, int> freeIndices; 

        public BatchedBodyRenderer()
        {
            va = new VertexArray(PrimitiveType.Triangles);
            indices = new Dictionary<Fixture, Vector2i>();
            freeIndices = new Dictionary<int, int>();
        }

        public void AddBody(Body body, Color color)
        {
            foreach (var fix in body.FixtureList)
            {
                AddFixture(fix, color);
            }
        }

        public void AddFixture(Fixture fix, Color color)
        {
            switch (fix.Shape.ShapeType)
            {
                case ShapeType.Unknown:
                    throw new NotImplementedException();
                    break;
                case ShapeType.Circle:
                    {
                        throw new NotImplementedException();
                        break;

                        var circle = (CircleShape)fix.Shape;
                    }
                    break;
                case ShapeType.Edge:
                    throw new NotImplementedException();
                    break;
                case ShapeType.Polygon:
                    {
                        var poly = (PolygonShape)fix.Shape;

                        int startIndex;
                        var vertCount = poly.Vertices.Count;
                        if (vertCount < 3)
                        {
                            break;
                        }

                        var neededVerts = 3 + 3 * (vertCount - 3);

                        var candidates = freeIndices.Where(pair => pair.Value >= neededVerts);
                        if (candidates.Any())
                        {
                            var smallest = candidates.Aggregate((agg, next) => next.Value < agg.Value ? next : agg);
                            startIndex = smallest.Key;
                            freeIndices.Remove(smallest.Key);

                            var newSize = smallest.Value - neededVerts;
                            if (newSize > 0)
                            {
                                var newStart = smallest.Key + newSize;
                                freeIndices.Add(newStart, newSize);
                            }
                        }
                        else
                        {
                            startIndex = (int)va.VertexCount;
                            va.Resize(va.VertexCount + (uint)neededVerts);
                        }

                        var center = poly.Vertices[0].ToSFML().SimToDisplay();

                        var addIndex = 0;
                        var added = 0;
                        for (int i = 1; i < vertCount - 1; i++)
                        {
                            var p1 = poly.Vertices[i].ToSFML().SimToDisplay();
                            var p2 = poly.Vertices[i + 1].ToSFML().SimToDisplay();

                            va[(uint)(startIndex + addIndex)] = new Vertex(center, color);
                            va[(uint)(startIndex + addIndex + 1)] = new Vertex(p1, color);
                            va[(uint)(startIndex + addIndex + 2)] = new Vertex(p2, color);
                            added += 3;

                            addIndex += 3;
                        }

                        Debug.Assert(added == neededVerts);

                        indices.Add(fix, new Vector2i(startIndex, neededVerts));
                    }
                    break;
                case ShapeType.TypeCount:
                    throw new NotImplementedException();
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }
        }

        public void ModifyBody(Body body, Color color)
        {
            foreach (var fix in body.FixtureList)
            {
                ModifyFixture(fix, color);
            }
        }

        public void ModifyFixture(Fixture fix, Color color)
        {
            var index = indices[fix];
            for (int i = index.X; i < index.X + index.Y; i++)
            {
                va[(uint)i] = new Vertex(va[(uint)i].Position, color);
            }
        }

        public void RemoveBody(Body body)
        {
            foreach (var fix in body.FixtureList)
            {
                RemoveFixture(fix);
            }
        }

        public void RemoveFixture(Fixture fix)
        {
            var index = indices[fix];
            for (int i = index.X; i < index.X + index.Y; i++)
            {
                va[(uint)i] = new Vertex(va[(uint)i].Position, Color.Transparent);
            }
            indices.Remove(fix);
            freeIndices.Add(index.X, index.Y);
        }

        public void Draw(RenderTarget rt, RenderStates rs)
        {
            rt.Draw(va, rs);
        }
    }
}