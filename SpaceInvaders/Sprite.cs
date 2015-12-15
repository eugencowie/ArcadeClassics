using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceInvaders
{
    class Sprite
    {
        public Texture2D Texture;
        public Rectangle? SourceRectangle;
        public Vector2 Origin;
        public Color Color;

        public Vector2 Position;
        public Vector2 Scale;
        public float UniformScale;
        public float Rotation;

        public SpriteEffects Effects;
        public float Depth;

        public bool Alive;

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle {
                    X = (int)(Position.X - Origin.X),
                    Y = (int)(Position.Y - Origin.Y),
                    Width  = (int)(Texture.Width  * (Scale.X * UniformScale)),
                    Height = (int)(Texture.Height * (Scale.Y * UniformScale))
                };
            }
        }

        public Sprite(Texture2D texture, Vector2 position)
        {
            Texture = texture;
            SourceRectangle = null;
            Origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            Color = Color.White;

            Position = position;
            Scale = Vector2.One;
            UniformScale = 1f;
            Rotation = 0f;

            Effects = SpriteEffects.None;
            Depth = 0.5f;

            Alive = true;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Alive)
            {
                spriteBatch.Draw(
                    Texture,
                    Position,
                    SourceRectangle,
                    Color,
                    Rotation,
                    Origin,
                    Scale * UniformScale,
                    Effects,
                    Depth);
            }
        }
    }
}
