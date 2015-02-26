using System;
using System.Collections.Generic;
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
    public static class BodyRenderer
    {
        private const float outlineThickness = -2f;

        private static readonly Dictionary<PolygonShape, ConvexShape> cache =
            new Dictionary<PolygonShape, ConvexShape>();

        public static void DrawBody(Body body, RenderTarget target, Transform? premadeTransform = null,
            RenderStates? states = null, Color? color = null, Color? outlineColor = null)
        {
            var tf = premadeTransform ?? getTransform(body);

            foreach (var fix in body.FixtureList)
            {
                DrawFixture(fix, target, tf, states ?? RenderStates.Default, color ?? Color.White, outlineColor);
            }
        }

        public static void DrawFixture(Fixture fix, RenderTarget target, Transform? tf = null,
            RenderStates? states = null, Color? color = null, Color? outlineColor = null)
        {
            DrawShape(fix.Shape, tf ?? getTransform(fix.Body), target, states ?? RenderStates.Default,
                color ?? Color.White, outlineColor);
        }

        public static void DrawShape(Shape shape, Transform tf, RenderTarget target, RenderStates? states = null,
            Color? color = null, Color? outlineColor = null)
        {
            switch (shape.ShapeType)
            {
                case ShapeType.Unknown:
                    throw new NotImplementedException();
                    break;
                case ShapeType.Circle:
                    {
                        var circle = (CircleShape)shape;

                        drawCircle(ref tf, target, circle, states ?? RenderStates.Default, color ?? Color.White,
                            outlineColor);
                    }
                    break;
                case ShapeType.Edge:
                    break;
                case ShapeType.Polygon:
                    {
                        var poly = (PolygonShape)shape;

                        drawPolygon(ref tf, target, poly, states ?? RenderStates.Default, color ?? Color.White, outlineColor);
                    }
                    break;
                case ShapeType.TypeCount:
                    throw new NotImplementedException();
                    break;
                default:
                    break;
            }
        }

        private static void drawPolygon(ref Transform tf, RenderTarget target, PolygonShape poly, RenderStates states,
            Color color, Color? outlineColor = null)
        {
            var rot = MathExtender.RadianToDegree(tf.q.GetAngle());

            ConvexShape convex;
            if (cache.ContainsKey(poly))
                convex = cache[poly];
            else
            {
                convex = new ConvexShape((uint)poly.Vertices.Count);

                for (var i = 0; i < poly.Vertices.Count; i++)
                {
                    var point = poly.Vertices[i].ToSFML().SimToDisplay();

                    convex.SetPoint((uint)i, point);
                }

                cache.Add(poly, convex);
            }

            var pos = tf.p.ToSFML().SimToDisplay();

            convex.FillColor = color;

            if (outlineColor != null)
            {
                convex.OutlineThickness = outlineThickness;
                convex.OutlineColor = outlineColor.Value;
            }

            convex.Rotation = rot;
            convex.Position = pos;

            target.Draw(convex, states);
        }

        private static void drawCircle(ref Transform tf, RenderTarget target, CircleShape circle, RenderStates states,
            Color color, Color? outlineColor = null)
        {
            var rot = MathExtender.RadianToDegree(tf.q.GetAngle());
            var radius = circle.Radius.SimToDisplay();
            var pos = MathUtils.Mul(ref tf, circle.Position).ToSFML().SimToDisplay();

            target.Draw(
                new SFML.Graphics.CircleShape(radius, (uint)radius)
                {
                    Origin = new Vector2f(radius, radius),
                    Position = pos,
                    Rotation = rot,
                    FillColor = color,
                    OutlineThickness = outlineThickness,
                    OutlineColor = outlineColor.Value
                }, states);
        }

        private static Transform getTransform(Body body)
        {
            Transform tf;
            body.GetTransform(out tf);

            return tf;
        }
    }
}